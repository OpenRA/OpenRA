#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This must be attached to player in order for TeleportNetwork to work.")]
	public class TeleportNetworkManagerInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Type of TeleportNetwork that pairs up, in order for it to work.")]
		public string Type;

		public object Create(ActorInitializer init) { return new TeleportNetworkManager(init, this); }
	}

	public class TeleportNetworkManager
	{
		public readonly string Type;
		public int Count = 0;
		public Actor PrimaryActor = null;

		public TeleportNetworkManager(ActorInitializer init, TeleportNetworkManagerInfo info)
		{
			this.Type = info.Type;
		}
	}
}
