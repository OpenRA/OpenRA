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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ReplaceSpecialMoveConsiderations : UpdateRule
	{
		public override string Name { get { return "Replaced special-case movement type considerations"; } }
		public override string Description
		{
			get
			{
				return "Removed AlwaysConsiderTurnAsMove from Mobile and ConsiderVerticalMovement\n" +
					"from GrantConditionOnMovement. Add 'Turn' and/or 'Vertical' on new ValidMovementTypes\n" +
					"fields on WithMoveAnimation and GrantConditionOnMovement instead.";
			}
		}

		readonly Dictionary<string, List<string>> locations = new Dictionary<string, List<string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "The following definitions implement WithMoveAnimation\n" +
					"or GrantConditionOnMovement. Check if they need updated ValidMovementTypes:\n" +
					UpdateUtils.FormatMessageList(locations.Select(
						kv => kv.Key + ":\n" + UpdateUtils.FormatMessageList(kv.Value)));

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var mobileNode = actorNode.ChildrenMatching("Mobile").FirstOrDefault(m => !m.IsRemoval());
			if (mobileNode != null)
			{
				var considerTurnAsMoveNode = mobileNode.LastChildMatching("AlwaysConsiderTurnAsMove");
				if (considerTurnAsMoveNode != null)
					mobileNode.RemoveNode(considerTurnAsMoveNode);
			}

			var used = new List<string>();
			var grantMoveConditions = actorNode.ChildrenMatching("GrantConditionOnMovement");
			foreach (var g in grantMoveConditions)
			{
				var considerVerticalNode = g.LastChildMatching("ConsiderVerticalMovement");
				if (considerVerticalNode != null)
					g.RemoveNode(considerVerticalNode);
			}

			if (grantMoveConditions.Any())
				used.Add("GrantConditionOnMovement");

			var moveAnim = actorNode.LastChildMatching("WithMoveAnimation");
			if (moveAnim != null)
				used.Add("WithMoveAnimation");

			if (used.Any())
			{
				var location = "{0} ({1})".F(actorNode.Key, actorNode.Location.Filename);
				locations[location] = used;
			}

			yield break;
		}
	}
}
