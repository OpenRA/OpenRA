#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public enum SpawnFacing { Default, Projectile, Random }

	[Desc("Spawns actors upon detonation.")]
	public class SpawnActorWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[FieldLoader.Require]
		[ActorReference]
		[Desc("Actors to spawn.")]
		public readonly string[] Actors;

		[Desc("The cell range to try placing the actors within.")]
		public readonly int Range = 10;

		[Desc("Map player to give the actors to. Defaults to the attacker's owner.")]
		public readonly string Owner;

		[Desc("Should this actor link to the actor which created it?")]
		public readonly bool LinkToParent = false;

		[Desc("Should actors always be spawned on the ground?")]
		public readonly bool ForceGround = false;

		[Desc("What direction should the spawned actors face?")]
		public readonly SpawnFacing ActorFacing = SpawnFacing.Default;

		[Desc("Don't spawn actors on this terrain.")]
		public readonly HashSet<string> InvalidTerrain = new();

		[Desc("Defines the image of an optional animation played at the spawning location.")]
		public readonly string Image;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Defines the sequence of an optional animation played at the spawning location.")]
		public readonly string Sequence = "idle";

		[PaletteReference]
		[Desc("Defines the palette of an optional animation played at the spawning location.")]
		public readonly string Palette = "effect";

		public readonly bool UsePlayerPalette = false;

		[Desc("List of sounds that can be played at the spawning location.")]
		public readonly string[] Sounds = Array.Empty<string>();

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			foreach (var a in Actors)
			{
				var actorInfo = rules.Actors[a.ToLowerInvariant()];
				var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();

				if (buildingInfo != null)
					throw new YamlException($"SpawnActorWarhead cannot be used to spawn building actor '{a}'!");
			}
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var world = firedBy.World;
			var map = world.Map;
			var pos = target.CenterPosition;
			var targetCell = map.CellContaining(pos);
			if (!map.Contains(targetCell))
				return;

			var directHit = false;
			foreach (var victim in world.FindActorsOnCircle(pos, WDist.Zero))
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				if (!victim.Info.HasTraitInfo<IHealthInfo>())
					continue;

				if (victim.TraitsImplementing<HitShape>()
					.Any(i => !i.IsTraitDisabled && i.DistanceFromEdge(victim, pos).Length <= 0))
				{
					directHit = true;
					break;
				}
			}

			if (!directHit && !IsValidTarget(map.GetTerrainInfo(targetCell).TargetTypes))
				return;

			foreach (var a in Actors)
			{
				var td = new TypeDictionary();
				var actorName = a.ToLowerInvariant();
				var ai = map.Rules.Actors[actorName];

				var owner = Owner == null ? firedBy.Owner : world.Players.First(p => p.InternalName == Owner);
				td.Add(new OwnerInit(owner));

				if (LinkToParent)
					td.Add(new ParentActorInit(firedBy));

				if (ActorFacing == SpawnFacing.Projectile)
					td.Add(new FacingInit(args.ImpactOrientation.Yaw));
				else if (ActorFacing == SpawnFacing.Random)
					td.Add(new FacingInit(WAngle.Random(world.SharedRandom)));

				var cachedTarget = target;
				world.AddFrameEndTask(w =>
				{
					var immobileInfo = ai.TraitInfoOrDefault<ImmobileInfo>();
					if (immobileInfo != null)
					{
						foreach (var cell in map.FindTilesInCircle(targetCell, Range))
						{
							if (!InvalidTerrain.Contains(map.GetTerrainInfo(cell).Type)
								&& (!immobileInfo.OccupiesSpace || !world.ActorMap.GetActorsAt(cell).Any()))
							{
								td.Add(new LocationInit(cell));
								world.CreateActor(actorName, td);
								PlaySoundAndEffect(w, owner, map.CenterOfCell(cell));
								return;
							}
						}

						return;
					}

					var unit = world.CreateActor(false, actorName, td);
					var positionable = unit.Trait<IPositionable>();
					foreach (var cell in map.FindTilesInCircle(targetCell, Range))
					{
						if (InvalidTerrain.Contains(map.GetTerrainInfo(cell).Type))
							continue;

						if (positionable is Aircraft aircraft)
						{
							var position = map.CenterOfCell(cell);
							var dat = map.DistanceAboveTerrain(cachedTarget.CenterPosition);
							var isAtGroundLevel = ForceGround || dat.Length <= 0;
							if (!aircraft.Info.TakeOffOnCreation && isAtGroundLevel)
							{
								if (aircraft.CanLand(cell))
								{
									positionable.SetPosition(unit, position);
									aircraft.AddInfluence(cell);
									aircraft.FinishedMoving(unit);
								}
								else
									continue;
							}
							else
							{
								position = new WPos(position.X, position.Y, Math.Max(position.Z, dat.Length));
								positionable.SetPosition(unit, position);
								unit.QueueActivity(new FlyIdle(unit));
							}

							PlaySoundAndEffect(w, owner, position);
							w.Add(unit);
							return;
						}

						var subCell = positionable.GetAvailableSubCell(cell);
						if (subCell != SubCell.Invalid)
						{
							positionable.SetPosition(unit, cell, subCell);
							var position = positionable.CenterPosition;
							if (positionable is Mobile mobile)
							{
								var dat = map.DistanceAboveTerrain(cachedTarget.CenterPosition);
								if (!ForceGround && dat.Length > 0)
								{
									positionable.SetCenterPosition(unit,
										new WPos(position.X, position.Y, Math.Max(position.Z, dat.Length)));

									unit.QueueActivity(mobile.ReturnToCell(unit));
								}
							}

							PlaySoundAndEffect(w, owner, position);
							w.Add(unit);
							return;
						}
					}

					unit.Dispose();
				});
			}
		}

		void PlaySoundAndEffect(World w, Player owner, WPos pos)
		{
			if (Image != null)
				w.Add(new SpriteEffect(pos, w, Image, Sequence, UsePlayerPalette ? Palette + owner.InternalName : Palette));

			var sound = Sounds.RandomOrDefault(Game.CosmeticRandom);
			if (sound != null)
				Game.Sound.Play(SoundType.World, sound, pos);
		}
	}
}
