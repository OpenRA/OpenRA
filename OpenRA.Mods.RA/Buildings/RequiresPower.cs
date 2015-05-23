﻿#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	class RequiresPowerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new RequiresPower(init.self); }
	}

	class RequiresPower : IDisable, INotifyCapture
	{
		PowerManager power;

		public RequiresPower( Actor self )
		{
			power = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		public bool Disabled
		{
			get { return power.PowerProvided < power.PowerDrained; }
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			power = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
