#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;

namespace OpenRA.Mods.Common.Traits
{
	public class DockClientInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DockClient(init, this); }
	}

	public enum DockState
	{
		NotAssigned,
		WaitAssigned,
		ServiceAssigned
	}

	// When dockmanager manages docked units, these units require dock client trait.
	public class DockClient
	{
		public readonly DockClientInfo Info;
		public readonly Actor self;

		public Dock CurrentDock;
		public Activity PostUndockActivity;

		public DockState DockState = DockState.NotAssigned;

		public DockClient(ActorInitializer init, DockClientInfo info)
		{
			Info = info;
			self = init.Self;
		}
	}
}
