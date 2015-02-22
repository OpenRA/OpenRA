#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Tracks neutral and enemy actors' visibility and notifies the player.",
		"Attach this to the player actor.")]
	class EnemyWatcherInfo : ITraitInfo
	{
		[Desc("Interval in ticks between scanning for enemies.")]
		public readonly int ScanInterval = 25;

		[Desc("Minimal ticks in-between notifications.")]
		public readonly int NotificationInterval = 750;

		public object Create(ActorInitializer init) { return new EnemyWatcher(init.Self, this); }
	}

	class EnemyWatcher : ITick
	{
		readonly EnemyWatcherInfo info;
		readonly Lazy<RadarPings> radarPings;

		bool announcedAny;
		int rescanInterval;
		int ticksBeforeNextNotification;
		HashSet<uint> lastKnownActorIds;
		HashSet<uint> visibleActorIds;
		HashSet<string> playedNotifications;

		public EnemyWatcher(Actor self, EnemyWatcherInfo info)
		{
			lastKnownActorIds = new HashSet<uint>();
			this.info = info;
			rescanInterval = info.ScanInterval;
			ticksBeforeNextNotification = info.NotificationInterval;
			radarPings = Exts.Lazy(() => self.World.WorldActor.Trait<RadarPings>());
		}

		public void Tick(Actor self)
		{
			// TODO: Make the AI handle such notifications and remove Owner.IsBot from this check
			// Disable notifications for AI and neutral players (creeps) and for spectators
			if (self.Owner.Shroud.Disabled || self.Owner.IsBot || !self.Owner.Playable || self.Owner.PlayerReference.Spectating)
				return;

			rescanInterval--;
			ticksBeforeNextNotification--;

			if (rescanInterval > 0 || ticksBeforeNextNotification > 0)
				return;

			rescanInterval = info.ScanInterval;

			announcedAny = false;
			visibleActorIds = new HashSet<uint>();
			playedNotifications = new HashSet<string>();

			foreach (var actor in self.World.ActorsWithTrait<AnnounceOnSeen>())
			{
				// We only care about enemy actors (creeps should be enemies)
				if ((actor.Actor.EffectiveOwner != null && self.Owner.Stances[actor.Actor.EffectiveOwner.Owner] != Stance.Enemy)
				 || self.Owner.Stances[actor.Actor.Owner] != Stance.Enemy)
					continue;

				// The actor is not currently visible
				if (!self.Owner.Shroud.IsVisible(actor.Actor))
					continue;

				visibleActorIds.Add(actor.Actor.ActorID);

				// We already know about this actor
				if (lastKnownActorIds.Contains(actor.Actor.ActorID))
					continue;

				// We have already played this type of notification
				if (playedNotifications.Contains(actor.Trait.Info.Notification))
					continue;

				if (self.Owner == self.World.RenderPlayer)
					Announce(self, actor);
			}

			if (announcedAny)
				ticksBeforeNextNotification = info.NotificationInterval;

			lastKnownActorIds = visibleActorIds;
		}

		void Announce(Actor self, TraitPair<AnnounceOnSeen> announce)
		{
			// Audio notification
			Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", announce.Trait.Info.Notification, self.Owner.Country.Race);

			// Radar notificaion
			if (announce.Trait.Info.PingRadar && radarPings.Value != null)
				radarPings.Value.Add(() => true, announce.Actor.CenterPosition, Color.Red, 50);

			playedNotifications.Add(announce.Trait.Info.Notification);
			announcedAny = true;
		}
	}
}