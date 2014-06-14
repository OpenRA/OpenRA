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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This unit has access to build queues.")]
	public class ProductionInfo : ITraitInfo
	{
		[Desc("e.g. Infantry, Vehicles, Aircraft, Buildings")]
		public readonly string[] Produces = { };

		public virtual object Create(ActorInitializer init) { return new Production(this, init.self); }
	}

	[Desc("Where the unit should leave the building. Multiples are allowed if IDs are added: Exit@2, ...")]
	public class ExitInfo : TraitInfo<Exit>
	{
		[Desc("Offset at which that the exiting actor is spawned")]
		public readonly WVec SpawnOffset = WVec.Zero;

		[Desc("Cell offset where the exiting actor enters the ActorMap")]
		public readonly CVec ExitCell = CVec.Zero;
		public readonly int Facing = -1;
		public readonly bool MoveIntoWorld = true;
	}

	public class Exit { }

	public class Production
	{
		Lazy<RallyPoint> rp;

		public ProductionInfo Info;
		public Production(ProductionInfo info, Actor self)
		{
			Info = info;
			rp = Exts.Lazy(() => self.IsDead() ? null : self.TraitOrDefault<RallyPoint>());
		}

		public void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo)
		{
			var inits = new TypeDictionary { new OwnerInit(self.Owner) };

			if (self.OccupiesSpace != null && exitinfo != null)
			{
				var exit = self.Location + exitinfo.ExitCell;
				var spawn = self.CenterPosition + exitinfo.SpawnOffset;

				if (producee.Traits.Contains<IFacingInfo>())
				{
					var to = exit.CenterPosition;
					var fi = producee.Traits.Get<IFacingInfo>();
					var initialFacing = exitinfo.Facing < 0 ? Util.GetFacing(to - spawn, fi.GetInitialFacing()) : exitinfo.Facing;

					inits.Add(new FacingInit(initialFacing));
				}

				inits.Add(new CenterPositionInit(spawn));
				inits.Add(new LocationInit(exit));
			}

			self.World.AddFrameEndTask(w =>
				{
					var newUnit = self.World.CreateActor(producee.Name, inits);
					
					if (exitinfo != null)
					{
						var exit = self.Location + exitinfo.ExitCell;

						var exitLocation = rp.Value != null ? rp.Value.rallyPoint : exit;
						var target = Target.FromCell(exitLocation);
						var nearEnough = rp.Value != null ? WRange.FromCells(rp.Value.nearEnough) : WRange.Zero;


						var move = newUnit.TraitOrDefault<IMove>();

						if (exitinfo.MoveIntoWorld && move != null)
						{
							newUnit.QueueActivity(newUnit.Trait<IMove>().MoveIntoWorld(newUnit, exit));
							newUnit.QueueActivity(new AttackMove.AttackMoveActivity(
								newUnit, move.MoveWithinRange(target, nearEnough)));

							newUnit.SetTargetLine(target, Color.Green, false);
						}

						foreach (var t in self.TraitsImplementing<INotifyProduction>())
							t.UnitProduced(self, newUnit, exit);
					}
				});
		}

		static CPos MoveToRallyPoint(Actor self, Actor newUnit, CPos exitLocation)
		{
			var rp = self.TraitOrDefault<RallyPoint>();
			if (rp == null)
				return exitLocation;

			var move = newUnit.TraitOrDefault<IMove>();
			if (move != null)
			{
				newUnit.QueueActivity(new AttackMove.AttackMoveActivity(
					newUnit, move.MoveTo(rp.rallyPoint, rp.nearEnough)));
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

			DoProduction(self, producee, exit);
			return true;
		}

		static bool CanUseExit(Actor self, ActorInfo producee, ExitInfo s)
		{
			var mobileInfo = producee.Traits.GetOrDefault<MobileInfo>();

			foreach (var blocker in self.World.ActorMap.GetUnitsAt(self.Location + s.ExitCell))
			{
				// Notify the blocker that he's blocking our move:
				foreach (var moveBlocked in blocker.TraitsImplementing<INotifyBlockingMove>())
					moveBlocked.OnNotifyBlockingMove(blocker, self);
			}

			return mobileInfo == null ||
				mobileInfo.CanEnterCell(self.World, self, self.Location + s.ExitCell, self, true, true);
		}
	}
}
