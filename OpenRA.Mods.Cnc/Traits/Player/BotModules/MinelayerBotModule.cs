#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Manages AI minelayer unit related with Minelayer traits.",
		"When enemy damage AI's actors, the location of conflict will be recorded,",
		"If a location is confirmed as can lay mine, it will add/merge to favorite location for usage later")]
	public class MinelayerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Enemy target types to ignore when add the minefield location to conflict location.")]
		public readonly BitSet<TargetableType> IgnoredEnemyTargetTypes = default;

		[Desc("Victim target types that considering conflict location as enemy location instead of victim location.")]
		public readonly BitSet<TargetableType> UseEnemyLocationTargetTypes = default;

		[Desc("Actor types that used for mine laying, must have Minelayer.")]
		public readonly HashSet<string> Minelayers = default;

		[Desc("Find this amount of suitable actors and lay mine to a location.")]
		public readonly int MaxMinelayersPerAssign = 1;

		[Desc("Scan suitable actors and target in this interval.")]
		public readonly int ScanTick = 331;

		[Desc("Minelayer radius.")]
		public readonly int MineFieldRadius = 1;

		[Desc("Minefield location is cancelled if those whose target type belong to allied nearby.")]
		public readonly BitSet<TargetableType> AwayFromAlliedTargetTypes = default;

		[Desc("Minefield location is cancelled if those whose target type belong to enemy nearby.")]
		public readonly BitSet<TargetableType> AwayFromEnemyTargetTypes = default;

		[Desc("Minefield location check distance to AwayFromAlliedTargettype and AwayFromEnemyTargettype.",
			"In addition, if any emeny actor within this range and minefield location is not cancelled,",
			"minelayer will try lay mines at the 3/4 path to minefield location")]
		public readonly int AwayFromCellDistance = 9;

		[Desc("Merge conflict point minefield position to a favorite minefield position if within this range and closest.",
			"If favorite minefield positions is at the max of 5, we always merge it to closest regardless of this")]
		public readonly int FavoritePositionDistance = 6;

		public override object Create(ActorInitializer init) { return new MinelayerBotModule(init.Self, this); }
	}

	public class MinelayerBotModule : ConditionalTrait<MinelayerBotModuleInfo>, IBotTick, IBotRespondToAttack
	{
		const int MaxPositionCacheLength = 5;
		const int RepeatedAltertTicks = 40;

		readonly World world;
		readonly Player player;
		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsBusy;
		readonly CPos?[] conflictPositionQueue;
		readonly CPos?[] favoritePositions;

		int minAssignRoleDelayTicks;
		int conflictPositionLength;
		int favoritePositionsLength;
		int currentFavoritePositionIndex;
		int alertedTicks;

		PathFinder pathFinder;

		public MinelayerBotModule(Actor self, MinelayerBotModuleInfo info)
		: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitCannotBeOrdered = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
			unitCannotBeOrderedOrIsBusy = a => unitCannotBeOrdered(a) || (!a.IsIdle && !(a.CurrentActivity is FlyIdle));
			conflictPositionQueue = new CPos?[MaxPositionCacheLength] { null, null, null, null, null };
			favoritePositions = new CPos?[MaxPositionCacheLength] { null, null, null, null, null };
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minAssignRoleDelayTicks = world.LocalRandom.Next(0, Info.ScanTick);
			alertedTicks = 0;
			conflictPositionLength = 0;
			favoritePositionsLength = 0;
			currentFavoritePositionIndex = 0;
			pathFinder = self.World.WorldActor.Trait<PathFinder>();
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (alertedTicks > 0)
				alertedTicks--;

			if (--minAssignRoleDelayTicks <= 0)
			{
				minAssignRoleDelayTicks = Info.ScanTick;

				var minelayingPosition = CPos.Zero;
				var useFavoritePosition = false;
				var layMineOnHalfway = false;

				while (conflictPositionLength > 0)
				{
					minelayingPosition = conflictPositionQueue[0].Value;
					var check = HasInvalidActorInCircle(world.Map.CenterOfCell(minelayingPosition), WDist.FromCells(Info.AwayFromCellDistance));
					if (check.hasInvalidActors)
						DequeueFirstConflictPosition();
					else
					{
						layMineOnHalfway = check.hasEnemyNearby;
						break;
					}
				}

				TraitPair<Minelayer>[] ats = null;

				if (conflictPositionLength == 0)
				{
					// If enemy turtle themselves at base and we don't have valid position recorded,
					// we will try find a location that at the middle of pathfinding cells
					if (favoritePositionsLength == 0)
					{
						ats = world.ActorsWithTrait<Minelayer>().Where(at => !unitCannotBeOrderedOrIsBusy(at.Actor)).ToArray();
						if (ats.Length == 0)
							return;

						var enemies = world.Actors.Where(a => IsPreferredEnemyUnit(a)).ToArray();
						if (enemies.Length == 0)
							return;

						var enemy = enemies.Random(world.LocalRandom);

						foreach (var at in ats)
						{
							var cells = pathFinder.FindPathToTargetCell(at.Actor, new[] { at.Actor.Location }, enemy.Location, BlockedByActor.Immovable, laneBias: false);
							if (cells != null && !(cells.Count == 0))
							{
								AIUtils.BotDebug("AI ({0}): try find a location to lay mine.", player.ClientIndex);
								EnqueueConflictPosition(cells[cells.Count / 2]);

								// We don't do other things in this tick, just find new location and abort
								return;
							}
						}

						return;
					}
					else
					{
						while (favoritePositionsLength > 0)
						{
							minelayingPosition = favoritePositions[currentFavoritePositionIndex].Value;
							var check = HasInvalidActorInCircle(world.Map.CenterOfCell(minelayingPosition), WDist.FromCells(Info.AwayFromCellDistance));
							if (check.hasInvalidActors)
							{
								DeleteCurrentFavoritePosition();
								if (favoritePositionsLength == 0)
									return;
							}
							else
							{
								layMineOnHalfway = check.hasEnemyNearby;
								useFavoritePosition = true;
								break;
							}
						}
					}
				}

				if (ats == null)
					ats = world.ActorsWithTrait<Minelayer>().Where(at => !unitCannotBeOrderedOrIsBusy(at.Actor)).ToArray();

				if (ats.Length == 0)
					return;

				var orderedActors = new List<Actor>();

				foreach (var at in ats)
				{
					var cells = pathFinder.FindPathToTargetCell(at.Actor, new[] { at.Actor.Location }, minelayingPosition, BlockedByActor.Immovable, laneBias: false);
					if (cells != null && !(cells.Count == 0))
					{
						orderedActors.Add(at.Actor);

						// if there is enemy actor nearby, we will try to lay mine on
						//  3/4 distance to desired position (the path cell is reversed)
						if (layMineOnHalfway)
						{
							minelayingPosition = cells[cells.Count * 1 / 4];
							layMineOnHalfway = false;
						}

						if (orderedActors.Count >= Info.MaxMinelayersPerAssign)
							break;
					}
				}

				if (orderedActors.Count > 0)
				{
					if (useFavoritePosition)
					{
						AIUtils.BotDebug("AI ({0}): Use favorite position {1} at index {2}", player.ClientIndex, minelayingPosition, currentFavoritePositionIndex);
						NextFavoritePositionIndex();
					}
					else
					{
						DequeueFirstConflictPosition();
						AddPositionToFavoritePositions(minelayingPosition);
						AIUtils.BotDebug("AI ({0}): Use in time conflict position {1}", player.ClientIndex, minelayingPosition);
					}

					var vec = new CVec(Info.MineFieldRadius, Info.MineFieldRadius);
					bot.QueueOrder(new Order("PlaceMinefield", null, Target.FromCell(world, minelayingPosition + vec), false, groupedActors: orderedActors.ToArray()) { ExtraLocation = minelayingPosition - vec });
					bot.QueueOrder(new Order("Move", null, Target.FromCell(world, orderedActors.First().Location), true, groupedActors: orderedActors.ToArray()));
				}
				else
				{
					if (useFavoritePosition)
						DeleteCurrentFavoritePosition();
					else
						DequeueFirstConflictPosition();
				}
			}
		}

		void DequeueFirstConflictPosition()
		{
			for (var i = 1; i < conflictPositionLength; i++)
				conflictPositionQueue[i - 1] = conflictPositionQueue[i];
			conflictPositionQueue[conflictPositionLength - 1] = null;
			conflictPositionLength--;
		}

		void DeleteCurrentFavoritePosition()
		{
			for (var i = currentFavoritePositionIndex; i < favoritePositionsLength - 1; i++)
				favoritePositions[i] = favoritePositions[i + 1];
			favoritePositions[favoritePositionsLength - 1] = null;

			if (--favoritePositionsLength > 0)
				currentFavoritePositionIndex = currentFavoritePositionIndex % favoritePositionsLength;
		}

		void AddPositionToFavoritePositions(CPos cpos)
		{
			var favoriteDistSquare = Info.FavoritePositionDistance * Info.FavoritePositionDistance;
			var closestIndex = 0;
			var closestDistSquare = int.MaxValue;
			for (var i = 0; i < favoritePositionsLength; i++)
			{
				var lengthsquare = (favoritePositions[i].Value - cpos).LengthSquared;
				if (lengthsquare < closestDistSquare)
				{
					closestIndex = i;
					closestDistSquare = lengthsquare;
				}
			}

			// Add new if there is space
			if (closestDistSquare > favoriteDistSquare && favoritePositionsLength < favoritePositions.Length)
			{
				favoritePositions[favoritePositionsLength] = cpos;
				favoritePositionsLength++;
			}
			else
			{
				var pos = favoritePositions[closestIndex].Value;
				favoritePositions[closestIndex] = (pos - cpos) / 2 + cpos;
			}
		}

		void NextFavoritePositionIndex()
		{
			currentFavoritePositionIndex = (currentFavoritePositionIndex + 1) % favoritePositionsLength;
		}

		bool IsPreferredEnemyUnit(Actor a)
		{
			if (a == null || a.IsDead || player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || a.Info.HasTraitInfo<HuskInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredEnemyTargetTypes);
		}

		(bool hasInvalidActors, bool hasEnemyNearby) HasInvalidActorInCircle(WPos pos, WDist dist)
		{
			var hasInvalidActor = false;
			var hasEnemyActor = false;
			hasInvalidActor = world.FindActorsInCircle(pos, dist).Any(a =>
			{
				if (a.Owner.RelationshipWith(player) == PlayerRelationship.Ally)
				{
					var targetTypes = a.GetEnabledTargetTypes();
					return !targetTypes.IsEmpty && targetTypes.Overlaps(Info.AwayFromAlliedTargetTypes);
				}

				if (a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy)
				{
					hasEnemyActor = true;
					var targetTypes = a.GetEnabledTargetTypes();
					return !targetTypes.IsEmpty && targetTypes.Overlaps(Info.AwayFromEnemyTargetTypes);
				}

				return false;
			});

			return (hasInvalidActor, hasEnemyActor);
		}

		void EnqueueConflictPosition(CPos cPos)
		{
			if (conflictPositionLength < MaxPositionCacheLength)
			{
				conflictPositionQueue[conflictPositionLength] = cPos;
				conflictPositionLength++;
			}
			else
				conflictPositionQueue[MaxPositionCacheLength - 1] = cPos;
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (alertedTicks > 0 || !IsPreferredEnemyUnit(e.Attacker))
				return;

			alertedTicks = RepeatedAltertTicks;

			var hasInvalidActor = HasInvalidActorInCircle(self.CenterPosition, WDist.FromCells(Info.AwayFromCellDistance)).hasInvalidActors;

			if (hasInvalidActor)
				return;

			var targetTypes = self.GetEnabledTargetTypes();
			CPos pos;
			if (!targetTypes.IsEmpty && targetTypes.Overlaps(Info.UseEnemyLocationTargetTypes))
				pos = e.Attacker.Location;
			else
				pos = self.Location;

			EnqueueConflictPosition(pos);
		}
	}
}
