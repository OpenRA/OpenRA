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
	public abstract class SupportPowerInfo : UpgradableTraitInfo
	{
		[Desc("Measured in seconds.")]
		public readonly int ChargeTime = 0;
		public readonly string Icon = null;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly bool AllowMultiple = false;
		public readonly bool OneShot = false;

		[Desc("Cursor to display for using this support power.")]
		public readonly string Cursor = "ability";

		[Desc("If set to true, the support power will be fully charged when it becomes available. " +
			"Normal rules apply for subsequent charges.")]
		public readonly bool StartFullyCharged = false;
		public readonly string[] Prerequisites = { };

		public readonly string BeginChargeSound = null;
		public readonly string BeginChargeSpeechNotification = null;
		public readonly string EndChargeSound = null;
		public readonly string EndChargeSpeechNotification = null;
		public readonly string SelectTargetSound = null;
		public readonly string SelectTargetSpeechNotification = null;
		public readonly string InsufficientPowerSound = null;
		public readonly string InsufficientPowerSpeechNotification = null;
		public readonly string LaunchSound = null;
		public readonly string LaunchSpeechNotification = null;
		public readonly string IncomingSound = null;
		public readonly string IncomingSpeechNotification = null;

		[Desc("Defines to which players the timer is shown.")]
		public readonly Stance DisplayTimerStances = Stance.None;

		[Desc("Palette used for the icon.")]
		[PaletteReference] public readonly string IconPalette = "chrome";

		[Desc("Beacons are only supported on the Airstrike, Paratroopers, and Nuke powers")]
		public readonly bool DisplayBeacon = false;

		public readonly bool BeaconPaletteIsPlayerPalette = true;
		[PaletteReference("BeaconPaletteIsPlayerPalette")] public readonly string BeaconPalette = "player";

		public readonly string BeaconImage = "beacon";
		[SequenceReference("BeaconImage")] public readonly string BeaconPoster = null;
		[PaletteReference] public readonly string BeaconPosterPalette = "chrome";
		[SequenceReference("BeaconImage")] public readonly string ClockSequence = "clock";

		[SequenceReference("BeaconImage")] public readonly string ArrowSequence = "arrow";
		[SequenceReference("BeaconImage")] public readonly string CircleSequence = "circles";

		public readonly bool DisplayRadarPing = false;

		[Desc("Measured in ticks.")]
		public readonly int RadarPingDuration = 5 * 25;

		public readonly string OrderName;

		public SupportPowerInfo() { OrderName = GetType().Name + "Order"; }
	}

	public class SupportPower : UpgradableTrait<SupportPowerInfo>
	{
		public readonly Actor Self;
		readonly SupportPowerInfo info;
		protected RadarPing ping;

		public SupportPower(Actor self, SupportPowerInfo info)
			: base(info)
		{
			Self = self;
			this.info = info;
		}

		public virtual void Charging(Actor self, string key)
		{
			Game.Sound.PlayToPlayer(self.Owner, Info.BeginChargeSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.BeginChargeSpeechNotification, self.Owner.Faction.InternalName);
		}

		public virtual void Charged(Actor self, string key)
		{
			Game.Sound.PlayToPlayer(self.Owner, Info.EndChargeSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.EndChargeSpeechNotification, self.Owner.Faction.InternalName);
		}

		public virtual void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(manager.Self.Owner, Info.SelectTargetSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);
			self.World.OrderGenerator = new SelectGenericPowerTarget(order, manager, info.Cursor, MouseButton.Left);
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

		public virtual void PlayLaunchSounds()
		{
			var renderPlayer = Self.World.RenderPlayer;
			var isAllied = Self.Owner.IsAlliedWith(renderPlayer);
			Game.Sound.Play(isAllied ? Info.LaunchSound : Info.IncomingSound);

			// IsAlliedWith returns true if renderPlayer is null, so we are safe here.
			var toPlayer = isAllied ? renderPlayer ?? Self.Owner : renderPlayer;
			var speech = isAllied ? Info.LaunchSpeechNotification : Info.IncomingSpeechNotification;
			Game.Sound.PlayNotification(Self.World.Map.Rules, toPlayer, "Speech", speech, toPlayer.Faction.InternalName);
		}
	}
}
