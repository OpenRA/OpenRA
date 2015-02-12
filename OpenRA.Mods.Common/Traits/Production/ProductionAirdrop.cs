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
	[Desc("Deliver the unit in production via skylift.")]
	public class ProductionAirdropInfo : ProductionInfo
	{
		[Desc("Cargo aircraft used.")]
		[ActorReference]
		public readonly string DeliveringActorType = "c17";

		public readonly string ReadyAudio = "Reinforce";

		public override object Create(ActorInitializer init) { return new ProductionAirdrop(this, init.Self); }
	}

	class ProductionAirdrop : Production
	{
		readonly CPos startPos;
		readonly CPos endPos;

		Actor self;
		string factionVariant;

		public ProductionAirdrop(ProductionInfo info, Actor self)
			: base(info, self)
		{
			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			startPos = self.Location + new CVec(self.Owner.World.Map.Bounds.Width, 0);
			endPos = new CPos(self.Owner.World.Map.Bounds.Left - 5, self.Location.Y);
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

			if (raceVariant != null)
				td.Add(new RaceInit(raceVariant));

			return self.World.CreateActor(false, actorToProduce.Name, td);
		}

		public override bool Produce(Actor self, IEnumerable<ActorInfo> actorsToProduce, string raceVariant)
		{
			var info = (ProductionAirdropInfo)Info;
			var deliveringActorType = info.DeliveringActorType;
			var owner = self.Owner;

			this.self = self;
			factionVariant = raceVariant;

			foreach (var tower in self.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(self);

			owner.World.AddFrameEndTask(w =>
			{
				var altitude = self.World.Map.Rules.Actors[deliveringActorType].Traits.Get<PlaneInfo>().CruiseAltitude;
				var deliveringActor = w.CreateActor(deliveringActorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WRange.Zero, WRange.Zero, altitude)),
					new OwnerInit(owner),
					new FacingInit(64)
				});

				deliveringActor.QueueActivity(new Fly(deliveringActor, Target.FromCell(w, self.Location + new CVec(9, 0))));
				deliveringActor.QueueActivity(new Land(Target.FromActor(self)));
				deliveringActor.QueueActivity(new CallFunc(() => MakeDelivery(actorsToProduce.ToList(), deliveringActor, w)));
			});

			return true;
		}

		private void MakeDelivery(ICollection<ActorInfo> actorInfos, Actor deliveringActor, World world)
		{
			if (!actorInfos.Any())
			{
				deliveringActor.QueueActivity(new Fly(deliveringActor, Target.FromCell(world, endPos)));
				deliveringActor.QueueActivity(new RemoveSelf());
			}
			else
			{
				var actorInfo = actorInfos.First();

				var chosenExit = self.Info.Traits.WithInterface<ExitInfo>()
					.FirstOrDefault(x => CanUseExit(self, actorInfo, x));
				if (chosenExit == null)
				{
					WaitAndRetry(deliveringActor, world, actorInfos);
					return;
				}

				var exitLocation = self.Location + chosenExit.ExitCell;
				var targetLocation = rp.Value != null ? rp.Value.Location : exitLocation;

				var newActor = DoProduction(self, actorInfo, chosenExit, factionVariant);

				var pos = newActor.Trait<IPositionable>();
				var subCell = pos.GetAvailableSubCell(newActor.Location, SubCell.Any, self);

				if (subCell == SubCell.Invalid)
				{
					var blockers = self.World.ActorMap.GetUnitsAt(newActor.Location);
					foreach (var blocker in blockers)
						foreach (var nbm in blocker.TraitsImplementing<INotifyBlockingMove>())
							nbm.OnNotifyBlockingMove(blocker, self);

					WaitAndRetry(deliveringActor, world, actorInfos);
				}
				else
				{
					actorInfos.Remove(actorInfo);

					world.AddFrameEndTask(w =>
						{
							MoveIntoWorld(w, newActor, chosenExit, exitLocation, targetLocation);
							IssueNotifications(self, newActor, exitLocation);

							deliveringActor.QueueActivity(new CallFunc(() => MakeDelivery(actorInfos, deliveringActor, world)));
						});
				}
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
			newActor.SetTargetLine(target, rp.Value != null ? Color.Red : Color.Green, false);
		}
	}
}
