#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class SupportPowerInfo : ITraitInfo
	{
		public readonly int ChargeTime = 0;
		public readonly string Icon = null;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly bool AllowMultiple = false;
		public readonly bool OneShot = false;

		[Desc("If set to true, the support power will be fully charged when it becomes available. " +
			"Normal rules apply for subsequent charges.")]
		public readonly bool StartFullyCharged = false;
		public readonly string[] Prerequisites = { };

		public readonly string BeginChargeSound = null;
		public readonly string EndChargeSound = null;
		public readonly string SelectTargetSound = null;
		public readonly string InsufficientPowerSound = null;
		public readonly string LaunchSound = null;
		public readonly string IncomingSound = null;

		public readonly bool DisplayTimer = false;

		[Desc("Beacons are only supported on the Airstrike and Nuke powers")]
		public readonly bool DisplayBeacon = false;
		public readonly string BeaconPalettePrefix = "player";
		public readonly string BeaconPoster = null;
		public readonly string BeaconPosterPalette = "chrome";

		public readonly bool DisplayRadarPing = false;
		public readonly int RadarPingDuration = 5 * 25;

		public readonly string OrderName;
		public abstract object Create(ActorInitializer init);

		public SupportPowerInfo() { OrderName = GetType().Name + "Order"; }
	}

	public class SupportPower
	{
		public readonly Actor Self;
		public readonly SupportPowerInfo Info;
		protected RadarPing ping;

		public SupportPower(Actor self, SupportPowerInfo info)
		{
			Info = info;
			Self = self;
		}

		public virtual void Charging(Actor self, string key)
		{
			Sound.PlayToPlayer(self.Owner, Info.BeginChargeSound);
		}

		public virtual void Charged(Actor self, string key)
		{
			Sound.PlayToPlayer(self.Owner, Info.EndChargeSound);
		}

		public virtual void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			if (Info.DisplayRadarPing && manager.RadarPings != null)
			{
				ping = manager.RadarPings.Value.Add(
					() => order.Player.IsAlliedWith(self.World.RenderPlayer),
					self.World.Map.CenterOfCell(order.TargetLocation),
					order.Player.Color.RGB,
					Info.RadarPingDuration);
			}
		}

		public virtual IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.Self.Owner, Info.SelectTargetSound);
			return new SelectGenericPowerTarget(order, manager, "ability", MouseButton.Left);
		}
	}
}
