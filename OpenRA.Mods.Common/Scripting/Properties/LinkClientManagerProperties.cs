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
	public class LinkClientManagerProperties : ScriptActorProperties, Requires<LinkClientManagerInfo>
	{
		readonly LinkClientManager manager;

		public LinkClientManagerProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			manager = self.Trait<LinkClientManager>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Can we link to this actor? If linkType is undefined check all valid hosts.")]
		public bool CanLinkTo(Actor target, string[] linkType = default, bool forceEnter = false, bool ignoreOccupancy = false)
		{
			if (target == null)
				throw new LuaException("Target actor is null.");

			BitSet<LinkType> type = default;
			if (linkType != null && linkType.Length != 0)
			{
				type = BitSet<LinkType>.FromStringsNoAlloc(linkType);
				if (!manager.LinkingPossible(type, forceEnter))
					return false;
			}

			return manager.CanLinkTo(target, type, forceEnter, ignoreOccupancy);
		}

		[ScriptActorPropertyActivity]
		[Desc("Link to the actor. If linkType is undefined link to all validhosts.")]
		public void Link(Actor target, string[] linkType = default, bool forceEnter = false, bool ignoreOccupancy = true)
		{
			if (target == null)
				throw new LuaException("Target actor is null.");

			if (manager.IsTraitDisabled)
				return;

			BitSet<LinkType> type = default;
			if (linkType != null && linkType.Length != 0)
			{
				type = BitSet<LinkType>.FromStringsNoAlloc(linkType);
				if (!manager.LinkingPossible(type, forceEnter))
					return;
			}

			var docks = manager.AvailableLinkHosts(target, type, forceEnter, ignoreOccupancy).ToList();
			if (docks.Count == 0)
				return;

			var dock = docks.ClosestLinkHost(Self, manager);
			if (!dock.HasValue)
				return;

			Self.QueueActivity(new MoveToDock(Self, manager, dock.Value.Actor, dock.Value.Trait));
		}

		[ScriptActorPropertyActivity]
		[Desc("Find and link to the closest LinkHost, if linkType is undefined search for all valid docks. "
			+ "Returns true if docking activity was successfully queued.")]
		public bool LinkToClosestHost(string[] linkType = default, bool forceEnter = false, bool ignoreOccupancy = true)
		{
			if (manager.IsTraitDisabled)
				return false;

			BitSet<LinkType> type = default;
			if (linkType != null && linkType.Length != 0)
			{
				type = BitSet<LinkType>.FromStringsNoAlloc(linkType);
				if (!manager.LinkingPossible(type, forceEnter))
					return false;
			}

			var dock = manager.ClosestLinkHost(null, type, forceEnter, ignoreOccupancy);
			if (!dock.HasValue)
				return false;

			Self.QueueActivity(new MoveToDock(Self, manager, dock.Value.Actor, dock.Value.Trait));
			return true;
		}
	}
}
