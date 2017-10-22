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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the bounty lobby option.")]
	public class GlobalBountyInfo : ITraitInfo, ILobbyOptions
	{
		[Desc("Default value of the bounty checkbox in the lobby.")]
		public readonly bool BountyEnabled = true;

		[Desc("Prevent the Bounty enabled state from being changed in the lobby.")]
		public readonly bool BountyLocked = false;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("bounty", "Bounty", BountyEnabled, BountyLocked);
		}

		public object Create(ActorInitializer init) { return new GlobalBounty(this); }
	}

	public class GlobalBounty : INotifyCreated
	{
		readonly GlobalBountyInfo info;
		public bool Bounty { get; private set; }

		public GlobalBounty(GlobalBountyInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Bounty = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("bounty", info.BountyEnabled);
		}
	}
}
