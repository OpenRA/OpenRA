#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Players will be notified when this actor becomes visible to them.")]
	public class AnnounceOnSeenInfo : ITraitInfo
	{
		[Desc("Should there be a radar ping on enemies' radar at the actor's location when they see him")]
		public readonly bool PingRadar = false;

		public readonly string Notification = "EnemyUnitSighted";

		public object Create(ActorInitializer init) { return new AnnounceOnSeen(this); }
	}

	public class AnnounceOnSeen
	{
		public readonly AnnounceOnSeenInfo Info;

		public AnnounceOnSeen(AnnounceOnSeenInfo info)
		{
			Info = info;
		}
	}
}