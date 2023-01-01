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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class UseMillisecondsForSounds : UpdateRule
	{
		public override string Name => "Convert announcement/notifier intervals to real (milli)seconds.";

		public override string Description =>
			"AnnounceOnKill.Interval, Harvester- and BaseAttackNotifier.NotifyInterval and\n" +
			"ResourceStorageWarning.AdviceInterval were using 'fake' seconds (value * 25 ticks).\n" +
			"PowerManager.AdviceInterval and PlayerResources.InsufficientFundsNotificationDelay were using ticks.\n" +
			"Converted all of those to use real milliseconds instead.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var announce in actorNode.ChildrenMatching("AnnounceOnKill"))
			{
				var intervalNode = announce.LastChildMatching("Interval");
				if (intervalNode != null)
				{
					var interval = intervalNode.NodeValue<int>();
					intervalNode.Value.Value = FieldSaver.FormatValue(interval * 1000);
				}
			}

			foreach (var notifier in actorNode.ChildrenMatching("BaseAttackNotifier"))
			{
				var notifyIntervalNode = notifier.LastChildMatching("NotifyInterval");
				if (notifyIntervalNode != null)
				{
					var notifyInterval = notifyIntervalNode.NodeValue<int>();
					notifyIntervalNode.Value.Value = FieldSaver.FormatValue(notifyInterval * 1000);
				}
			}

			foreach (var notifier in actorNode.ChildrenMatching("HarvesterAttackNotifier"))
			{
				var notifyIntervalNode = notifier.LastChildMatching("NotifyInterval");
				if (notifyIntervalNode != null)
				{
					var notifyInterval = notifyIntervalNode.NodeValue<int>();
					notifyIntervalNode.Value.Value = FieldSaver.FormatValue(notifyInterval * 1000);
				}
			}

			foreach (var rsw in actorNode.ChildrenMatching("ResourceStorageWarning"))
			{
				var adviceIntervalNode = rsw.LastChildMatching("AdviceInterval");
				if (adviceIntervalNode != null)
				{
					var adviceInterval = adviceIntervalNode.NodeValue<int>();
					adviceIntervalNode.Value.Value = FieldSaver.FormatValue(adviceInterval * 1000);
				}
			}

			foreach (var pm in actorNode.ChildrenMatching("PowerManager"))
			{
				var adviceIntervalNode = pm.LastChildMatching("AdviceInterval");
				if (adviceIntervalNode != null)
				{
					var adviceInterval = adviceIntervalNode.NodeValue<int>();
					adviceIntervalNode.Value.Value = FieldSaver.FormatValue(adviceInterval * 40);
				}
			}

			foreach (var pr in actorNode.ChildrenMatching("PlayerResources"))
			{
				var noFundsIntervalNode = pr.LastChildMatching("InsufficientFundsNotificationDelay");
				if (noFundsIntervalNode != null)
				{
					var noFundsInterval = noFundsIntervalNode.NodeValue<int>();
					noFundsIntervalNode.Value.Value = FieldSaver.FormatValue(noFundsInterval * 40);
					noFundsIntervalNode.RenameKey("InsufficientFundsNotificationInterval");
				}
			}

			yield break;
		}
	}
}
