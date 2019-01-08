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
	public class ReplacedWithChargeAnimation : UpdateRule
	{
		public override string Name { get { return "Replaced WithChargeAnimation with WithChargeSpriteBody"; } }
		public override string Description
		{
			get
			{
				return "Replaced WithChargeAnimation with WithChargeSpriteBody.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "WithChargeAnimation has been replaced by WithChargeSpriteBody.\n" +
					"You may need to disable/remove any previous (including inherited) *SpriteBody traits\n" +
					"on the following actors:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var chargeAnims = actorNode.ChildrenMatching("WithChargeAnimation");

			foreach (var ca in chargeAnims)
			{
				// If it's a trait removal, we only rename it.
				if (ca.IsRemoval())
				{
					ca.RenameKey("WithChargeSpriteBody");
					continue;
				}

				var sequence = ca.LastChildMatching("Sequence");
				var body = ca.LastChildMatching("Body");

				if (sequence == null)
				{
					var newSequenceNode = new MiniYamlNode("Sequence", "active");
					ca.AddNode(newSequenceNode);
				}

				if (body != null)
					ca.RemoveNode(body);

				ca.RenameKey("WithChargeSpriteBody");
				locations.Add("{0} ({1})".F(actorNode.Key, ca.Location.Filename));
			}

			yield break;
		}
	}
}
