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

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Controls AI bulk unit production from selected queue")]
	public class BulkBuilderBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("If > 0, only produce units as long as there are less than this amount of units idling inside the base.",
			"Beware: if it is less than squad size, e.g. the `SquadSize` from `SquadManagerBotModule`, " +
			"the bot might get stuck as there aren't enough idle units to create squad.")]
		public readonly int IdleBaseUnitsMaximum = -1;

		[Desc("Production queue AI uses for bulk production.")]
		public readonly string ProductionQueue = "Starport";
		public readonly FrozenDictionary<string, int> UnitsToBuild = null;

		[Desc("What units should the AI have a maximum limit to train.")]
		public readonly FrozenDictionary<string, int> UnitLimits = null;

		[Desc("When should the AI start train specific units.")]
		public readonly FrozenDictionary<string, int> UnitDelays = null;

		[Desc("Only bulk production when cash reach this threshold")]
		public readonly int MinCashRequirement = 5000;

		[Desc("Force purchase when cash drop below this threshold")]
		public readonly int ForcePurchaseThresholdCash = 1000;
		public override object Create(ActorInitializer init) { return new BulkBuilderBotModule(init.Self, this); }
	}

	public class BulkBuilderBotModule : ConditionalTrait<BulkBuilderBotModuleInfo>,
		IBotTick, IBotNotifyIdleBaseUnits, INotifyActorDisposing
	{
		public const int FeedbackTime = 100; // ticks; = a bit over 1s. must be >= netlag.
		readonly World world;
		readonly Player player;
		readonly ActorIndex.OwnerAndNames unitsToBuild;

		IBotRequestPauseUnitProduction[] requestPause;
		int idleUnitCount;
		bool enableBuying = false;

		ulong ticks;
		PlayerResources playerResources;

		public BulkBuilderBotModule(Actor self, BulkBuilderBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitsToBuild = new ActorIndex.OwnerAndNames(world, info.UnitsToBuild.Keys, player);
			ticks = (ulong)world.LocalRandom.Next(0, FeedbackTime);
		}

		protected override void Created(Actor self)
		{
			requestPause = self.Owner.PlayerActor.TraitsImplementing<IBotRequestPauseUnitProduction>().ToArray();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		void IBotTick.BotTick(IBot bot)
		{
			ticks++;
			if (ticks % FeedbackTime == 0 && playerResources.GetCashAndResources() > Info.MinCashRequirement)
				enableBuying = true;

			if (enableBuying && !requestPause.Any(rp => rp.PauseUnitProduction))
			{
				var bulkProductionQueue = world.ActorsWithTrait<BulkProductionQueue>()
				.FirstOrDefault(a => a.Actor.Owner == player && a.Trait.Enabled &&
				!a.Trait.HasDeliveryStarted() && a.Trait.Info.Type == Info.ProductionQueue).Trait;

				if (bulkProductionQueue == null || bulkProductionQueue.AllQueued().Any())
					return;

				if (Info.IdleBaseUnitsMaximum >= 0 && Info.IdleBaseUnitsMaximum <= idleUnitCount)
				{
					PurchaseOrder(bulkProductionQueue);
					return;
				}

				if (playerResources.GetCashAndResources() < Info.ForcePurchaseThresholdCash ||
				bulkProductionQueue.GetActorsReadyForDelivery().Count == bulkProductionQueue.MaxCapacity)
				{
					PurchaseOrder(bulkProductionQueue);
					return;
				}

				if (bulkProductionQueue.GetActorsReadyForDelivery().Count < bulkProductionQueue.MaxCapacity)
				{
					var unit = ChooseRandomUnitToBuild(bulkProductionQueue);
					if (unit == null)
					{
						PurchaseOrder(bulkProductionQueue);
					}
					else
					{
						bot.QueueOrder(Order.StartProduction(bulkProductionQueue.Actor, unit.Name, 1));
					}
				}
			}
		}

		void PurchaseOrder(BulkProductionQueue queue)
		{
			if (queue.GetActorsReadyForDelivery().Count > 0)
			{
				world.IssueOrder(
					new Order("PurchaseOrder", queue.Actor, false)
					{
						TargetString = Info.ProductionQueue
					});
			}

			enableBuying = false;
		}

		ActorInfo ChooseRandomUnitToBuild(BulkProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems().Shuffle(world.LocalRandom).ToArray();
			if (buildableThings.Length == 0)
				return null;

			var allUnits = unitsToBuild.Actors.Where(a => !a.IsDead).ToArray();

			ActorInfo desiredUnit = null;
			var desiredError = int.MaxValue;
			foreach (var unit in buildableThings)
			{
				if (!Info.UnitsToBuild.TryGetValue(unit.Name, out var share) ||
					(Info.UnitDelays != null && Info.UnitDelays.TryGetValue(unit.Name, out var delay) && delay > world.WorldTick))
					continue;

				var unitCount = allUnits.Count(a => a.Info.Name == unit.Name);
				if (Info.UnitLimits != null && Info.UnitLimits.TryGetValue(unit.Name, out var count) && unitCount >= count)
					continue;

				var error = allUnits.Length > 0 ? unitCount * 100 / allUnits.Length - share : -1;

				if (error < desiredError)
				{
					desiredError = error;
					desiredUnit = unit;
				}
			}

			return desiredUnit;
		}

		void IBotNotifyIdleBaseUnits.UpdatedIdleBaseUnits(List<Actor> idleUnits)
		{
			idleUnitCount = idleUnits.Count;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			unitsToBuild.Dispose();
		}
	}
}
