#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class RemoveIDisable : UpdateRule
	{
		public override string Name { get { return "Remove 'IDisabled'"; } }
		public override string Description
		{
			get
			{
				return "'Actor.IsDisabled' has been removed in favor of pausing/disabling traits via conditions.";
			}
		}

		bool displayed;

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var doc = actorNode.LastChildMatching("DisableOnCondition");
			var grant = actorNode.LastChildMatching("GrantConditionOnDisabled");

			if (!displayed && (doc != null || grant != null))
			{
				displayed = true;
				yield return "Actor.IsDisabled has been removed in favor of pausing/disabling traits via conditions.\n" +
					"DisableOnCondition and GrantConditionOnDisabled were stop-gap solutions that have been removed along with it.\n" +
					"You'll have to use RequiresCondition or PauseOnCondition on individual traits to 'disable' actors.";
			}

			actorNode.RemoveNodes("DisableOnCondition");
			actorNode.RemoveNodes("GrantConditionOnDisabled");
			yield break;
		}
	}
}
