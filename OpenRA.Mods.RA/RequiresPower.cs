#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RequiresPowerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new RequiresPower(init.self); }
	}

	class RequiresPower : IDisable
	{
		readonly Actor self;
		readonly PowerManager power;
		public RequiresPower( Actor self )
		{
			this.self = self;
			power = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		public bool Disabled
		{
			get { return power.IsPowered(self); }
		}
	}
}
