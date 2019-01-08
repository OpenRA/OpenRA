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
	public class MergeRearmAndRepairAnimation : UpdateRule
	{
		public override string Name { get { return "WithRearmAnimation and WithRepairAnimation were merged to WithResupplyAnimation"; } }
		public override string Description
		{
			get
			{
				return "The WithRearmAnimation  and WithRepairAnimation traits were merged intto a single\n" +
					"WithResupplyAnimation trait.";
			}
		}

		bool displayedMessage;
		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "If an actor had both a WithRearmAnimation and a WithRepairAnimation\n"
				+ "or multiple traits of either type, you may want to check the update results for possible\n"
				+ "redundant entries.\n";

			if (!displayedMessage)
				yield return message;

			displayedMessage = true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var rearmAnims = actorNode.ChildrenMatching("WithRearmAnimation");
			var repairAnims = actorNode.ChildrenMatching("WithRepairAnimation");
			var rearmAnimsTotal = rearmAnims.Count();
			var repairAnimsTotal = repairAnims.Count();

			if (rearmAnimsTotal == 0 && repairAnimsTotal == 0)
				yield break;
			else if (rearmAnimsTotal == 1 && repairAnimsTotal == 0)
				foreach (var rearmAnim in rearmAnims)
					rearmAnim.RenameKey("WithResupplyAnimation");
			else if (rearmAnimsTotal == 0 && repairAnimsTotal == 1)
				foreach (var repairAnim in repairAnims)
					repairAnim.RenameKey("WithResupplyAnimation");
			else if (rearmAnimsTotal == 1 && repairAnimsTotal == 1)
			{
				var rearmAnim = rearmAnims.First();
				var repairAnim = repairAnims.First();
				var rearmSequence = rearmAnim.LastChildMatching("Sequence");
				var rearmBody = rearmAnim.LastChildMatching("Body");
				var repairSequence = repairAnim.LastChildMatching("Sequence");
				var repairBody = repairAnim.LastChildMatching("Body");
				var matchingSequences = (rearmSequence == null && repairSequence == null)
					|| (rearmSequence != null && repairSequence != null && rearmSequence.Value.Value == repairSequence.Value.Value);
				var matchingBodies = (rearmBody == null && repairBody == null)
					|| (rearmBody != null && repairBody != null && rearmBody.Value.Value == repairBody.Value.Value);

				// If neither animation strays from the default values, we can safely merge them
				if (matchingSequences && matchingBodies)
				{
					rearmAnim.RenameKey("WithResupplyAnimation");
					actorNode.RemoveNode(repairAnim);
				}
				else
				{
					rearmAnim.RenameKey("WithResupplyAnimation@Rearm", false, true);
					repairAnim.RenameKey("WithResupplyAnimation@Repair", false, true);
				}
			}
			else
			{
				// If we got here, we have more than one of at least one of the two animation traits.
				var rearmAnimCount = 0;
				foreach (var rearmAnim in rearmAnims)
				{
					++rearmAnimCount;
					rearmAnim.RenameKey("WithResupplyAnimation@Rearm" + rearmAnimCount.ToString(), false, true);
					var playOnRearmNode = new MiniYamlNode("PlayAnimationOn", "Rearm");
					rearmAnim.AddNode(playOnRearmNode);
				}

				var repairAnimCount = 0;
				foreach (var repairAnim in repairAnims)
				{
					++repairAnimCount;
					repairAnim.RenameKey("WithResupplyAnimation@Repair" + repairAnimCount.ToString(), false, true);
					var playOnRepairNode = new MiniYamlNode("PlayAnimationOn", "Repair");
					repairAnim.AddNode(playOnRepairNode);
				}
			}

			yield break;
		}
	}
}
