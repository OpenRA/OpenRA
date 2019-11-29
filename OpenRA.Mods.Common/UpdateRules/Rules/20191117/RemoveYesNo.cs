﻿#region Copyright & License Information
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
	public class RemoveYesNo : UpdateRule
	{
		public override string Name { get { return "Remove 'yes' and 'no' in favor of 'true' and 'false'."; } }

		public override string Description
		{
			get
			{
				return "'Yes' and 'no' are no longer valid values for booleans. " +
					"Use 'true' and 'false' instead.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var traitNode in actorNode.Value.Nodes)
			{
				foreach (var n in traitNode.Value.Nodes)
				{
					var value = n.NodeValue<string>();
					if (value == null)
						continue;

					if (value.ToLowerInvariant() == "yes")
						n.ReplaceValue("true");
					else if (value.ToLowerInvariant() == "no")
						n.ReplaceValue("false");
				}
			}

			yield break;
		}

		bool displayed;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (displayed)
				yield break;

			displayed = true;
			yield return "'Yes' and 'no' have been removed from the mod rules. "
				+ "Chrome yaml files may need a manual update.";
		}
	}
}
