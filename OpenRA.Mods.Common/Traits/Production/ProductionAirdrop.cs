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
		public readonly string ReadyAudio = "Reinforce";

		[Desc("Cargo aircraft used.")]
		[ActorReference] public readonly string DeliveringActorType = "c17";

		public override object Create(ActorInitializer init) { return new ProductionAirdrop(this, init.Self); }
	}

	class ProductionAirdrop : Production
	{
		public ProductionAirdrop(ProductionInfo info, Actor self)
			: base(info, self) { }

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
			var owner = self.Owner;
			var actorInfos = actorsToProduce.ToList();

			var info = (ProductionAirdropInfo)Info;
			var deliveringActorType = info.DeliveringActorType;

			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			var startPos = self.Location + new CVec(owner.World.Map.Bounds.Width, 0);
			var endPos = new CPos(owner.World.Map.Bounds.Left - 5, self.Location.Y);

			foreach (var tower in self.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(self);

			owner.World.AddFrameEndTask(w =>
			{
				var altitude = self.World.Map.Rules.Actors[deliveringActorType].Traits.Get<PlaneInfo>().CruiseAltitude;
				var a = w.CreateActor(deliveringActorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WRange.Zero, WRange.Zero, altitude)),
					new OwnerInit(owner),
					new FacingInit(64)
				});

				Action func = null;
				func = () =>
				{
					if (!actorsToProduce.Any())
					{
						a.QueueActivity(new Fly(a, Target.FromCell(w, endPos)));
						a.QueueActivity(new RemoveSelf());
					}
					else
					{
						var actorInfo = actorInfos.First();

						var chosenExit = self.Info.Traits.WithInterface<ExitInfo>()
							.FirstOrDefault(x => CanUseExit(self, actorInfo, x));
						if (chosenExit == null)
						{
							a.QueueActivity(new Wait(10));
							a.QueueActivity(new CallFunc(func));
							return;
						}

						var exitLocation = self.Location + chosenExit.ExitCell;
						var targetLocation = rp.Value != null ? rp.Value.Location : exitLocation;

						var newActor = DoProduction(self, actorInfo, chosenExit, raceVariant);

						var pos = newActor.Trait<IPositionable>();
						var subCell = pos.GetAvailableSubCell(newActor.Location, SubCell.Any, self);

						if (subCell == SubCell.Invalid)
						{
							var blockers = self.World.ActorMap.GetUnitsAt(newActor.Location);
							foreach (var blocker in blockers)
								foreach (var nbm in blocker.TraitsImplementing<INotifyBlockingMove>())
									nbm.OnNotifyBlockingMove(blocker, self);

							a.QueueActivity(new Wait(10));
							a.QueueActivity(new CallFunc(func));
						}
						else
						{
							actorInfos.Remove(actorInfo);

							w.AddFrameEndTask(world =>
							{
								MoveIntoWorld(world, newActor, chosenExit, exitLocation, targetLocation);
								IssueNotifications(self, newActor, exitLocation);

								a.QueueActivity(new CallFunc(func));
							});
						}
					}
				};

				a.QueueActivity(new Fly(a, Target.FromCell(w, self.Location + new CVec(9, 0))));
				a.QueueActivity(new Land(Target.FromActor(self)));
				a.QueueActivity(new CallFunc(func));
			});

			return true;
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
