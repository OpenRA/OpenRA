#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Requires `GpsWatcher` on the player actor.")]
	class GpsPowerInfo : SupportPowerInfo
	{
		[Desc("Delay in ticks between launching and revealing the map.")]
		public readonly int RevealDelay = 0;

		public readonly string DoorImage = "atek";

		[SequenceReference(nameof(DoorImage))]
		public readonly string DoorSequence = "active";

		[PaletteReference(nameof(DoorPaletteIsPlayerPalette))]
		[Desc("Palette to use for rendering the launch animation")]
		public readonly string DoorPalette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool DoorPaletteIsPlayerPalette = true;

		public readonly string SatelliteImage = "sputnik";

		[SequenceReference(nameof(SatelliteImage))]
		public readonly string SatelliteSequence = "idle";

		[PaletteReference(nameof(SatellitePaletteIsPlayerPalette))]
		[Desc("Palette to use for rendering the satellite projectile")]
		public readonly string SatellitePalette = "player";

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
				PlayLaunchSounds();

				w.Add(new SatelliteLaunch(self, info));
			});
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e) { RemoveGps(self); }

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self) { RemoveGps(self); }

		void RemoveGps(Actor self)
		{
			// Extra function just in case something needs to be added later
			owner.GpsRemove(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RemoveGps(self);
			owner = newOwner.PlayerActor.Trait<GpsWatcher>();
			owner.GpsAdd(self);
		}

		bool NoActiveRadar { get { return !self.World.ActorsHavingTrait<ProvidesRadar>(r => !r.IsTraitDisabled).Any(a => a.Owner == self.Owner); } }
		bool wasPaused;

		void ITick.Tick(Actor self)
		{
			if (!wasPaused && (IsTraitPaused || (info.RequiresActiveRadar && NoActiveRadar)))
			{
				wasPaused = true;
				RemoveGps(self);
			}
			else if (wasPaused && !IsTraitPaused && !(info.RequiresActiveRadar && NoActiveRadar))
			{
				wasPaused = false;
				owner.GpsAdd(self);
			}
		}
	}
}
