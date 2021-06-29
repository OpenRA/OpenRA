﻿#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
	public class RenameSelfHealing : UpdateRule
	{
		public override string Name => "SelfHealing was renamed as negative SelfHealing is a common usecase.";

		public override string Description =>
			"SelfHealing was renamed to ChangesHealth\n" +
			"HealIfBelow was renamed to StartIfBelow.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var modId = modData.Manifest.Id;

			foreach (var sh in actorNode.ChildrenMatching("SelfHealing"))
			{
				sh.RenameChildrenMatching("HealIfBelow", "StartIfBelow");
				sh.RenameKey("ChangesHealth");
			}

			yield break;
		}
	}
}
