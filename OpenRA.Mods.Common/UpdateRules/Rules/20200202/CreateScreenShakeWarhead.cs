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
	public class CreateScreenShakeWarhead : UpdateRule
	{
		public override string Name => "Create ScreenShakeWarhead to replace hardcoded shaking.";

		public override string Description => "The traits MadTank and NukePower (via the NukeLaunch projectile that it uses) no longer have built-in screen shaking.";

		readonly List<Tuple<string, string, string>> weaponsToUpdate = new List<Tuple<string, string, string>>();

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var madTankTraits = actorNode.ChildrenMatching("MadTank");
			var nukePowerTraits = actorNode.ChildrenMatching("NukePower");

			foreach (var madTankTrait in madTankTraits)
			{
				var traitName = madTankTrait.Key;
				var weaponNode = madTankTrait.ChildrenMatching("MADTankThump").FirstOrDefault();
				var weaponName = weaponNode != null ? weaponNode.Value.Value : "MADTankThump";

				weaponsToUpdate.Add(new Tuple<string, string, string>(weaponName, traitName, $"{actorNode.Key} ({actorNode.Location.Filename})"));

				madTankTrait.RemoveNodes("ThumpShakeTime");
				madTankTrait.RemoveNodes("ThumpShakeIntensity");
				madTankTrait.RemoveNodes("ThumpShakeMultiplier");
			}

			foreach (var nukePowerTrait in nukePowerTraits)
			{
				var traitName = nukePowerTrait.Key;
				var weaponNode = nukePowerTrait.ChildrenMatching("MissileWeapon").FirstOrDefault();
				if (weaponNode == null)
					continue;

				var weaponName = weaponNode.Value.Value;

				weaponsToUpdate.Add(new Tuple<string, string, string>(weaponName, traitName, $"{actorNode.Key} ({actorNode.Location.Filename})"));
			}

			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (weaponsToUpdate.Count > 0)
				yield return "Add a ScreenShakeWarhead to the following weapons:\n" +
					UpdateUtils.FormatMessageList(weaponsToUpdate.Select(x => $"Weapon `{x.Item1}`, used by trait `{x.Item2}` on actor {x.Item3}"));

			weaponsToUpdate.Clear();
		}
	}
}
