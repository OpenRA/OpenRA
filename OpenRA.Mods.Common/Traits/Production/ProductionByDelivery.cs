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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Deliver produced units from outside the map.")]
	public class ProductionByDeliveryInfo : ProductionInfo
	{
		[Desc("Delivering actor name. The actor needs to have either of the `Helicopter` or `Plane` traits.")]
		[ActorReference]
		public readonly string DeliveryActor = "c17";

		public readonly string ReadyAudio = "Reinforce";

		public override object Create(ActorInitializer init) { return new ProductionByDelivery(init, this); }
	}

	class ProductionByDelivery : Production
	{
		readonly Actor self;
		readonly CPos startPos;
		readonly CPos endPos;
		readonly ProductionByDeliveryInfo info;
		readonly AircraftInfo aircraft;
		readonly bool isPlane;

		string factionVariant;

		public ProductionByDelivery(ActorInitializer init, ProductionByDeliveryInfo info)
			: base(init, info)
		{
			self = init.Self;

			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			startPos = self.Location + new CVec(self.Owner.World.Map.Bounds.Width, 0);
			endPos = new CPos(self.Owner.World.Map.Bounds.Left - 5, self.Location.Y);
			this.info = info;

			aircraft = self.World.Map.Rules.Actors[info.DeliveryActor].Traits.Get<AircraftInfo>();
			isPlane = aircraft is PlaneInfo;
		}

		public override Actor DoProduction(Actor self, ActorInfo actorToProduce, ExitInfo exitInfo, string raceVariant)
		{
			var exit = self.Location + exitInfo.ExitCell;
			var spawn = self.CenterPosition + exitInfo.SpawnOffset;

			var td = new TypeDictionary
			{
				new OwnerInit(self.Owner),
				new LocationInit(exit),
				new CenterPositionInit(spawn)
			};

			if (!string.IsNullOrEmpty(raceVariant))
				td.Add(new RaceInit(raceVariant));

			return self.World.CreateActor(false, actorToProduce.Name, td);
		}

		public override bool Produce(Actor self, IEnumerable<ActorInfo> actorsToProduce, string raceVariant)
		{
			var deliveringActorType = info.DeliveryActor;
			var owner = self.Owner;

			factionVariant = raceVariant;

			// Check if there is a valid drop-off point before sending the transport
			var exit = GetAvailableExit(self, actorsToProduce.First());
			if (exit == null)
				return false;

			foreach (var trait in self.TraitsImplementing<INotifyDelivery>())
				trait.IncomingDelivery(self);

			owner.World.AddFrameEndTask(w =>
			{
				var deliveringActor = w.CreateActor(deliveringActorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WRange.Zero, WRange.Zero, aircraft.CruiseAltitude)),
					new OwnerInit(owner),
					new FacingInit(64)
				});

				if (isPlane)
					deliveringActor.QueueActivity(new Fly(deliveringActor, Target.FromCell(w, self.Location + new CVec(9, 0))));
				else
					deliveringActor.QueueActivity(new HeliFly(deliveringActor, Target.FromCell(w, self.Location + new CVec(9, 0))));

				deliveringActor.QueueActivity(new CallFunc(() => TryToLand(deliveringActor, actorsToProduce)));
			});

			return true;
		}

		void TryToLand(Actor deliveringActor, IEnumerable<ActorInfo> actorsToProduce)
		{
			// Check if there is a valid drop-off point before beginning to descend
			var exit = GetAvailableExit(self, actorsToProduce.First());

			// Abort the landing and refund the player
			if (exit == null || self.IsDead || !self.IsInWorld)
			{
				var value = actorsToProduce.Sum(actor => actor.Traits.Get<ValuedInfo>().Cost);
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(value);

				if (isPlane)
					deliveringActor.QueueActivity(new Fly(deliveringActor, Target.FromCell(self.World, endPos)));
				else
					deliveringActor.QueueActivity(new HeliFly(deliveringActor, Target.FromCell(self.World, endPos)));

				deliveringActor.QueueActivity(new RemoveSelf());

				return;
			}

			if (isPlane)
				deliveringActor.QueueActivity(new Land(deliveringActor, Target.FromActor(self)));
			else
			{
				deliveringActor.QueueActivity(new HeliFly(deliveringActor, Target.FromActor(self)));
				deliveringActor.QueueActivity(new HeliLand(deliveringActor, false));
			}

			deliveringActor.QueueActivity(new CallFunc(() => MakeDelivery(actorsToProduce.ToList(), deliveringActor, deliveringActor.World)));
		}

		void MakeDelivery(ICollection<ActorInfo> actorInfos, Actor deliveringActor, World world)
		{
			if (!actorInfos.Any())
			{
				if (isPlane)
					deliveringActor.QueueActivity(new Fly(deliveringActor, Target.FromCell(world, endPos)));
				else
					deliveringActor.QueueActivity(new HeliFly(deliveringActor, Target.FromCell(world, endPos)));

				deliveringActor.QueueActivity(new RemoveSelf());
			}
			else
			{
				var actorInfo = actorInfos.First();

				var chosenExit = GetAvailableExit(self, actorInfo);
				if (chosenExit == null)
				{
					WaitAndRetry(deliveringActor, world, actorInfos);
					return;
				}

				var exitLocation = self.Location + chosenExit.ExitCell;
				var targetLocation = RallyPoint.Value != null ? RallyPoint.Value.Location : exitLocation;

				var newActor = DoProduction(self, actorInfo, chosenExit, factionVariant);

				actorInfos.Remove(actorInfo);

				world.AddFrameEndTask(w =>
				{
					MoveIntoWorld(w, newActor, chosenExit, exitLocation, targetLocation);
					IssueNotifications(self, newActor, exitLocation);

					deliveringActor.QueueActivity(new CallFunc(() => MakeDelivery(actorInfos, deliveringActor, world)));
					Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Country.Race);
				});
			}
		}

		void WaitAndRetry(Actor actor, World world, ICollection<ActorInfo> actorInfos)
		{
			actor.QueueActivity(new Wait(10));
			actor.QueueActivity(new CallFunc(() => MakeDelivery(actorInfos, actor, world)));
		}

		static void IssueNotifications(Actor self, Actor newActor, CPos exitLocation)
		{
			if (!self.IsDead)
				foreach (var t in self.TraitsImplementing<INotifyProduction>())
					t.UnitProduced(self, newActor, exitLocation);

			var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
			foreach (var notify in notifyOthers)
				notify.Trait.UnitProducedByOther(notify.Actor, self, newActor);

			var bi = newActor.Info.Traits.GetOrDefault<BuildableInfo>();
			if (bi != null && bi.InitialActivity != null)
				newActor.QueueActivity(Game.CreateObject<Activity>(bi.InitialActivity));

			foreach (var t in newActor.TraitsImplementing<INotifyBuildComplete>())
				t.BuildingComplete(newActor);
		}

		void MoveIntoWorld(World world, Actor newActor, ExitInfo chosenExit, CPos exitLocation, CPos targetLocation)
		{
			world.Add(newActor);

			var move = newActor.TraitOrDefault<IMove>();
			if (move != null && chosenExit.MoveIntoWorld)
			{
				newActor.QueueActivity(move.MoveIntoWorld(newActor, exitLocation));
				newActor.QueueActivity(new AttackMoveActivity(newActor, move.MoveTo(targetLocation, 1)));
			}

			var target = Target.FromCell(world, targetLocation);
			newActor.SetTargetLine(target, RallyPoint.Value != null ? Color.Red : Color.Green, false);
		}
	}
}
