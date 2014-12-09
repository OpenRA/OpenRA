#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Disables the actor when a power outage is triggered (see `InfiltrateForPowerOutage` for more information).")]
	public class AffectedByPowerOutageInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new AffectedByPowerOutage(init.self); }
	}

	public class AffectedByPowerOutage : INotifyOwnerChanged, ISelectionBar, IPowerModifier, IDisable
	{
		PowerManager playerPower;

		public AffectedByPowerOutage(Actor self)
		{
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		public float GetValue()
		{
			if (playerPower.PowerOutageRemainingTicks <= 0)
				return 0;

			return (float)playerPower.PowerOutageRemainingTicks / playerPower.PowerOutageTotalTicks;
		}

		public Color GetColor()
		{
			return Color.Yellow;
		}

		public int GetPowerModifier()
		{
			return playerPower.PowerOutageRemainingTicks > 0 ? 0 : 100;
		}

		public bool Disabled
		{
			get { return playerPower.PowerOutageRemainingTicks > 0; }
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
