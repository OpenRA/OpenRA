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
	public class RemoveAirdropActorTypeDefault : UpdateRule
	{
		public override string Name => "Removed internal default of ProductionAirdrop.ActorType";

		public override string Description => "Removed internal default of 'c17' from ProductionAirdrop.ActorType.";

		readonly List<Tuple<string, string>> missingActorTypes = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "ProductionAirdrop.ActorType no longer defaults to 'c17' and must be defined explicitly.\n"
				+ "You may have to define it manually now in the following places:\n"
				+ UpdateUtils.FormatMessageList(missingActorTypes.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (missingActorTypes.Count > 0)
				yield return message;

			missingActorTypes.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var airProd = actorNode.LastChildMatching("ProductionAirdrop");
			if (airProd != null)
			{
				var actorTypeNode = airProd.LastChildMatching("ActorType");
				if (actorTypeNode == null)
					missingActorTypes.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));
			}

			yield break;
		}
	}
}
