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
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Deliver multiple units via skylift. Works with BuildProductionQueue")]
	public class ProductionStarportInfo : ProductionInfo
	{
		[NotificationReference("Speech")]
		[Desc("Speech notification to play when a unit is delivered.")]
		public readonly string ReadyAudio = "Reinforce";

		[TranslationReference(optional: true)]
		[Desc("Text notification to display when a unit is delivered.")]
		public readonly string ReadyTextNotification = null;

		[FieldLoader.Require]
		[ActorReference(typeof(AircraftInfo))]
		[Desc("Cargo aircraft used for delivery. Must have the `" + nameof(Aircraft) + "` trait.")]
		public readonly string ActorType = null;

		[Desc("Direction the aircraft should face to land.")]
		public readonly WAngle Facing = new(256);

		[Desc("Offset the aircraft used for landing.")]
		public readonly WVec LandOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new ProductionStarport(init, this); }
	}

	sealed class ProductionStarport : Production
	{
		RallyPoint rp;

		Actor transport;

		public ProductionStarport(ActorInitializer init, ProductionStarportInfo info)
			: base(init, info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);

			rp = self.TraitOrDefault<RallyPoint>();
		}

		public bool DeliverOrder(Actor producer, List<(ActorInfo Actor, int Resources, int Cash)> orderedActors, string productionType, BulkProductionQueue queue)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;
			var info = (ProductionStarportInfo)Info;
			var owner = producer.Owner;
			var map = owner.World.Map;
			var startPos = producer.World.Map.ChooseClosestEdgeCell(producer.Location);
			var spawnFacing = producer.World.Map.FacingBetween(startPos, producer.Location, WAngle.Zero);

			foreach (var tower in producer.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(producer);

			owner.World.AddFrameEndTask(w =>
			{
				if (!producer.IsInWorld || producer.IsDead)
				{
					// Try to find another producer
					var anotherProducer = queue.MostLikelyProducer();
					if (anotherProducer.Trait != null && anotherProducer.Trait is ProductionStarport anotherStarport)
						anotherStarport.DeliverOrder(anotherProducer.Actor, orderedActors, productionType, queue);
					else
						queue.DeliverFinished();
					return;
				}

				// aircrafts are delivered by themselfs
				var waitTickbeforeSpawn = 15;
				var aircraftInfo = producer.World.Map.Rules.Actors[info.ActorType].TraitInfo<AircraftInfo>();
				var exit = producer.RandomExitOrDefault(producer.World, productionType);
				var exitCell = producer.Location + exit.Info.ExitCell;
				var destinations = rp != null && rp.Path.Count > 0 ? rp.Path : new List<CPos> { exitCell };

				foreach (var orderedAircraft in orderedActors.Where(actor => actor.Actor.HasTraitInfo<AircraftInfo>()))
				{
					var altitude = orderedAircraft.Actor.TraitInfo<AircraftInfo>().CruiseAltitude;
					var aircraft = w.CreateActor(orderedAircraft.Actor.Name, new TypeDictionary
					{
						new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WDist.Zero, WDist.Zero, altitude)),
						new OwnerInit(owner),
						new FacingInit(spawnFacing)
					});
					var move = aircraft.TraitOrDefault<IMove>();
					if (move != null)
					{
						aircraft.QueueActivity(new Wait(waitTickbeforeSpawn));
						waitTickbeforeSpawn += 15;

						// first move must be to the Producer location
						aircraft.QueueActivity(move.MoveTo(exitCell, 2, evaluateNearestMovableCell: true));
						foreach (var cell in destinations)
						{
							aircraft.QueueActivity(move.MoveTo(cell, 2, evaluateNearestMovableCell: true));
						}
					}
				}

				orderedActors.RemoveAll(actor => actor.Actor.HasTraitInfo<AircraftInfo>());
				if (orderedActors.Count == 0)
				{
					queue.DeliverFinished();
					return;
				}

				transport = w.CreateActor(info.ActorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WDist.Zero, WDist.Zero, aircraftInfo.CruiseAltitude)),
					new OwnerInit(owner),
					new FacingInit(spawnFacing)
				});

				transport.QueueActivity(new DeliverBulkOrder(transport, producer, orderedActors, productionType, queue));
			});

			return true;
		}

		public Exit PublicExit(Actor self, ActorInfo producee, string productionType)
		{
			return SelectExit(self, producee, productionType);
		}
	}
}
