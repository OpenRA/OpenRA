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

		public virtual object Create(ActorInitializer init) { return new Production(this, init.Self); }
	}

	public class Production
	{
		protected Lazy<RallyPoint> rp;

		public ProductionInfo Info;
		public Production(ProductionInfo info, Actor self)
		{
			Info = info;
			rp = Exts.Lazy(() => self.IsDead ? null : self.TraitOrDefault<RallyPoint>());
		}

		public virtual Actor DoProduction(Actor self, ActorInfo actorToProduce, ExitInfo exitInfo, string raceVariant)
		{
			var exit = self.Location + exitInfo.ExitCell;
			var spawn = self.CenterPosition + exitInfo.SpawnOffset;
			var to = self.World.Map.CenterOfCell(exit);

			var fi = actorToProduce.Traits.GetOrDefault<IFacingInfo>();
			var initialFacing = exitInfo.Facing < 0 ? Util.GetFacing(to - spawn, fi == null ? 0 : fi.GetInitialFacing()) : exitInfo.Facing;

			var exitLocation = rp.Value != null ? rp.Value.Location : exit;
			var target = Target.FromCell(self.World, exitLocation);

			var td = new TypeDictionary
			{
				new OwnerInit(self.Owner),
				new LocationInit(exit),
				new CenterPositionInit(spawn),
				new FacingInit(initialFacing)
			};

			if (raceVariant != null)
				td.Add(new RaceInit(raceVariant));

			var newUnit = self.World.CreateActor(false, actorToProduce.Name, td);

			self.World.AddFrameEndTask(w =>
			{
				self.World.Add(newUnit);
				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
				{
					if (exitInfo.MoveIntoWorld)
					{
						newUnit.QueueActivity(move.MoveIntoWorld(newUnit, exit));
						newUnit.QueueActivity(new AttackMoveActivity(
							newUnit, move.MoveTo(exitLocation, 1)));
					}
				}

				newUnit.SetTargetLine(target, rp.Value != null ? Color.Red : Color.Green, false);

				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, exit);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit);

				var bi = newUnit.Info.Traits.GetOrDefault<BuildableInfo>();
				if (bi != null && bi.InitialActivity != null)
					newUnit.QueueActivity(Game.CreateObject<Activity>(bi.InitialActivity));

				foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
					t.BuildingComplete(newUnit);
			});

			return newUnit;
		}

		public virtual bool Produce(Actor self, IEnumerable<ActorInfo> actorsToProduce, string raceVariant)
		{
			if (Reservable.IsReserved(self))
				return false;

		    var allProduced = true;

			foreach (var actorInfo in actorsToProduce)
			{
				// pick a spawn/exit point pair
				var exit = self.Info.Traits.WithInterface<ExitInfo>().Shuffle(self.World.SharedRandom)
					.FirstOrDefault(e => CanUseExit(self, actorInfo, e));
			    if (exit != null)
			        DoProduction(self, actorInfo, exit, raceVariant);
			    else
			        allProduced = false;
			}

			return allProduced;
		}

		protected static bool CanUseExit(Actor self, ActorInfo producee, ExitInfo s)
		{
			var mobileInfo = producee.Traits.GetOrDefault<MobileInfo>();

			self.NotifyBlocker(self.Location + s.ExitCell);

			return mobileInfo == null ||
				mobileInfo.CanEnterCell(self.World, self, self.Location + s.ExitCell, self);
		}
	}
}
