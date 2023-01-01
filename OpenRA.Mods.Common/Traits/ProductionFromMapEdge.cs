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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Produce a unit on the closest map edge cell and move into the world.")]
	class ProductionFromMapEdgeInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new ProductionFromMapEdge(init, this); }
	}

	class ProductionFromMapEdge : Production
	{
		readonly CPos? spawnLocation;
		readonly IPathFinder pathFinder;
		RallyPoint rp;

		public ProductionFromMapEdge(ActorInitializer init, ProductionInfo info)
			: base(init, info)
		{
			pathFinder = init.Self.World.WorldActor.Trait<IPathFinder>();

			var spawnLocationInit = init.GetOrDefault<ProductionSpawnLocationInit>(info);
			if (spawnLocationInit != null)
				spawnLocation = spawnLocationInit.Value;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			rp = self.TraitOrDefault<RallyPoint>();
		}

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits, int refundableValue)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			var aircraftInfo = producee.TraitInfoOrDefault<AircraftInfo>();
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

			var destinations = rp != null && rp.Path.Count > 0 ? rp.Path : new List<CPos> { self.Location };

			var location = spawnLocation;
			if (!location.HasValue)
			{
				if (aircraftInfo != null)
					location = self.World.Map.ChooseClosestEdgeCell(self.Location);

				if (mobileInfo != null)
				{
					var locomotor = self.World.WorldActor.TraitsImplementing<Locomotor>().First(l => l.Info.Name == mobileInfo.Locomotor);
					location = self.World.Map.ChooseClosestMatchingEdgeCell(self.Location,
						c => mobileInfo.CanEnterCell(self.World, null, c) && pathFinder.PathExistsForLocomotor(locomotor, c, destinations[0]));
				}
			}

			// No suitable spawn location could be found, so production has failed.
			if (!location.HasValue)
				return false;

			var pos = self.World.Map.CenterOfCell(location.Value);

			// If aircraft, spawn at cruise altitude
			if (aircraftInfo != null)
				pos += new WVec(0, 0, aircraftInfo.CruiseAltitude.Length);

			var initialFacing = self.World.Map.FacingBetween(location.Value, destinations[0], WAngle.Zero);

			self.World.AddFrameEndTask(w =>
			{
				var td = new TypeDictionary();
				foreach (var init in inits)
					td.Add(init);

				td.Add(new LocationInit(location.Value));
				td.Add(new CenterPositionInit(pos));
				td.Add(new FacingInit(initialFacing));

				var newUnit = self.World.CreateActor(producee.Name, td);

				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
					foreach (var cell in destinations)
						newUnit.QueueActivity(move.MoveTo(cell, 2, evaluateNearestMovableCell: true));

				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, destinations[0]);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, productionType, td);
			});

			return true;
		}
	}

	public class ProductionSpawnLocationInit : ValueActorInit<CPos>
	{
		public ProductionSpawnLocationInit(TraitInfo info, CPos value)
			: base(info, value) { }
	}
}
