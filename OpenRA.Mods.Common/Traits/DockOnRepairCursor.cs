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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Send actor to dock to repair cursor.")]
	public class DockOnRepairCursorInfo : TraitInfo, Requires<IDockClientManagerInfo>
	{
		[Desc("Docking type")]
		public readonly BitSet<DockType> Type = new("Repair");

		public override object Create(ActorInitializer init) { return new DockOnRepairCursor(init.Self, this); }
	}

	public class DockOnRepairCursor
	{
		protected readonly DockClientManager Manager;
		protected readonly DockOnRepairCursorInfo Info;

		public DockOnRepairCursor(Actor self, DockOnRepairCursorInfo info)
		{
			Info = info;
			Manager = self.Trait<DockClientManager>();
		}

		public virtual Order GetDockOrder(Actor self, MouseInput mi)
		{
			if (Manager.DockingPossible(Info.Type))
			{
				var dockHost = Manager.ClosestDock(null, Info.Type, false, true);
				if (dockHost != null)
					return new Order("Dock", self, Target.FromActor(dockHost.Value.Actor), Target.FromActor(self), mi.Modifiers.HasModifier(Modifiers.Shift));
			}

			return null;
		}
	}
}
