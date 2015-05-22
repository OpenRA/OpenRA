#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This unit has access to build queues.")]
	public class ProductionInfo : ITraitInfo
	{
		[Desc("e.g. Infantry, Vehicles, Aircraft, Buildings")]
		public readonly string[] Produces = { };

		public virtual object Create(ActorInitializer init) { return new Production(init, this); }
	}

	public class Production
	{
		protected readonly Lazy<RallyPoint> RallyPoint;

		public ProductionInfo Info;
		public string Race { get; private set; }

		public Production(ActorInitializer init, ProductionInfo info)
		{
			Info = info;
			RallyPoint = Exts.Lazy(() => init.Self.IsDead ? null : init.Self.TraitOrDefault<RallyPoint>());
			Race = init.Contains<RaceInit>() ? init.Get<RaceInit, string>() : init.Self.Owner.Country.Race;
		}

		public virtual Actor DoProduction(Actor self, ActorInfo actorToProduce, ExitInfo exitInfo, string raceVariant)
		{
			var exit = self.Location + exitInfo.ExitCell;
			var spawn = self.CenterPosition + exitInfo.SpawnOffset;
			var to = self.World.Map.CenterOfCell(exit);

			var fi = actorToProduce.Traits.GetOrDefault<IFacingInfo>();
			var initialFacing = exitInfo.Facing < 0 ? Util.GetFacing(to - spawn, fi == null ? 0 : fi.GetInitialFacing()) : exitInfo.Facing;

			var td = new TypeDictionary
			{
				new OwnerInit(self.Owner),
				new LocationInit(exit),
				new CenterPositionInit(spawn),
				new FacingInit(initialFacing)
			};

			if (!string.IsNullOrEmpty(raceVariant))
				td.Add(new RaceInit(raceVariant));

			return self.World.CreateActor(false, actorToProduce.Name, td);
		}

		public virtual bool Produce(Actor self, IEnumerable<ActorInfo> actorsToProduce, string raceVariant)
		{
			if (Reservable.IsReserved(self))
				return false;

			var allProduced = true;

			foreach (var actorInfo in actorsToProduce)
			{
				// Pick a spawn/exit point pair
				var exit = GetAvailableExit(self, actorInfo);
				if (exit != null)
				{
					var newUnit = DoProduction(self, actorInfo, exit, raceVariant);

					var exitLocation = self.Location + exit.ExitCell;
					var targetLocation = RallyPoint.Value != null ? RallyPoint.Value.Location : exitLocation;

					self.World.AddFrameEndTask(w =>
					{
						MoveIntoWorld(w, newUnit, exit, exitLocation, targetLocation);
						NotifyProduction(self, newUnit, exitLocation);
					});
				}
				else
					allProduced = false;
			}

			return allProduced;
		}

		public static ExitInfo GetAvailableExit(Actor self, ActorInfo actorToProduce)
		{
			return self.Info.Traits.WithInterface<ExitInfo>().Shuffle(self.World.SharedRandom)
				.FirstOrDefault(e => CanUseExit(self, actorToProduce, e));
		}

		protected static bool CanUseExit(Actor self, ActorInfo producee, ExitInfo s)
		{
			var mobileInfo = producee.Traits.GetOrDefault<MobileInfo>();

			self.NotifyBlocker(self.Location + s.ExitCell);

			return mobileInfo == null ||
				mobileInfo.CanEnterCell(self.World, self, self.Location + s.ExitCell, self);
		}

		public void MoveIntoWorld(World world, Actor newActor, ExitInfo chosenExit, CPos exitLocation, CPos targetLocation)
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

			var bi = newActor.Info.Traits.GetOrDefault<BuildableInfo>();
			if (bi != null && bi.InitialActivity != null)
				newActor.QueueActivity(Game.CreateObject<Activity>(bi.InitialActivity));
		}

		public static void NotifyProduction(Actor self, Actor newActor, CPos exitLocation)
		{
			if (!self.IsDead)
				foreach (var t in self.TraitsImplementing<INotifyProduction>())
					t.UnitProduced(self, newActor, exitLocation);

			var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
			foreach (var notify in notifyOthers)
				notify.Trait.UnitProducedByOther(notify.Actor, self, newActor);

			foreach (var t in newActor.TraitsImplementing<INotifyBuildComplete>())
				t.BuildingComplete(newActor);
		}
	}
}
