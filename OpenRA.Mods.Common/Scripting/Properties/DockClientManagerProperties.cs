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

using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class LinkClientManagerProperties : ScriptActorProperties, Requires<DockClientManagerInfo>
	{
		readonly DockClientManager manager;

		public LinkClientManagerProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			manager = self.Trait<DockClientManager>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Can we link to this actor? If dockType is undefined check all valid hosts.")]
		public bool CanDockAt(Actor target, string[] dockType = default, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			if (target == null)
				throw new LuaException("Target actor is null.");

			BitSet<DockType> type = default;
			if (dockType != null && dockType.Length != 0)
			{
				type = BitSet<DockType>.FromStringsNoAlloc(dockType);
				if (!manager.DockingPossible(type, forceEnter))
					return false;
			}

			return manager.CanDockAt(target, type, forceEnter, ignoreOccupancy);
		}

		[ScriptActorPropertyActivity]
		[Desc("Link to the actor. If linkType is undefined link to all validhosts.")]
		public void Dock(Actor target, string[] dockType = default, bool forceEnter = false, bool ignoreOccupancy = true)
		{
			if (target == null)
				throw new LuaException("Target actor is null.");

			if (manager.IsTraitDisabled)
				return;

			BitSet<DockType> type = default;
			if (dockType != null && dockType.Length != 0)
			{
				type = BitSet<DockType>.FromStringsNoAlloc(dockType);
				if (!manager.DockingPossible(type, forceEnter))
					return;
			}

			var docks = manager.AvailableDockHosts(target, type, forceEnter, ignoreOccupancy).ToList();
			if (docks.Count == 0)
				return;

			var dock = docks.ClosestDock(Self, manager);
			if (!dock.HasValue)
				return;

			Self.QueueActivity(new MoveToDock(Self, dock.Value.Actor, dock.Value.Trait));
		}

		[ScriptActorPropertyActivity]
		[Desc("Find and link to the closest LinkHost, if linkType is undefined search for all valid docks. "
			+ "Returns true if docking activity was successfully queued.")]
		public bool DockToClosestHost(string[] dockType = default, bool forceEnter = false, bool ignoreOccupancy = true)
		{
			if (manager.IsTraitDisabled)
				return false;

			BitSet<DockType> type = default;
			if (dockType != null && dockType.Length != 0)
			{
				type = BitSet<DockType>.FromStringsNoAlloc(dockType);
				if (!manager.DockingPossible(type, forceEnter))
					return false;
			}

			var dock = manager.ClosestDock(null, type, forceEnter, ignoreOccupancy);
			if (!dock.HasValue)
				return false;

			Self.QueueActivity(new MoveToDock(Self, dock.Value.Actor, dock.Value.Trait));
			return true;
		}
	}
}
