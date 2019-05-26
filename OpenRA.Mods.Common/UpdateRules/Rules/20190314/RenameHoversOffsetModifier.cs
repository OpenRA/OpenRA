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
	public class RenameHoversOffsetModifier : UpdateRule
	{
		public override string Name { get { return "Rename Hovers OffsetModifier"; } }
		public override string Description
		{
			get
			{
				return "Hovers' OffsetModifier was renamed to BobDistance,\n" +
					"as 'Modifier' is a term we don't normally use for distance,\n" +
					"while 'Offset' would imply a 3D vector, which isn't the case here.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var h in actorNode.ChildrenMatching("Hovers"))
				foreach (var node in h.ChildrenMatching("OffsetModifier"))
					node.RenameKey("BobDistance");

			yield break;
		}
	}
}
