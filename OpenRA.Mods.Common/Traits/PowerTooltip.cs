#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Shown power info on the build palette widget.")]
	public class PowerTooltipInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PowerTooltip(init.Self); }
	}

	public class PowerTooltip : IProvideTooltipInfo, INotifyOwnerChanged
	{
		readonly Actor self;
		PowerManager powerManager;
		DeveloperMode developerMode;

		public PowerTooltip(Actor self)
		{
			this.self = self;
			powerManager = self.Owner.PlayerActor.Trait<PowerManager>();
			developerMode = self.Owner.PlayerActor.Trait<DeveloperMode>();
		}

		public bool IsTooltipVisible(Player forPlayer)
		{
			return forPlayer == self.Owner;
		}

		public string TooltipText
		{
			get
			{
				return "Power Usage: {0}{1}".F(powerManager.PowerDrained, developerMode.UnlimitedPower ? "" : "/" + powerManager.PowerProvided);
			}
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			powerManager = newOwner.PlayerActor.Trait<PowerManager>();
			developerMode = newOwner.PlayerActor.Trait<DeveloperMode>();
		}
	}
}
