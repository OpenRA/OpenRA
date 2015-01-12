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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Activities;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Delivers a spice harvester after construction of this building. Optionally provides the harvester insurance function.")]
	public class DeliverFreeActorInfo : ITraitInfo
	{
		[ActorReference]
		[Desc("Name of actor (use HARV if this trait is for refineries)")]
		public readonly string Actor = null;

		[Desc("What the unit should start doing. Warning: If this is not a harvester", "it will break if you use FindResources.")]
		public readonly string InitialActivity = null;

		[Desc("Offset relative to structure-center in 2D (e.g. 1, 2)")]
		public readonly CVec DeliveryOffset = CVec.Zero;

		[ActorReference]
		[Desc("Name of the carrying actor. This actor must have the Carryall trait")]
		public readonly string CarryingActor = null;

		public object Create(ActorInitializer init) { return new DeliverFreeActor(init.Self, this); }
	}

	public class DeliverFreeActor
	{
		readonly Actor self;

		public DeliverFreeActor(Actor self, DeliverFreeActorInfo info)
		{
			this.self = self;

			// Get a carryall spawn location
			var location = ProductionFromMapEdge.GetEdgeCell(self.Location);
			var spawn = self.World.Map.CenterOfCell(location);

			var initialFacing = self.World.Map.FacingBetween(location, self.Location, 0);

			// If aircraft, spawn at cruise altitude
			var ai = self.World.Map.Rules.Actors[info.CarryingActor.ToLower()].Traits.GetOrDefault<AircraftInfo>();
			if (ai != null)
				spawn += new WVec(0, 0, ai.CruiseAltitude.Range);

			// Create Carryall actor
			var carrier = self.World.CreateActor(false, info.CarryingActor, new TypeDictionary
			{
				new LocationInit(location),
				new CenterPositionInit(spawn),
				new OwnerInit(self.Owner),
				new FacingInit(initialFacing)
			});

			// Create harvester actor
			var harv = self.World.CreateActor(false, info.Actor, new TypeDictionary
			{
				new OwnerInit(self.Owner),
			});

			DoHarvesterDelivery(self.Location + info.DeliveryOffset, harv, carrier, info.InitialActivity);
		}

		public void DoHarvesterDelivery(CPos location, Actor client, Actor carrier, string clientInitialActivity)
		{
			if (clientInitialActivity != null)
				client.QueueActivity(Game.CreateObject<Activity>(clientInitialActivity));

			client.Trait<Carryable>().Destination = location;

			carrier.Trait<Carryall>().AttachCarryable(client);

			carrier.QueueActivity(new DeliverUnit(carrier));
			carrier.QueueActivity(new HeliFly(carrier, Target.FromCell(self.World, self.World.Map.ChooseRandomEdgeCell(self.World.SharedRandom))));
			carrier.QueueActivity(new RemoveSelf());

			self.World.AddFrameEndTask(w => self.World.Add(carrier));
		}
	}
}