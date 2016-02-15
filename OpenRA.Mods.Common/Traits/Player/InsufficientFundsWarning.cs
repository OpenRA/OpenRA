#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides the player with an audible warning when they run out of money while producing.")]
	public class InsufficientFundsWarningInfo : ITraitInfo, Requires<PlayerResourcesInfo>
	{
		[Desc("The speech to play for the warning.")]
		public readonly string Notification = "InsufficientFunds";

		public object Create(ActorInitializer init) { return new InsufficientFundsWarning(this); }
	}

	public class InsufficientFundsWarning : INotifyInsufficientFunds
	{
		readonly InsufficientFundsWarningInfo info;

		bool played;

		public InsufficientFundsWarning(InsufficientFundsWarningInfo info)
		{
			this.info = info;
		}

		void INotifyInsufficientFunds.InsufficientFunds(Actor self)
		{
			Game.RunAfterTick(() =>
			{
				if (played)
					return;

				played = true;
				var owner = self.Owner;
				Game.Sound.PlayNotification(self.World.Map.Rules, owner, "Speech", info.Notification, owner.Faction.InternalName);
			});
		}

		void INotifyInsufficientFunds.SufficientFunds(Actor self)
		{
			played = false;
		}
	}
}
