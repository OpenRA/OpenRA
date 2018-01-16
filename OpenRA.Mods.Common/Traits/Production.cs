#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This unit has access to build queues.")]
	public class ProductionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("e.g. Infantry, Vehicles, Aircraft, Buildings")]
		public readonly string[] Produces = { };

		public override object Create(ActorInitializer init) { return new Production(init, this); }
	}

	public class Production : PausableConditionalTrait<ProductionInfo>, INotifyCreated
	{
		readonly Lazy<RallyPoint> rp;

		public string Faction { get; private set; }

		Building building;

		public Production(ActorInitializer init, ProductionInfo info)
			: base(info)
		{
			rp = Exts.Lazy(() => init.Self.IsDead ? null : init.Self.TraitOrDefault<RallyPoint>());
			Faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		void INotifyCreated.Created(Actor self)
		{
			building = self.TraitOrDefault<Building>();
		}

		public virtual void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo, string productionType, TypeDictionary inits)
		{
			var exit = CPos.Zero;
			var exitLocation = CPos.Zero;
			var target = Target.Invalid;

			// Clone the initializer dictionary for the new actor
			var td = new TypeDictionary();
			foreach (var init in inits)
				td.Add(init);

			if (self.OccupiesSpace != null)
			{
				exit = self.Location + exitinfo.ExitCell;
				var spawn = self.CenterPosition + exitinfo.SpawnOffset;
				var to = self.World.Map.CenterOfCell(exit);

				var initialFacing = exitinfo.Facing;
				if (exitinfo.Facing < 0)
				{
					var delta = to - spawn;
					if (delta.HorizontalLengthSquared == 0)
					{
						var fi = producee.TraitInfoOrDefault<IFacingInfo>();
						initialFacing = fi != null ? fi.GetInitialFacing() : 0;
					}
					else
						initialFacing = delta.Yaw.Facing;
				}

				exitLocation = rp.Value != null ? rp.Value.Location : exit;
				target = Target.FromCell(self.World, exitLocation);

				td.Add(new LocationInit(exit));
				td.Add(new CenterPositionInit(spawn));
				td.Add(new FacingInit(initialFacing));
			}

			self.World.AddFrameEndTask(w =>
			{
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
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, productionType);

				foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
					t.BuildingComplete(newUnit);
			});
		}

		protected virtual ExitInfo SelectExit(Actor self, ActorInfo producee, string productionType, Func<ExitInfo, bool> p)
		{
			return self.RandomExitOrDefault(productionType, p);
		}

		protected ExitInfo SelectExit(Actor self, ActorInfo producee, string productionType)
		{
			return SelectExit(self, producee, productionType, e => CanUseExit(self, producee, e));
		}

		public virtual bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits)
		{
			if (IsTraitDisabled || IsTraitPaused || Reservable.IsReserved(self) || (building != null && building.Locked))
				return false;

			// Pick a spawn/exit point pair
			var exit = SelectExit(self, producee, productionType);

			if (exit != null || self.OccupiesSpace == null)
			{
				DoProduction(self, producee, exit, productionType, inits);

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
