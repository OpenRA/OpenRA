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
	public class ReplaceAircraftCanHover : UpdateRule
	{
		public override string Name { get { return "CanHover removed from Aircraft"; } }
		public override string Description
		{
			get
			{
				return "'CanHover: true' is now implied by 'IdleSpeed: 0'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var node in actorNode.ChildrenMatching("Aircraft"))
			{
				var ch = node.LastChildMatching("CanHover");
				if (ch == null)
					yield break;

				if (ch.IsRemoval() || !ch.NodeValue<bool>())
				{
					ch.ReplaceValue("-1");
					ch.RenameKey("IdleSpeed", includeRemovals: false);
					yield break;
				}

				ch.ReplaceValue("0");
				ch.RenameKey("IdleSpeed");
			}

			yield break;
		}
	}
}
