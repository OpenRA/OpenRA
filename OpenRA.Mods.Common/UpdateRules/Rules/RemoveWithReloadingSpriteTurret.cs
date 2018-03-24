#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class RemoveWithReloadingSpriteTurret : UpdateRule
	{
		public override string Name { get { return "Remove WithReloadingSpriteTurret trait"; } }
		public override string Description
		{
			get
			{
				return "WithReloadingSpriteTurret has been superseded by conditions.\n" +
					"The trait is switched for with WithSpriteTurret.\n";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var turret in actorNode.ChildrenMatching("WithReloadingSpriteTurret"))
			{
				turret.RenameKeyPreservingSuffix("WithSpriteTurret");
				yield return turret.Location.ToString() + ": WithReloadingSpriteTurret has been replaced by WithSpriteTurret.\n" +
					"You should use AmmoPool.AmmoConditions to switch turret type when reloading to restore the previous behaviour.";
			}

			yield break;
		}
	}
}
