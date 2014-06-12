#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.Common.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Attach this to the world actor (not a building!) to define a new shared build queue.",
		"Will only work together with the Production: trait on the actor that actually does the production.",
		"You will also want to add PrimaryBuildings: to let the user choose where new units should exit.")]
	public class ClassicProductionQueueInfo : ProductionQueueInfo, Requires<TechTreeInfo>, Requires<PowerManagerInfo>, Requires<PlayerResourcesInfo>
	{
		[Desc("If you build more actors of the same type,", "the same queue will get its build time lowered for every actor produced there.")]
		public readonly bool SpeedUp = false;
		[Desc("Every time another production building of the same queue is",
			"contructed, the build times of all actors in the queue",
			"decreased by a percentage of the original time.")]
		public readonly int[] BuildTimeSpeedReduction = { 100, 85, 75, 65, 60, 55, 50 };

		public override object Create(ActorInitializer init) { return new ClassicProductionQueue(init.self, this); }
	}

	public class ClassicProductionQueue : ProductionQueue, ISync
	{
		public new ClassicProductionQueueInfo Info;

		public ClassicProductionQueue(Actor self, ClassicProductionQueueInfo info)
			: base(self, self, info)
		{
			this.Info = info;
		}

		[Sync] bool isActive = false;

		public override void Tick(Actor self)
		{
			isActive = self.World.ActorsWithTrait<Production>()
				.Any(x => x.Actor.Owner == self.Owner
					 && x.Trait.Info.Produces.Contains(Info.Type));

			base.Tick(self);
		}

		static ActorInfo[] None = { };
		public override IEnumerable<ActorInfo> AllItems()
		{
			return isActive ? base.AllItems() : None;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			return isActive ? base.BuildableItems() : None;
		}

		protected override bool BuildUnit(string name)
		{
			// Find a production structure to build this actor
			var producers = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& x.Trait.Info.Produces.Contains(Info.Type))
					.OrderByDescending(x => x.Actor.IsPrimaryBuilding())
					.ThenByDescending(x => x.Actor.ActorID);

			if (!producers.Any())
			{
				CancelProduction(name, 1);
				return true;
			}

			foreach (var p in producers.Where(p => !p.Actor.IsDisabled()))
			{
				if (p.Trait.Produce(p.Actor, self.World.Map.Rules.Actors[name]))
				{
					FinishProduction();
					return true;
				}
			}
			return false;
		}

		public override int GetBuildTime(String unitString)
		{
			var unit = self.World.Map.Rules.Actors[unitString];
			if (unit == null || !unit.Traits.Contains<BuildableInfo>())
				return 0;

			if (self.World.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().FastBuild)
				return 0;

			var time = (int)(unit.GetBuildTime() * Info.BuildSpeed);

			if (Info.SpeedUp)
			{
				var selfsameBuildings = self.World.ActorsWithTrait<Production>()
					.Where(p => p.Trait.Info.Produces.Contains(unit.Traits.Get<BuildableInfo>().Queue))
						.Where(p => p.Actor.Owner == self.Owner).ToArray();

				var speedModifier = selfsameBuildings.Count().Clamp(1, Info.BuildTimeSpeedReduction.Length) - 1;
				time = (time * Info.BuildTimeSpeedReduction[speedModifier]) / 100;
			}

			return time;
		}
	}
}
