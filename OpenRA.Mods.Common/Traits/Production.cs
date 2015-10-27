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
		[FieldLoader.Require]
		[Desc("e.g. Infantry, Vehicles, Aircraft, Buildings")]
		public readonly string[] Produces = { };

		public virtual object Create(ActorInitializer init) { return new Production(init, this); }
	}

	public class Production
	{
		readonly Lazy<RallyPoint> rp;

		public readonly ProductionInfo Info;
		public string Faction { get; private set; }

		public Production(ActorInitializer init, ProductionInfo info)
		{
			Info = info;
			rp = Exts.Lazy(() => init.Self.IsDead ? null : init.Self.TraitOrDefault<RallyPoint>());
			Faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		public void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo, string factionVariant)
		{
			var exit = CPos.Zero;
			var exitLocation = CPos.Zero;
			var target = Target.Invalid;

			var bi = producee.TraitInfoOrDefault<BuildableInfo>();
			if (bi != null && bi.ForceFaction != null)
				factionVariant = bi.ForceFaction;

			var td = new TypeDictionary
			{
				new OwnerInit(self.Owner),
			};

			if (self.OccupiesSpace != null)
			{
				exit = self.Location + exitinfo.ExitCell;
				var spawn = self.CenterPosition + exitinfo.SpawnOffset;
				var to = self.World.Map.CenterOfCell(exit);

				var fi = producee.TraitInfoOrDefault<IFacingInfo>();
				var initialFacing = exitinfo.Facing < 0 ? Util.GetFacing(to - spawn, fi == null ? 0 : fi.GetInitialFacing()) : exitinfo.Facing;

				exitLocation = rp.Value != null ? rp.Value.Location : exit;
				target = Target.FromCell(self.World, exitLocation);

				td.Add(new LocationInit(exit));
				td.Add(new CenterPositionInit(spawn));
				td.Add(new FacingInit(initialFacing));
			}

			self.World.AddFrameEndTask(w =>
			{
				if (factionVariant != null)
					td.Add(new FactionInit(factionVariant));

				var newUnit = self.World.CreateActor(producee.Name, td);

				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
				{
					if (exitinfo.MoveIntoWorld)
					{
						if (exitinfo.ExitDelay > 0)
							newUnit.QueueActivity(new Wait(exitinfo.ExitDelay, false));

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

				foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
					t.BuildingComplete(newUnit);
			});
		}

		public virtual bool Produce(Actor self, ActorInfo producee, string factionVariant)
		{
			if (Reservable.IsReserved(self))
				return false;

			// Pick a spawn/exit point pair
			var exit = self.Info.TraitInfos<ExitInfo>().Shuffle(self.World.SharedRandom)
				.FirstOrDefault(e => CanUseExit(self, producee, e));

			if (exit != null || self.OccupiesSpace == null)
			{
				DoProduction(self, producee, exit, factionVariant);
				return true;
			}

			return false;
		}

		static bool CanUseExit(Actor self, ActorInfo producee, ExitInfo s)
		{
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

			self.NotifyBlocker(self.Location + s.ExitCell);

			return mobileInfo == null ||
				mobileInfo.CanEnterCell(self.World, self, self.Location + s.ExitCell, self);
		}
	}
}
