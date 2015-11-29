#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows granting upgrades to actors from Lua scripts.")]
	public class UpgradesCacheInfo : ITraitInfo
	{
		[UpgradeGrantedReference]
		[Desc("Upgrades that can be granted/revoked from the scripts.")]
		public readonly HashSet<string> Scriptable = new HashSet<string>();

		[UpgradeUsedReference]
		[Desc("Upgrades not to check whether used.")]
		public readonly HashSet<string> IgnoredAsUnused = new HashSet<string>();

		[UpgradeGrantedReference]
		[Desc("Upgrades not to check whether granted.")]
		public readonly HashSet<string> IgnoredAsNotGranted = new HashSet<string>();

		public object Create(ActorInitializer init) { return new UpgradesCache(this); }
	}

	public sealed class UpgradesCache
	{
		public readonly UpgradesCacheInfo Info;

		public UpgradesCache(UpgradesCacheInfo info)
		{
			Info = info;
		}
	}
}
