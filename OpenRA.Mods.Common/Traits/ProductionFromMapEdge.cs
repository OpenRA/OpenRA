#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Produce a unit on the closest map edge cell and move into the world.")]
	class ProductionFromMapEdgeInfo : ProductionInfo, UsesInit<ProductionSpawnLocationInit>
	{
		public override object Create(ActorInitializer init) { return new ProductionFromMapEdge(init, this); }
	}

	class ProductionFromMapEdge : Production, INotifyCreated
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

		void INotifyCreated.Created(Actor self)
		{
			rp = self.TraitOrDefault<RallyPoint>();
		}

		public override bool Produce(Actor self, ActorInfo producee, string factionVariant)
		{
			var aircraftInfo = producee.TraitInfoOrDefault<AircraftInfo>();
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

			var passable = mobileInfo != null ? (uint)mobileInfo.GetMovementClass(self.World.Map.Rules.TileSet) : 0;
			var destination = rp != null ? rp.Location : self.Location;

			var location = spawnLocation;
			if (!location.HasValue)
			{
				if (aircraftInfo != null)
					location = self.World.Map.ChooseClosestEdgeCell(self.Location);

				if (mobileInfo != null)
					location = self.World.Map.ChooseClosestMatchingEdgeCell(self.Location,
						c => mobileInfo.CanEnterCell(self.World, null, c) && domainIndex.IsPassable(c, destination, passable));
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
				var td = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new LocationInit(location.Value),
					new CenterPositionInit(pos),
					new FacingInit(initialFacing)
				};

				if (factionVariant != null)
					td.Add(new FactionInit(factionVariant));

				var newUnit = self.World.CreateActor(producee.Name, td);

				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
					newUnit.QueueActivity(move.MoveTo(destination, 2));

				newUnit.SetTargetLine(Target.FromCell(self.World, destination), Color.Green, false);

				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, destination);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit);

				foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
					t.BuildingComplete(newUnit);
			});

			return true;
		}
	}

	public class ProductionSpawnLocationInit : IActorInit<CPos>
	{
		[FieldFromYamlKey] readonly CPos value = CPos.Zero;
		public ProductionSpawnLocationInit() { }
		public ProductionSpawnLocationInit(CPos init) { value = init; }
		public CPos Value(World world) { return value; }
	}
}
