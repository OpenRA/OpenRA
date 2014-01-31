#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public abstract class SupportPowerInfo : ITraitInfo
	{
		public readonly int ChargeTime = 0;
		public readonly string Icon = null;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly bool AllowMultiple = false;
		public readonly bool OneShot = false;

		public readonly string BeginChargeSound = null;
		public readonly string EndChargeSound = null;
		public readonly string SelectTargetSound = null;
		public readonly string InsufficientPowerSound = null;
		public readonly string LaunchSound = null;
		public readonly string IncomingSound = null;

		public readonly bool DisplayTimer = false;

		public readonly string OrderName;
		public abstract object Create(ActorInitializer init);

		public SupportPowerInfo() { OrderName = GetType().Name + "Order"; }
	}

	public class SupportPower
	{
		public readonly Actor self;
		public readonly SupportPowerInfo Info;

		public SupportPower(Actor self, SupportPowerInfo info)
		{
			Info = info;
			this.self = self;
		}

		public virtual void Charging(Actor self, string key)
		{
			Sound.PlayToPlayer(self.Owner, Info.BeginChargeSound);
		}

		public virtual void Charged(Actor self, string key)
		{
			Sound.PlayToPlayer(self.Owner, Info.EndChargeSound);
		}

		public virtual void Activate(Actor self, Order order) { }

		public virtual IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectGenericPowerTarget(order, manager, "ability", MouseButton.Left);
		}
	}
}
