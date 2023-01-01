#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class CreateFlashPaletteEffectWarhead : UpdateRule
	{
		public override string Name => "Create FlashPaletteEffectWarhead to replace hardcoded nuke flashing.";

		public override string Description => "The trait NukePower (via the NukeLaunch projectile that it uses) no longer has built-in palette flashing.";

		readonly List<Tuple<string, string, string>> weaponsToUpdate = new List<Tuple<string, string, string>>();

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var nukePowerTraits = actorNode.ChildrenMatching("NukePower");
			foreach (var nukePowerTrait in nukePowerTraits)
			{
				var traitName = nukePowerTrait.Key;
				var weaponNode = nukePowerTrait.ChildrenMatching("MissileWeapon").FirstOrDefault();
				if (weaponNode == null)
					continue;

				var weaponName = weaponNode.Value.Value;

				weaponsToUpdate.Add(new Tuple<string, string, string>(weaponName, traitName, $"{actorNode.Key} ({actorNode.Location.Filename})"));

				nukePowerTrait.RemoveNodes("FlashType");
			}

			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (weaponsToUpdate.Count > 0)
				yield return "Add a FlashPaletteEffectWarhead to the following weapons:\n" +
					UpdateUtils.FormatMessageList(weaponsToUpdate.Select(x => $"Weapon `{x.Item1}`, used by trait `{x.Item2}` on actor {x.Item3}"));

			weaponsToUpdate.Clear();
		}
	}
}
