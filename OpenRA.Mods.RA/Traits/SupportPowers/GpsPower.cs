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

	class GpsPower : SupportPower, INotifyKilled, INotifySold, INotifyOwnerChanged, ITick
	{
		readonly Actor self;
		readonly GpsPowerInfo info;
		GpsWatcher owner;

		public GpsPower(Actor self, GpsPowerInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
			owner = self.Owner.PlayerActor.Trait<GpsWatcher>();
			owner.GpsAdd(self);
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

				owner.Launch(self, info);
			});
		}

		public void Killed(Actor self, AttackInfo e) { RemoveGps(self); }

		public void Selling(Actor self) { }
		public void Sold(Actor self) { RemoveGps(self); }

		void RemoveGps(Actor self)
		{
			// Extra function just in case something needs to be added later
			owner.GpsRemove(self);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RemoveGps(self);
			owner = newOwner.PlayerActor.Trait<GpsWatcher>();
			owner.GpsAdd(self);
		}

		bool NoActiveRadar { get { return !self.World.ActorsHavingTrait<ProvidesRadar>(r => r.IsActive).Any(a => a.Owner == self.Owner); } }
		bool wasDisabled;

		public void Tick(Actor self)
		{
			if (!wasDisabled && (self.IsDisabled() || (info.RequiresActiveRadar && NoActiveRadar)))
			{
				wasDisabled = true;
				RemoveGps(self);
			}
			else if (wasDisabled && !self.IsDisabled() && !(info.RequiresActiveRadar && NoActiveRadar))
			{
				wasDisabled = false;
				owner.GpsAdd(self);
			}
		}
	}
}
