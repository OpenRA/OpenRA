#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

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
		readonly DomainIndex domainIndex;
		RallyPoint rp;

		public ProductionFromMapEdge(ActorInitializer init, ProductionInfo info)
			: base(init, info)
		{
			domainIndex = init.Self.World.WorldActor.Trait<DomainIndex>();
			if (init.Contains<ProductionSpawnLocationInit>())
				spawnLocation = init.Get<ProductionSpawnLocationInit, CPos>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			rp = self.TraitOrDefault<RallyPoint>();
		}

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			var aircraftInfo = producee.TraitInfoOrDefault<AircraftInfo>();
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

			var destination = rp != null ? rp.Location : self.Location;

			var location = spawnLocation;
			if (!location.HasValue)
			{
				if (aircraftInfo != null)
					location = self.World.Map.ChooseClosestEdgeCell(self.Location);

				if (mobileInfo != null)
				{
					var locomotorInfo = mobileInfo.LocomotorInfo;
					location = self.World.Map.ChooseClosestMatchingEdgeCell(self.Location,
						c => mobileInfo.CanEnterCell(self.World, null, c) && domainIndex.IsPassable(c, destination, locomotorInfo));
				}
			}

			// No suitable spawn location could be found, so production has failed.
			if (!location.HasValue)
				return false;

			var pos = self.World.Map.CenterOfCell(location.Value);

			// If aircraft, spawn at cruise altitude
			if (aircraftInfo != null)
				pos += new WVec(0, 0, aircraftInfo.CruiseAltitude.Length);

			var initialFacing = self.World.Map.FacingBetween(location.Value, destination, 0);

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
					newUnit.QueueActivity(move.MoveTo(destination, 2));

				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, destination);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, productionType, td);
			});

			return true;
		}
	}

	public class ProductionSpawnLocationInit : IActorInit<CPos>
	{
		[FieldFromYamlKey]
		readonly CPos value = CPos.Zero;

		public ProductionSpawnLocationInit() { }
		public ProductionSpawnLocationInit(CPos init) { value = init; }
		public CPos Value(World world) { return value; }
	}
}
