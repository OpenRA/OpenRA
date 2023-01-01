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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckNotifications : ILintRulesPass, ILintServerMapPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			Run(emitError, rules);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, mapRules);
		}

		void Run(Action<string> emitError, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<NotificationReferenceAttribute>()))
					{
						string type = null;
						var notificationReference = field.GetCustomAttributes<NotificationReferenceAttribute>(true).First();
						if (!string.IsNullOrEmpty(notificationReference.NotificationTypeFieldName))
						{
							var fieldInfo = fields.First(f => f.Name == notificationReference.NotificationTypeFieldName);
							type = (string)fieldInfo.GetValue(traitInfo);
						}
						else
							type = notificationReference.NotificationType;

						var notifications = LintExts.GetFieldValues(traitInfo, field);
						foreach (var notification in notifications)
						{
							if (string.IsNullOrEmpty(notification))
								continue;

							if (string.IsNullOrEmpty(type) || !rules.Notifications.TryGetValue(type.ToLowerInvariant(), out var soundInfo) ||
								!soundInfo.Notifications.ContainsKey(notification))
								emitError($"Undefined notification reference {type ?? "(null)"}.{notification} detected at {traitInfo.GetType().Name} for {actorInfo.Key}");
						}
					}
				}
			}
		}
	}
}
