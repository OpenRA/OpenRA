#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Produce a unit on the closest map edge cell and move into the world.")]
	class ProductionFromMapEdgeInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new ProductionFromMapEdge(init, this); }
	}

	class ProductionFromMapEdge : Production
	{
		CPos closestEdgeCell;
		WPos closestEdgeCellCenter;

		public ProductionFromMapEdge(ActorInitializer init, ProductionInfo info)
			: base(init, info) { }

		public override bool Produce(Actor self, IEnumerable<ActorInfo> actorsToProduce, string raceVariant)
		{
			closestEdgeCell = self.World.Map.ChooseClosestEdgeCell(self.Location);
			closestEdgeCellCenter = self.World.Map.CenterOfCell(closestEdgeCell);

			foreach (var actorInfo in actorsToProduce)
				DoProduction(self, actorInfo, null, raceVariant);

			return true;
		}

		public override Actor DoProduction(Actor self, ActorInfo actorToProduce, ExitInfo exitInfo, string raceVariant)
		{
			// If aircraft, spawn at cruise altitude
			var aircraftInfo = actorToProduce.Traits.GetOrDefault<AircraftInfo>();
			if (aircraftInfo != null)
				closestEdgeCellCenter += new WVec(0, 0, aircraftInfo.CruiseAltitude.Range);

			var initialFacing = self.World.Map.FacingBetween(closestEdgeCell, self.Location, 0);

			var td = new TypeDictionary
					{
						new OwnerInit(self.Owner),
						new LocationInit(closestEdgeCell),
						new CenterPositionInit(closestEdgeCellCenter),
						new FacingInit(initialFacing)
					};

			if (!string.IsNullOrEmpty(raceVariant))
				td.Add(new RaceInit(raceVariant));

			var newUnit = self.World.CreateActor(actorToProduce.Name, td);

			self.World.AddFrameEndTask(w =>
			{
				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
					newUnit.QueueActivity(move.MoveIntoWorld(newUnit, self.Location));

				newUnit.SetTargetLine(Target.FromCell(self.World, self.Location), Color.Green, false);

				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, self.Location);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit);

				var bi = newUnit.Info.Traits.GetOrDefault<BuildableInfo>();
				if (bi != null && bi.InitialActivity != null)
					newUnit.QueueActivity(Game.CreateObject<Activity>(bi.InitialActivity));

				foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
					t.BuildingComplete(newUnit);
			});

			return newUnit;
		}
	}
}
