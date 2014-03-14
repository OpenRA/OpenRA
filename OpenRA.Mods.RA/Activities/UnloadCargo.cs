#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class UnloadCargo : Activity
	{
		readonly Actor self;
		readonly Cargo cargo;
		readonly Cloak cloak;
		readonly bool unloadAll;

		public UnloadCargo(Actor self, bool unloadAll)
		{
			this.self = self;
			cargo = self.Trait<Cargo>();
			cloak = self.TraitOrDefault<Cloak>();
			this.unloadAll = unloadAll;
		}

		public CPos? ChooseExitCell(Actor passenger)
		{
			var mobile = passenger.Trait<Mobile>();

			return cargo.CurrentAdjacentCells
				.Shuffle(self.World.SharedRandom)
				.Cast<CPos?>()
				.FirstOrDefault(c => mobile.CanEnterCell(c.Value));
		}

		IEnumerable<CPos> BlockedExitCells(Actor passenger)
		{
			var mobile = passenger.Trait<Mobile>();

			return cargo.CurrentAdjacentCells
				.Where(c => mobile.MovementSpeedForCell(passenger, c) != int.MaxValue && !mobile.CanEnterCell(c));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || cargo.IsEmpty(self))
				return NextActivity;

			if (cloak != null && cloak.Info.UncloakOnUnload)
				cloak.Uncloak();

			var actor = cargo.Peek(self);

			var exitCell = ChooseExitCell(actor);
			if (exitCell == null)
			{
				foreach (var blocker in BlockedExitCells(actor).SelectMany(self.World.ActorMap.GetUnitsAt))
				{
					foreach (var nbm in blocker.TraitsImplementing<INotifyBlockingMove>())
						nbm.OnNotifyBlockingMove(blocker, self);
				}
				return Util.SequenceActivities(new Wait(10), this);
			}

			cargo.Unload(self);

			self.World.AddFrameEndTask(w =>
			{
				if (actor.Destroyed)
					return;

				var mobile = actor.Trait<Mobile>();

				var exitSubcell = mobile.GetDesiredSubcell(exitCell.Value, null);

				mobile.fromSubCell = exitSubcell; // these settings make sure that the below Set* calls
				mobile.toSubCell = exitSubcell; // and the above GetDesiredSubcell call pick a good free subcell for later units being unloaded

				var exit = exitCell.Value.CenterPosition + MobileInfo.SubCellOffsets[exitSubcell];
				var current = self.Location.CenterPosition + MobileInfo.SubCellOffsets[exitSubcell];

				mobile.Facing = Util.GetFacing(exit - current, mobile.Facing);
				mobile.SetPosition(actor, exitCell.Value);
				mobile.SetVisualPosition(actor, current);
				var speed = mobile.MovementSpeedForCell(actor, exitCell.Value);
				var length = speed > 0 ? (exit - current).Length / speed : 0;

				w.Add(actor);
				actor.CancelActivity();
				actor.QueueActivity(new Drag(current, exit, length));
				actor.QueueActivity(mobile.MoveTo(exitCell.Value, 0));

				actor.SetTargetLine(Target.FromCell(exitCell.Value), Color.Green, false);
			});

			if (!unloadAll || cargo.IsEmpty(self))
				return NextActivity;

			return this;
		}
	}
}
