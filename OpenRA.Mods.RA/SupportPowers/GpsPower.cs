#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public override object Create(ActorInitializer init) { return new GpsPower(init.self, this); }
	}

	class GpsPower : SupportPower
	{
		public GpsPower(Actor self, GpsPowerInfo info) : base(self, info) { }

		public override void Charged(Actor self, string key)
		{
			self.Owner.PlayerActor.Trait<SupportPowerManager>().Powers[key].Activate(new Order());
		}
		
		public override void Activate(Actor self, Order order)
		{
			self.World.AddFrameEndTask(w =>
			{
				Sound.PlayToPlayer(self.Owner, Info.LaunchSound);

				w.Add(new SatelliteLaunch(self));
				w.Add(new DelayedAction((Info as GpsPowerInfo).RevealDelay * 25, 
					() => self.Owner.Shroud.Disabled = true));
			});
		}
	}
}
