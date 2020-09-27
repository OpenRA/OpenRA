#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class SupportPowerInfo : PausableConditionalTraitInfo
	{
		[Desc("Measured in ticks.")]
		public readonly int ChargeInterval = 0;

		public readonly string IconImage = "icon";

		[SequenceReference(nameof(IconImage))]
		[Desc("Icon sprite displayed in the support power palette.")]
		public readonly string Icon = null;

		[PaletteReference]
		[Desc("Palette used for the icon.")]
		public readonly string IconPalette = "chrome";

		public readonly string Description = "";
		public readonly string LongDesc = "";

		[Desc("Allow multiple instances of the same support power.")]
		public readonly bool AllowMultiple = false;

		[Desc("Allow this to be used only once.")]
		public readonly bool OneShot = false;

		[Desc("Cursor to display for using this support power.")]
		public readonly string Cursor = "ability";

		[Desc("If set to true, the support power will be fully charged when it becomes available. " +
			"Normal rules apply for subsequent charges.")]
		public readonly bool StartFullyCharged = false;
		public readonly string[] Prerequisites = { };

		public readonly string BeginChargeSound = null;

		[NotificationReference("Speech")]
		public readonly string BeginChargeSpeechNotification = null;

		public readonly string EndChargeSound = null;

		[NotificationReference("Speech")]
		public readonly string EndChargeSpeechNotification = null;

		public readonly string SelectTargetSound = null;

		[NotificationReference("Speech")]
		public readonly string SelectTargetSpeechNotification = null;

		public readonly string InsufficientPowerSound = null;

		[NotificationReference("Speech")]
		public readonly string InsufficientPowerSpeechNotification = null;

		public readonly string LaunchSound = null;

		[NotificationReference("Speech")]
		public readonly string LaunchSpeechNotification = null;

		public readonly string IncomingSound = null;

		[NotificationReference("Speech")]
		public readonly string IncomingSpeechNotification = null;

		[Desc("Defines to which players the timer is shown.")]
		public readonly PlayerRelationship DisplayTimerRelationships = PlayerRelationship.None;

		[Desc("Beacons are only supported on the Airstrike, Paratroopers, and Nuke powers")]
		public readonly bool DisplayBeacon = false;

		public readonly bool BeaconPaletteIsPlayerPalette = true;

		[PaletteReference(nameof(BeaconPaletteIsPlayerPalette))]
		public readonly string BeaconPalette = "player";

		public readonly string BeaconImage = "beacon";

		[SequenceReference(nameof(BeaconImage))]
		public readonly string BeaconPoster = null;

		[PaletteReference]
		public readonly string BeaconPosterPalette = "chrome";

		[SequenceReference(nameof(BeaconImage))]
		public readonly string ClockSequence = null;

		[SequenceReference(nameof(BeaconImage))]
		public readonly string BeaconSequence = null;

		[SequenceReference(nameof(BeaconImage))]
		public readonly string ArrowSequence = null;

		[SequenceReference(nameof(BeaconImage))]
		public readonly string CircleSequence = null;

		[Desc("Delay after launch, measured in ticks.")]
		public readonly int BeaconDelay = 0;

		public readonly bool DisplayRadarPing = false;

		[Desc("Measured in ticks.")]
		public readonly int RadarPingDuration = 5 * 25;

		public readonly string OrderName;

		[Desc("Sort order for the support power palette. Smaller numbers are presented earlier.")]
		public readonly int SupportPowerPaletteOrder = 9999;

		public SupportPowerInfo() { OrderName = GetType().Name + "Order"; }
	}

	public class SupportPower : PausableConditionalTrait<SupportPowerInfo>
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

		public virtual SupportPowerInstance CreateInstance(string key, SupportPowerManager manager)
		{
			return new SupportPowerInstance(key, info, manager);
		}

		public virtual void Charging(Actor self, string key)
		{
			Game.Sound.PlayToPlayer(SoundType.UI, self.Owner, Info.BeginChargeSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.BeginChargeSpeechNotification, self.Owner.Faction.InternalName);
		}

		public virtual void Charged(Actor self, string key)
		{
			Game.Sound.PlayToPlayer(SoundType.UI, self.Owner, Info.EndChargeSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.EndChargeSpeechNotification, self.Owner.Faction.InternalName);

			foreach (var notify in self.TraitsImplementing<INotifySupportPower>())
				notify.Charged(self);
		}

		public virtual void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectGenericPowerTarget(order, manager, info.Cursor, MouseButton.Left);
		}

		public virtual void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			if (Info.DisplayRadarPing && manager.RadarPings != null)
			{
				ping = manager.RadarPings.Value.Add(
					() => order.Player.IsAlliedWith(self.World.RenderPlayer),
					order.Target.CenterPosition,
					order.Player.Color,
					Info.RadarPingDuration);
			}

			foreach (var notify in self.TraitsImplementing<INotifySupportPower>())
				notify.Activated(self);
		}

		public virtual void PlayLaunchSounds()
		{
			var renderPlayer = Self.World.RenderPlayer;
			var isAllied = Self.Owner.IsAlliedWith(renderPlayer);
			Game.Sound.Play(SoundType.UI, isAllied ? Info.LaunchSound : Info.IncomingSound);

			// IsAlliedWith returns true if renderPlayer is null, so we are safe here.
			var toPlayer = isAllied ? renderPlayer ?? Self.Owner : renderPlayer;
			var speech = isAllied ? Info.LaunchSpeechNotification : Info.IncomingSpeechNotification;
			Game.Sound.PlayNotification(Self.World.Map.Rules, toPlayer, "Speech", speech, toPlayer.Faction.InternalName);
		}

		public IEnumerable<CPos> CellsMatching(CPos location, char[] footprint, CVec dimensions)
		{
			var index = 0;
			var x = location.X - (dimensions.X - 1) / 2;
			var y = location.Y - (dimensions.Y - 1) / 2;
			for (var j = 0; j < dimensions.Y; j++)
				for (var i = 0; i < dimensions.X; i++)
					if (footprint[index++] == 'x')
						yield return new CPos(x + i, y + j);
		}
	}
}
