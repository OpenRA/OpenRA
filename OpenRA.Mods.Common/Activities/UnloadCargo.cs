#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
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

		public Pair<CPos, SubCell>? ChooseExitSubCell(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			return cargo.CurrentAdjacentCells
				.Shuffle(self.World.SharedRandom)
				.Select(c => Pair.New(c, pos.GetAvailableSubCell(c)))
				.Cast<Pair<CPos, SubCell>?>()
				.FirstOrDefault(s => s.Value.Second != SubCell.Invalid);
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
			cargo.Unloading = false;
			if (IsCanceled || cargo.IsEmpty(self))
				return NextActivity;

			if (cloak != null && cloak.Info.UncloakOn.HasFlag(UncloakType.Unload))
				cloak.Uncloak();

			var actor = cargo.Peek(self);
			var spawn = self.CenterPosition;

			var exitSubCell = ChooseExitSubCell(actor);
			if (exitSubCell == null)
			{
				self.NotifyBlocker(BlockedExitCells(actor));

				return ActivityUtils.SequenceActivities(new Wait(10), this);
			}

			cargo.Unload(self);
			self.World.AddFrameEndTask(w =>
			{
				if (actor.Disposed)
					return;

				var move = actor.Trait<IMove>();
				var pos = actor.Trait<IPositionable>();

				actor.CancelActivity();
				pos.SetVisualPosition(actor, spawn);
				actor.QueueActivity(move.MoveIntoWorld(actor, exitSubCell.Value.First, exitSubCell.Value.Second));
				actor.SetTargetLine(Target.FromCell(w, exitSubCell.Value.First, exitSubCell.Value.Second), Color.Green, false);
				w.Add(actor);
			});

			if (!unloadAll || cargo.IsEmpty(self))
				return NextActivity;

			cargo.Unloading = true;
			return this;
		}
	}
}
