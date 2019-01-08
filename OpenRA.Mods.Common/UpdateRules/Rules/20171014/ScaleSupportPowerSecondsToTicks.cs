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
	public class ScaleSupportPowerSecondsToTicks : UpdateRule
	{
		public override string Name { get { return "Scale support power time fields from seconds to ticks."; } }
		public override string Description
		{
			get
			{
				return "Scales ChargeTime, GpsPower.RevealDelay, and ChronoshiftPower.Duration\n" +
					"from seconds to ticks (in other words, by a factor of 25).\n" +
					"Additionally, renames 'ChargeTime' to 'ChargeInterval'.";
			}
		}

		static readonly Dictionary<string, string> ChargeTimeMapping = new Dictionary<string, string>()
		{
			{ "AirstrikePower", "ChargeTime" },
			{ "GrantExternalConditionPower", "ChargeTime" },
			{ "NukePower", "ChargeTime" },
			{ "ParatroopersPower", "ChargeTime" },
			{ "ProduceActorPower", "ChargeTime" },
			{ "SpawnActorPower", "ChargeTime" },
			{ "AttackOrderPower", "ChargeTime" },
			{ "GpsPower", "ChargeTime" },
			{ "ChronoshiftPower", "ChargeTime" },
			{ "IonCannonPower", "ChargeTime" },
		};

		static readonly Dictionary<string, string> SingularCases = new Dictionary<string, string>()
		{
			{ "GpsPower", "RevealDelay" },
			{ "ChronoshiftPower", "Duration" },
		};

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var kv in ChargeTimeMapping)
			{
				foreach (var trait in actorNode.ChildrenMatching(kv.Key))
				{
					var node = trait.LastChildMatching(kv.Value);
					if (node != null)
					{
						node.ReplaceValue((25 * node.NodeValue<int>()).ToString());
						node.RenameKey("ChargeInterval");
					}
				}
			}

			foreach (var kv in SingularCases)
			{
				foreach (var trait in actorNode.ChildrenMatching(kv.Key))
				{
					var node = trait.LastChildMatching(kv.Value);
					if (node != null)
						node.ReplaceValue((25 * node.NodeValue<int>()).ToString());
				}
			}

			yield break;
		}
	}
}
