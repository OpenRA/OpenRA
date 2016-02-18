#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
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
		public ProductionFromMapEdge(ActorInitializer init, ProductionInfo info)
			: base(init, info) { }

		public override bool Produce(Actor self, ActorInfo producee, string factionVariant)
		{
			var location = self.World.Map.ChooseClosestEdgeCell(self.Location);
			var pos = self.World.Map.CenterOfCell(location);

			// If aircraft, spawn at cruise altitude
			var aircraftInfo = producee.TraitInfoOrDefault<AircraftInfo>();
			if (aircraftInfo != null)
				pos += new WVec(0, 0, aircraftInfo.CruiseAltitude.Length);

			var initialFacing = self.World.Map.FacingBetween(location, self.Location, 0);

			self.World.AddFrameEndTask(w =>
				{
					var td = new TypeDictionary
					{
						new OwnerInit(self.Owner),
						new LocationInit(location),
						new CenterPositionInit(pos),
						new FacingInit(initialFacing)
					};

					if (factionVariant != null)
						td.Add(new FactionInit(factionVariant));

					var newUnit = self.World.CreateActor(producee.Name, td);

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

					foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
						t.BuildingComplete(newUnit);
				});

			return true;
		}
	}
}
