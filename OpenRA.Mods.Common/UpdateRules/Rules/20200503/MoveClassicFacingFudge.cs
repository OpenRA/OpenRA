#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class MoveClassicFacingFudge : UpdateRule
	{
		public override string Name { get { return "UseClassicFacingFudge functionality was moved to Cnc-specific sequence/coordinate code."; } }
		public override string Description
		{
			get
			{
				return "UseClassicFacingFudge has been replaced with ClassicFacingBodyOrientation trait\n" +
						"and Classic* variants of *Sequence loaders respectively, both located in Mods.Cnc.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "UseClassicFacingFudge property on BodyOrientation was replaced with ClassicFacingBodyOrientation trait.\n" +
							 "UseClassicFacingFudge for sequences was renamed to UseClassicFacings and moved to\n" +
							 "Classic(TileSetSpecific)SpriteSequence loaders in Mods.Cnc.\n" +
							 "Update SpriteSequenceFormat: in mod.yaml accordingly.\n" +
				             "Make sure that actors implementing the following places don't use or inherit the standard BodyOrientation:\n" +
				             UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var modId = modData.Manifest.Id;

			foreach (var bo in actorNode.ChildrenMatching("BodyOrientation"))
			{
				var usesClassicFacings = false;
				var facingFudgeNode = bo.LastChildMatching("UseClassicFacingFudge");
				if (facingFudgeNode != null)
				{
					usesClassicFacings = facingFudgeNode.NodeValue<bool>();
					bo.RemoveNode(facingFudgeNode);
				}

				if (usesClassicFacings)
				{
					bo.RenameKey("ClassicFacingBodyOrientation");
					locations.Add("{0} ({1})".F(actorNode.Key, actorNode.Location.Filename));
				}
			}

			yield break;
		}

		public override IEnumerable<string> UpdateSequenceNode(ModData modData, MiniYamlNode sequenceNode)
		{
			foreach (var sequence in sequenceNode.Value.Nodes)
			{
				var facingFudgeNode = sequence.LastChildMatching("UseClassicFacingFudge");
				facingFudgeNode?.RenameKey("UseClassicFacings");
			}

			yield break;
		}
	}
}
