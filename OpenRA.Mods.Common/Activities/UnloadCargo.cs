#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
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
		readonly INotifyUnload[] notifiers;
		readonly bool unloadAll;
		readonly Aircraft aircraft;
		readonly Mobile mobile;
		readonly bool assignTargetOnFirstRun;
		readonly WDist unloadRange;

		Target destination;
		bool takeOffAfterUnload;

		public UnloadCargo(Actor self, WDist unloadRange, bool unloadAll = true)
			: this(self, Target.Invalid, unloadRange, unloadAll)
		{
			assignTargetOnFirstRun = true;
		}

		public UnloadCargo(Actor self, in Target destination, WDist unloadRange, bool unloadAll = true)
		{
			this.self = self;
			cargo = self.Trait<Cargo>();
			notifiers = self.TraitsImplementing<INotifyUnload>().ToArray();
			this.unloadAll = unloadAll;
			aircraft = self.TraitOrDefault<Aircraft>();
			mobile = self.TraitOrDefault<Mobile>();
			this.destination = destination;
			this.unloadRange = unloadRange;
		}

		public (CPos Cell, SubCell SubCell)? ChooseExitSubCell(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			return cargo.CurrentAdjacentCells
				.Shuffle(self.World.SharedRandom)
				.Select(c => (c, pos.GetAvailableSubCell(c)))
				.Cast<(CPos, SubCell SubCell)?>()
				.FirstOrDefault(s => s.Value.SubCell != SubCell.Invalid);
		}

		IEnumerable<CPos> BlockedExitCells(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			// Find the cells that are blocked by transient actors
			return cargo.CurrentAdjacentCells
				.Where(c => pos.CanEnterCell(c, null, BlockedByActor.All) != pos.CanEnterCell(c, null, BlockedByActor.None));
		}

		protected override void OnFirstRun(Actor self)
		{
			if (assignTargetOnFirstRun)
				destination = Target.FromCell(self.World, self.Location);

			// Move to the target destination
			if (aircraft != null)
			{
				// Queue the activity even if already landed in case self.Location != destination
				QueueChild(new Land(self, destination, unloadRange));
				takeOffAfterUnload = !aircraft.AtLandAltitude;
			}
			else if (mobile != null)
			{
				var cell = self.World.Map.Clamp(this.self.World.Map.CellContaining(destination.CenterPosition));
				QueueChild(new Move(self, cell, unloadRange));
			}

			QueueChild(new Wait(cargo.Info.BeforeUnloadDelay));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || cargo.IsEmpty(self))
				return true;

			if (cargo.CanUnload())
			{
				foreach (var inu in notifiers)
					inu.Unloading(self);

				var actor = cargo.Peek(self);
				var spawn = self.CenterPosition;

				var exitSubCell = ChooseExitSubCell(actor);
				if (exitSubCell == null)
				{
					self.NotifyBlocker(BlockedExitCells(actor));
					QueueChild(new Wait(10));
					return false;
				}

				cargo.Unload(self);
				self.World.AddFrameEndTask(w =>
				{
					if (actor.Disposed)
						return;

					var move = actor.Trait<IMove>();
					var pos = actor.Trait<IPositionable>();

					pos.SetPosition(actor, exitSubCell.Value.Cell, exitSubCell.Value.SubCell);
					pos.SetVisualPosition(actor, spawn);

					actor.CancelActivity();
					w.Add(actor);
				});
			}

			if (!unloadAll || !cargo.CanUnload())
			{
				if (cargo.Info.AfterUnloadDelay > 0)
					QueueChild(new Wait(cargo.Info.AfterUnloadDelay, false));

				if (takeOffAfterUnload)
					QueueChild(new TakeOff(self));

				return true;
			}

			return false;
		}
	}
}
