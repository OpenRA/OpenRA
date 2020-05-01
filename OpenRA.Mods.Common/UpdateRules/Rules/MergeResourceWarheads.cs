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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class MergeResourceWarheads : UpdateRule
	{
		public override string Name { get { return "Merged CreateResourceWarhead and DestroyResourceWarhead."; } }

		public override string Description
		{
			get
			{
				return "The CreateResourceWarhead was renamed to ModifyResourceWarhead and the DestroyResourceWarhead\n" +
					"turned into a simple RemoveResources boolean on ModifyResourceWarhead.";
			}
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			var destroyResourceWarheads = weaponNode.ChildrenMatching("Warhead").Where(wh => wh.Value.Value == "DestroyResource");
			foreach (var drwh in destroyResourceWarheads)
			{
				drwh.ReplaceValue("ModifyResource");
				drwh.AddNode("RemoveResource", "true");
			}

			var createResourceWarheads = weaponNode.ChildrenMatching("Warhead").Where(wh => wh.Value.Value == "CreateResource");
			foreach (var crwh in createResourceWarheads)
				crwh.ReplaceValue("ModifyResource");

			yield break;
		}
	}
}
