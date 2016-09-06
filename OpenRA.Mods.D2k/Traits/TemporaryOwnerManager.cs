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

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Interacts with the ChangeOwner warhead.",
		"Displays a bar how long this actor is affected and reverts back to the old owner on temporary changes.")]
	public class TemporaryOwnerManagerInfo : ITraitInfo
	{
		public readonly Color BarColor = Color.Orange;

		public object Create(ActorInitializer init) { return new TemporaryOwnerManager(init.Self, this); }
	}

	public class TemporaryOwnerManager : ISelectionBar, ITick, ISync, INotifyOwnerChanged
	{
		readonly TemporaryOwnerManagerInfo info;

		Player originalOwner;
		Player changingOwner;

		[Sync] int remaining = -1;
		int duration;

		public TemporaryOwnerManager(Actor self, TemporaryOwnerManagerInfo info)
		{
			this.info = info;
			originalOwner = self.Owner;
		}

		public void ChangeOwner(Actor self, Player newOwner, int duration)
		{
			remaining = this.duration = duration;
			changingOwner = newOwner;
			self.ChangeOwner(newOwner);
		}

		public void Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			if (--remaining == 0)
			{
				changingOwner = originalOwner;
				self.ChangeOwner(originalOwner);
				self.CancelActivity(); // Stop shooting, you have got new enemies
			}
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (changingOwner == null || changingOwner != newOwner)
				originalOwner = newOwner; // It wasn't a temporary change, so we need to update here
			else
				changingOwner = null; // It was triggered by this trait: reset
		}

		float ISelectionBar.GetValue()
		{
			if (remaining <= 0)
				return 0;

			return (float)remaining / duration;
		}

		Color ISelectionBar.GetColor()
		{
			return info.BarColor;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
