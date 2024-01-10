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
	[Desc("Send actor to link to repair cursor.")]
	public class LinkOnRepairCursorInfo : TraitInfo, Requires<ILinkClientManagerInfo>
	{
		[Desc("Linking type")]
		public readonly BitSet<LinkType> Type = new("Repair");

		public override object Create(ActorInitializer init) { return new LinkOnRepairCursor(init.Self, this); }
	}

	public class LinkOnRepairCursor
	{
		readonly LinkClientManager manager;
		readonly LinkOnRepairCursorInfo info;

		public LinkOnRepairCursor(Actor self, LinkOnRepairCursorInfo info)
		{
			this.info = info;
			manager = self.Trait<LinkClientManager>();
		}

		public Order GetDockOrder(Actor self, MouseInput mi)
		{
			if (manager.LinkingPossible(info.Type))
			{
				var dockHost = manager.ClosestLinkHost(null, info.Type, false, true);
				if (dockHost != null)
					return new Order("Link", self, Target.FromActor(dockHost.Value.Actor), Target.FromActor(self), mi.Modifiers.HasModifier(Modifiers.Shift));
			}

			return null;
		}
	}
}
