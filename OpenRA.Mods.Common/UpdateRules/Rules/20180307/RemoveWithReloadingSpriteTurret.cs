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
	public class RemoveWithReloadingSpriteTurret : UpdateRule
	{
		public override string Name { get { return "Remove WithReloadingSpriteTurret trait"; } }
		public override string Description
		{
			get
			{
				return "WithReloadingSpriteTurret has been superseded by conditions.\n" +
					"Instances of this trait are replaced by WithSpriteTurret.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "WithReloadingSpriteTurret has been replaced by WithSpriteTurret\n" +
					"You should use AmmoPool.AmmoConditions to switch turret type when reloading\n" +
					"to restore the previous behaviour on the following actors:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var turret in actorNode.ChildrenMatching("WithReloadingSpriteTurret"))
			{
				turret.RenameKey("WithSpriteTurret");
				locations.Add("{0} ({1})".F(actorNode.Key, turret.Location.Filename));
			}

			yield break;
		}
	}
}
