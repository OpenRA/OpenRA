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
	/// <summary>
	/// Replaces the BaseAttackNotifier with a new AttackNotifier that uses the
	/// new attack system.
	/// </summary>
	public class ReplaceBaseAttackNotifier : UpdateRule, IBeforeUpdateActors
	{
		public override string Name => "Replace BaseAttackNotifier with DamageNotifier";
		public override string Description => "Replaces the BaseAttackNotifier with a new DamageNotifier that uses BaseBuilding target type.";

		bool any = false;
		readonly HashSet<string> definedNotifications = [];

		IEnumerable<string> IBeforeUpdateActors.BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			any = false;
			foreach (var actor in resolvedActors)
				foreach (var notifier in actor.ChildrenMatching("BaseAttackNotifier"))
					if (notifier.LastChildMatching("Notification", false) != null)
						definedNotifications.Add(actor.Key + ":" + notifier.Key);

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var notifier in actorNode.ChildrenMatching("BaseAttackNotifier"))
			{
				any = true;
				notifier.AddNode("ValidTargets", "BaseBuilding");
				if (!definedNotifications.Contains(actorNode.Key + ":" + notifier.Key))
					notifier.AddNode("Notification", "BaseAttack");

				notifier.RenameKey("DamageNotifier");
			}

			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!any)
				yield break;

			yield return "BaseAttackNotifier has been replaced by DamageNotifier with ValidTargets: BaseBuilding" +
				"\nPlease replace the target type or add it to actor types as needed.";
		}
	}
}
