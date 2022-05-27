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

using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Player receives a unit for free once the building is placed.",
		"If you want more than one unit to be delivered, copy this section and assign IDs like FreeActorWithDelivery@2, ...")]
	public class FreeActorWithDeliveryInfo : FreeActorInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Name of the delivering actor. This actor must have the `" + nameof(Carryall) + "` trait")]
		public readonly string DeliveringActor = null;

		[Desc("Cell coordinates for spawning the delivering actor. If left blank, the closest edge cell will be chosen.")]
		public readonly CPos SpawnLocation = CPos.Zero;

		[Desc("Offset relative to the top-left cell of the building.")]
		public readonly CVec DeliveryOffset = CVec.Zero;

		[Desc("Range to search for an alternative delivery location if the DeliveryOffset cell is blocked.")]
		public readonly WDist DeliveryRange = WDist.Zero;

		public override object Create(ActorInitializer init) { return new FreeActorWithDelivery(init, this); }
	}

	public class FreeActorWithDelivery : FreeActor
	{
		readonly FreeActorWithDeliveryInfo info;
		readonly Actor self;

		public FreeActorWithDelivery(ActorInitializer init, FreeActorWithDeliveryInfo info)
			: base(init, info)
		{
			self = init.Self;
			this.info = info;
		}

		protected override void TraitEnabled(Actor self)
		{
			if (!allowSpawn)
				return;

			allowSpawn = info.AllowRespawn;

			DoDelivery(self.Location + info.DeliveryOffset, Info.Actor, info.DeliveringActor);
		}

		public void DoDelivery(CPos location, string actorName, string carrierActorName)
		{
			CreateActors(actorName, carrierActorName, out var cargo, out var carrier);

			var carryable = cargo.Trait<Carryable>();
			carryable.Reserve(cargo, carrier);

			var carryall = carrier.Trait<Carryall>();
			carryall.AttachCarryable(carrier, cargo);
			carrier.QueueActivity(new DeliverUnit(carrier, Target.FromCell(self.World, location), info.DeliveryRange, carryall.Info.TargetLineColor));
			carrier.QueueActivity(new Fly(carrier, Target.FromCell(self.World, self.World.Map.ChooseRandomEdgeCell(self.World.SharedRandom))));
			carrier.QueueActivity(new RemoveSelf());

			self.World.AddFrameEndTask(w => self.World.Add(carrier));
		}

		void CreateActors(string actorName, string deliveringActorName, out Actor cargo, out Actor carrier)
		{
			// Get a carryall spawn location
			var location = info.SpawnLocation;
			if (location == CPos.Zero)
				location = self.World.Map.ChooseClosestEdgeCell(self.Location);

			var spawn = self.World.Map.CenterOfCell(location);

			var initialFacing = self.World.Map.FacingBetween(location, self.Location, WAngle.Zero);

			// If aircraft, spawn at cruise altitude
			var aircraftInfo = self.World.Map.Rules.Actors[deliveringActorName.ToLowerInvariant()].TraitInfoOrDefault<AircraftInfo>();
			if (aircraftInfo != null)
				spawn += new WVec(0, 0, aircraftInfo.CruiseAltitude.Length);

			// Create delivery actor
			carrier = self.World.CreateActor(false, deliveringActorName, new TypeDictionary
			{
				new LocationInit(location),
				new CenterPositionInit(spawn),
				new OwnerInit(self.Owner),
				new FacingInit(initialFacing)
			});

			// Create delivered actor
			cargo = self.World.CreateActor(false, actorName, new TypeDictionary
			{
				new OwnerInit(self.Owner),
			});
		}
	}
}
