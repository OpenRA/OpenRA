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
	sealed class RenameInfiltrationNotifications : UpdateRule
	{
		public override string Name => "Renamed InfiltrateForCash Notification to InfiltratedNotification.";

		public override string Description => "The InfiltrateForCash Notification has been renamed to be in line with new notification properties added.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var rp in actorNode.ChildrenMatching("InfiltrateForCash"))
				rp.RenameChildrenMatching("Notification", "InfiltratedNotification");

			yield break;
		}
	}
}
