#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
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

		public object Create(ActorInitializer init) { return new EnemyWatcher(this); }
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
			rescanInterval = info.ScanInterval;
			ticksBeforeNextNotification = info.NotificationInterval;
		}

		// Here self is the player actor
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
				// We don't want notifications for allied actors
				if ((actor.Actor.EffectiveOwner != null && self.Owner.Stances[actor.Actor.EffectiveOwner.Owner] == Stance.Ally)
				 || self.Owner.Stances[actor.Actor.Owner] == Stance.Ally)
					continue;

				if (actor.Actor.IsDead || !actor.Actor.IsInWorld)
					continue;

				// The actor is not currently visible
				if (!self.Owner.Shroud.IsVisible(actor.Actor))
					continue;

				visibleActorIds.Add(actor.Actor.ActorID);

				// We already know about this actor
				if (lastKnownActorIds.Contains(actor.Actor.ActorID))
					continue;

				var notificationPlayed = playedNotifications.Contains(actor.Trait.Info.Notification);

				// Notify the actor that he has been discovered
				foreach (var trait in actor.Actor.TraitsImplementing<INotifyDiscovered>())
					trait.OnDiscovered(actor.Actor, self.Owner, !notificationPlayed);

				var discoveredPlayer = actor.Actor.Owner;
				if (!discoveredPlayers.Contains(discoveredPlayer))
				{
					// Notify the actor's owner that he has been discovered
					foreach (var trait in discoveredPlayer.PlayerActor.TraitsImplementing<INotifyDiscovered>())
						trait.OnDiscovered(actor.Actor, self.Owner, false);

					discoveredPlayers.Add(discoveredPlayer);
				}

				// We have already played this type of notification
				if (notificationPlayed)
					continue;

				playedNotifications.Add(actor.Trait.Info.Notification);
				announcedAny = true;
			}

			if (announcedAny)
				ticksBeforeNextNotification = info.NotificationInterval;

			lastKnownActorIds = visibleActorIds;
		}
	}
}