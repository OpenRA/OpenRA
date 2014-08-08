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
			var pos = passenger.Trait<IPositionable>();

			return cargo.CurrentAdjacentCells
				.Shuffle(self.World.SharedRandom)
				.Cast<CPos?>()
				.FirstOrDefault(c => pos.CanEnterCell(c.Value));
		}

		IEnumerable<CPos> BlockedExitCells(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			// Find the cells that are blocked by transient actors
			return cargo.CurrentAdjacentCells
				.Where(c => pos.CanEnterCell(c, null, true) != pos.CanEnterCell(c, null, false));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || cargo.IsEmpty(self))
				return NextActivity;

			if (cloak != null && cloak.Info.UncloakOnUnload)
				cloak.Uncloak();

			var actor = cargo.Peek(self);
			var spawn = self.CenterPosition;

			var exitCell = ChooseExitCell(actor);
			if (exitCell == null)
			{
				foreach (var blocker in BlockedExitCells(actor).SelectMany(p => self.World.ActorMap.GetUnitsAt(p)))
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

				var move = actor.Trait<IMove>();
				var pos = actor.Trait<IPositionable>();

				w.Add(actor);
				actor.CancelActivity();
				pos.SetVisualPosition(actor, spawn);
				actor.QueueActivity(move.MoveIntoWorld(actor, exitCell.Value));
				actor.SetTargetLine(Target.FromCell(w, exitCell.Value), Color.Green, false);
			});

			if (!unloadAll || cargo.IsEmpty(self))
				return NextActivity;

			return this;
		}
	}
}
