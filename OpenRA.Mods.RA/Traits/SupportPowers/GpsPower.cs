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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Requires `GpsWatcher` on the player actor.")]
	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public readonly string DoorImage = "atek";
		[SequenceReference("DoorImage")] public readonly string DoorSequence = "active";

		[Desc("Palette to use for rendering the launch animation")]
		[PaletteReference("DoorPaletteIsPlayerPalette")] public readonly string DoorPalette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool DoorPaletteIsPlayerPalette = true;

		public readonly string SatelliteImage = "sputnik";
		[SequenceReference("SatelliteImage")] public readonly string SatelliteSequence = "idle";

		[Desc("Palette to use for rendering the satellite projectile")]
		[PaletteReference("SatellitePaletteIsPlayerPalette")] public readonly string SatellitePalette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool SatellitePaletteIsPlayerPalette = true;

		[Desc("Requires an actor with an online `ProvidesRadar` to show GPS dots.")]
		public readonly bool RequiresActiveRadar = true;

		public override object Create(ActorInitializer init) { return new GpsPower(init.Self, this); }
	}

	class GpsPower : SupportPower, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged, ITick
	{
		readonly Actor self;
		readonly GpsPowerInfo info;

		GpsWatcher watcher;
		bool wasEnabled;

		public GpsPower(Actor self, GpsPowerInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
			watcher = self.Owner.PlayerActor.Trait<GpsWatcher>();
		}

		public override void Charged(Actor self, string key)
		{
			self.Owner.PlayerActor.Trait<SupportPowerManager>().Powers[key].Activate(new Order());
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			self.World.AddFrameEndTask(w =>
			{
				Game.Sound.PlayToPlayer(self.Owner, Info.LaunchSound);
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					Info.LaunchSpeechNotification, self.Owner.Faction.InternalName);

				w.Add(new SatelliteLaunch(self, info));

				watcher.Launch(self, info);
			});
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Note: watcher registration is already handled by removal/addition from world
			watcher = newOwner.PlayerActor.Trait<GpsWatcher>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			// Registration will happen in the next tick if needed
			wasEnabled = false;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (wasEnabled)
				watcher.RemoveSource(self);
		}

		bool NoActiveRadar { get { return !self.World.ActorsHavingTrait<ProvidesRadar>(r => r.IsActive).Any(a => a.Owner == self.Owner); } }

		void ITick.Tick(Actor self)
		{
			var isEnabled = !(self.IsDisabled() || (info.RequiresActiveRadar && NoActiveRadar));
			if (!wasEnabled && isEnabled)
				watcher.AddSource(self);
			else if (wasEnabled && !isEnabled)
				watcher.RemoveSource(self);

			wasEnabled = isEnabled;
		}
	}
}
