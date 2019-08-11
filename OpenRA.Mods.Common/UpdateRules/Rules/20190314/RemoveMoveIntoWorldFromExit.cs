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
	public class RemoveMoveIntoWorldFromExit : UpdateRule
	{
		public override string Name { get { return "Remove MoveIntoWorld from Exit."; } }
		public override string Description
		{
			get
			{
				return "The MoveIntoWorld parameter has been removed from the Exit trait because it no\n" +
    				"longer serves a purpose (aircraft can now use the same exit procedure as other\n" +
					"units).";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var t in actorNode.ChildrenMatching("Exit"))
				t.RemoveNodes("MoveIntoWorld");

			yield break;
		}
	}
}
