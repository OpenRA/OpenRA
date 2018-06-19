#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckNotifications : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<ITraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields().Where(f => f.HasAttribute<NotificationReferenceAttribute>());
					foreach (var field in fields)
					{
						var notifications = LintExts.GetFieldValues(traitInfo, field, emitError);
						foreach (var notification in notifications)
						{
							if (string.IsNullOrEmpty(notification))
								continue;

							CheckActorNotifications(actorInfo.Value, emitError, rules, notification);
						}
					}
				}
			}
		}

		void CheckActorNotifications(ActorInfo actorInfo, Action<string> emitError, Ruleset rules, string notification)
		{
			if (!rules.Notifications.ContainsKey(notification))
				return;

			var soundInfo = rules.Notifications[notification.ToLowerInvariant()];

			foreach (var traitInfo in actorInfo.TraitInfos<ITraitInfo>())
			{
				var fields = traitInfo.GetType().GetFields().Where(f => f.HasAttribute<NotificationReferenceAttribute>());
				foreach (var field in fields)
				{
					var notifications = LintExts.GetFieldValues(traitInfo, field, emitError);
					foreach (var notificationSound in notifications)
					{
						if (string.IsNullOrEmpty(notificationSound))
							continue;

						if (!soundInfo.Voices.Keys.Contains(notificationSound))
							emitError("Actor {0} using notification {1} does not define {2} notification required by {3}.".F(actorInfo.Name,
								notification,
								notificationSound, traitInfo));
					}
				}
			}
		}
	}
}
