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
	public class RenameBurstDelay : UpdateRule
	{
		public override string Name { get { return "BurstDelay was renamed to BurstDelays due to support of multiple values."; } }
		public override string Description
		{
			get
			{
				return "It's now possible to set multiple delay values (one for each consecutive burst),\n" +
					"so the property was renamed to BurstDelays to account for this.";
			}
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			var bd = weaponNode.LastChildMatching("BurstDelay");
			if (bd != null)
				bd.RenameKey("BurstDelays");

			yield break;
		}
	}
}
