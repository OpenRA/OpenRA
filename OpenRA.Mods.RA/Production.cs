#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ProductionInfo : ITraitInfo
	{
		public readonly string[] Produces = { };

		public virtual object Create(ActorInitializer init) { return new Production(this); }
	}

	public class ExitInfo : TraitInfo<Exit>
	{
		public readonly int2 SpawnOffset = int2.Zero;	// in px relative to CenterLocation
		public readonly int2 ExitCell = int2.Zero;			// in cells relative to TopLeft
		public readonly int Facing = -1;

		public PVecInt SpawnOffsetVector { get { return (PVecInt)SpawnOffset; } }
		public CVec ExitCellVector { get { return (CVec)ExitCell; } }
	}
	public class Exit {}

	public class Production
	{
		public ProductionInfo Info;
		public Production(ProductionInfo info)
		{
			Info = info;
		}

		public void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo)
		{
			var newUnit = self.World.CreateActor(false, producee.Name, new TypeDictionary
			{
				new OwnerInit( self.Owner ),
			});

			var exit = self.Location + exitinfo.ExitCellVector;
			var spawn = self.Trait<IHasLocation>().PxPosition + exitinfo.SpawnOffsetVector;

			var teleportable = newUnit.Trait<ITeleportable>();
			var facing = newUnit.TraitOrDefault<IFacing>();

			// Set the physical position of the unit as the exit cell
			teleportable.SetPosition(newUnit,exit);
			var to = Util.CenterOfCell(exit);
			teleportable.AdjustPxPosition(newUnit, spawn);
			if (facing != null)
				facing.Facing = exitinfo.Facing < 0 ? Util.GetFacing(to - spawn, facing.Facing) : exitinfo.Facing;
			self.World.Add(newUnit);

			var mobile = newUnit.TraitOrDefault<Mobile>();
			if (mobile != null)
			{
				// Animate the spawn -> exit transition
				var speed = mobile.MovementSpeedForCell(newUnit, exit);
				var length = speed > 0 ? (int)((to - spawn).Length * 3 / speed) : 0;
				newUnit.QueueActivity(new Drag(spawn, to, length));
			}

			var target = MoveToRallyPoint(self, newUnit, exit);

			newUnit.SetTargetLine(Target.FromCell(target), Color.Green, false);
			foreach (var t in self.TraitsImplementing<INotifyProduction>())
				t.UnitProduced(self, newUnit, exit);
		}

		static CPos MoveToRallyPoint(Actor self, Actor newUnit, CPos exitLocation)
		{
			var rp = self.TraitOrDefault<RallyPoint>();
			if (rp == null)
				return exitLocation;

			var mobile = newUnit.TraitOrDefault<Mobile>();
			if (mobile != null)
			{
				newUnit.QueueActivity(new AttackMove.AttackMoveActivity(
					newUnit, mobile.MoveTo(rp.rallyPoint, rp.nearEnough)));
				return rp.rallyPoint;
			}

			// todo: don't talk about HeliFly here.
			var helicopter = newUnit.TraitOrDefault<Helicopter>();
			if (helicopter != null)
			{
				newUnit.QueueActivity(new HeliFly(Util.CenterOfCell(rp.rallyPoint)));
				return rp.rallyPoint;
			}

			return exitLocation;
		}

		public virtual bool Produce(Actor self, ActorInfo producee)
		{
			if (Reservable.IsReserved(self))
				return false;

			// pick a spawn/exit point pair
			var exit = self.Info.Traits.WithInterface<ExitInfo>().Shuffle(self.World.SharedRandom)
				.FirstOrDefault(e => CanUseExit(self, producee, e));

			if (exit != null)
			{
				DoProduction(self, producee, exit);
				return true;
			}

			return false;
		}

		static bool CanUseExit(Actor self, ActorInfo producee, ExitInfo s)
		{
			var mobileInfo = producee.Traits.GetOrDefault<MobileInfo>();

			return mobileInfo == null ||
				mobileInfo.CanEnterCell(self.World, self, self.Location + s.ExitCellVector, self, true, true);
		}
	}
}
