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

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits.Sound;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Tracks neutral and enemy actors' visibility and notifies the player.",
		"Attach this to the player actor. The actors to track need the 'AnnounceOnSeen' trait.")]
	class EnemyWatcherInfo : TraitInfo
	{
		[Desc("Interval in ticks between scanning for enemies.")]
		public readonly int ScanInterval = 25;

		[Desc("Minimal ticks in-between notifications.")]
		public readonly int NotificationInterval = 750;

		public override object Create(ActorInitializer init) { return new EnemyWatcher(this); }
	}

	class EnemyWatcher : ITick
	{
		readonly EnemyWatcherInfo info;
		readonly HashSet<Player> discoveredPlayers;

		bool announcedAny;
		int rescanInterval;
		int ticksBeforeNextNotification;
		HashSet<uint> lastKnownActorIds;
		HashSet<uint> visibleActorIds;
		HashSet<string> playedNotifications;

		public EnemyWatcher(EnemyWatcherInfo info)
		{
			lastKnownActorIds = new HashSet<uint>();
			discoveredPlayers = new HashSet<Player>();
			this.info = info;
			rescanInterval = 0;
			ticksBeforeNextNotification = 0;
		}

		// Here self is the player actor
		void ITick.Tick(Actor self)
		{
			// TODO: Make the AI handle such notifications and remove Owner.IsBot from this check
			// Disable notifications for AI and neutral players (creeps) and for spectators
			if (self.Owner.Shroud.Disabled || self.Owner.IsBot || !self.Owner.Playable || self.Owner.PlayerReference.Spectating)
				return;

			rescanInterval--;
			ticksBeforeNextNotification--;

			if (rescanInterval > 0)
				return;

			rescanInterval = info.ScanInterval;

			announcedAny = false;
			visibleActorIds = new HashSet<uint>();
			playedNotifications = new HashSet<string>();

			foreach (var actor in self.World.ActorsWithTrait<AnnounceOnSeen>())
			{
				// We don't want notifications for allied actors or actors disguised as such
				if (actor.Actor.AppearsFriendlyTo(self))
					continue;

				if (actor.Actor.IsDead || !actor.Actor.IsInWorld)
					continue;

				// The actor is not currently visible
				if (!actor.Actor.CanBeViewedByPlayer(self.Owner))
					continue;

				visibleActorIds.Add(actor.Actor.ActorID);

				// We already know about this actor
				if (lastKnownActorIds.Contains(actor.Actor.ActorID))
					continue;

				// Should we play a notification?
				var notificationId = $"{actor.Trait.Info.Notification} {actor.Trait.Info.TextNotification}";
				var playNotification = !playedNotifications.Contains(notificationId) && ticksBeforeNextNotification <= 0;

				// Notify the actor that he has been discovered
				foreach (var trait in actor.Actor.TraitsImplementing<INotifyDiscovered>())
					trait.OnDiscovered(actor.Actor, self.Owner, playNotification);

				var discoveredPlayer = actor.Actor.Owner;
				if (!discoveredPlayers.Contains(discoveredPlayer))
				{
					// Notify the actor's owner that he has been discovered
					foreach (var trait in discoveredPlayer.PlayerActor.TraitsImplementing<INotifyDiscovered>())
						trait.OnDiscovered(actor.Actor, self.Owner, false);

					discoveredPlayers.Add(discoveredPlayer);
				}

				if (!playNotification)
					continue;

				playedNotifications.Add(notificationId);
				announcedAny = true;
			}

			if (announcedAny)
				ticksBeforeNextNotification = info.NotificationInterval;

			lastKnownActorIds = visibleActorIds;
		}
	}
}
