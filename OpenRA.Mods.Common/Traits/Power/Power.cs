#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Power
{
	public class PowerInfo : ITraitInfo
	{
		[Desc("If negative, it will drain power. If positive, it will provide power.")]
		public readonly int Amount = 0;

		public object Create(ActorInitializer init) { return new Power(init.self, this); }
	}

	public class Power : INotifyOwnerChanged
	{
		readonly PowerInfo info;
		readonly Lazy<IPowerModifier[]> powerModifiers;

		public PowerManager PlayerPower { get; private set; }

		public int GetCurrentPower()
		{
			return Util.ApplyPercentageModifiers(info.Amount, powerModifiers.Value.Select(m => m.GetPowerModifier()));
		}

		public Power(Actor self, PowerInfo info)
		{
			this.info = info;
			PlayerPower = self.Owner.PlayerActor.Trait<PowerManager>();
			powerModifiers = Exts.Lazy(() => self.TraitsImplementing<IPowerModifier>().ToArray());
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			PlayerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
