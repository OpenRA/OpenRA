#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This unit can spawn and eject other actors while flying.")]
	public class ParaDropInfo : TraitInfo, Requires<CargoInfo>
	{
		[Desc("Distance around the drop-point to unload troops.")]
		public readonly WDist DropRange = WDist.FromCells(4);

		[Desc("Wait at least this many ticks between each drop.")]
		public readonly int DropInterval = 5;

		[Desc("Sound to play when dropping.")]
		public readonly string ChuteSound = null;

		public override object Create(ActorInitializer init) { return new ParaDrop(init.Self, this); }
	}

	public class ParaDrop : ITick, ISync, INotifyRemovedFromWorld
	{
		readonly ParaDropInfo info;
		readonly Actor self;
		readonly Cargo cargo;

		public event Action<Actor> OnRemovedFromWorld = self => { };
		public event Action<Actor> OnEnteredDropRange = self => { };
		public event Action<Actor> OnExitedDropRange = self => { };

		[Sync]
		bool inDropRange;

		[Sync]
		Target target;

		[Sync]
		int dropDelay;

		bool checkForSuitableCell;

		public ParaDrop(Actor self, ParaDropInfo info)
		{
			this.info = info;
			this.self = self;
			cargo = self.Trait<Cargo>();
		}

		public void SetLZ(CPos lz, bool checkLandingCell)
		{
			target = Target.FromCell(self.World, lz);
			checkForSuitableCell = checkLandingCell;
		}

		void ITick.Tick(Actor self)
		{
			if (dropDelay > 0)
			{
				dropDelay--;
				return;
			}

			var wasInDropRange = inDropRange;
			inDropRange = target.IsInRange(self.CenterPosition, info.DropRange);

			if (inDropRange && !wasInDropRange)
				OnEnteredDropRange(self);

			if (!inDropRange && wasInDropRange)
				OnExitedDropRange(self);

			// Are we able to drop the next trooper?
			if (!inDropRange || cargo.IsEmpty() || !self.World.Map.Contains(self.Location))
				return;

			var dropActor = cargo.Peek();
			var dropPositionable = dropActor.Trait<IPositionable>();
			var dropCell = self.Location;
			var dropSubCell = dropPositionable.GetAvailableSubCell(dropCell);
			if (dropSubCell == SubCell.Invalid)
			{
				if (checkForSuitableCell)
					return;

				dropSubCell = SubCell.Any;
			}

			// Unload here
			if (cargo.Unload(self) != dropActor)
				throw new InvalidOperationException("Peeked cargo was not unloaded!");

			self.World.AddFrameEndTask(w =>
			{
				dropPositionable.SetPosition(dropActor, dropCell, dropSubCell);

				var dropPosition = dropActor.CenterPosition + new WVec(0, 0, self.CenterPosition.Z - dropActor.CenterPosition.Z);
				dropPositionable.SetCenterPosition(dropActor, dropPosition);
				w.Add(dropActor);
			});

			Game.Sound.Play(SoundType.World, info.ChuteSound, self.CenterPosition);
			dropDelay = info.DropInterval;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			OnRemovedFromWorld(self);
		}
	}
}
