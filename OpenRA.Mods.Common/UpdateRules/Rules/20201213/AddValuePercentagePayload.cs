#region Copyright & License Information
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
	public class AddValuePercentagePayload : UpdateRule
	{
		public override string Name { get { return "Adds ValuePercentagePayload to DeliversCash."; } }
		public override string Description
		{
			get
			{
				return "Added ValuePercentagePayload (default 100) to DeliversCash and set Payload default to 0 in return.";
			}
		}

		readonly Dictionary<string, List<string>> locations = new Dictionary<string, List<string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "Defining an explicit Payload is now only needed when payload should differ from\n" +
					"the actor's value/cost, since ValuePercentagePayload defaults to 100(%) of cost.\n" +
					"Review the following definitions:\n" +
					UpdateUtils.FormatMessageList(locations.Select(
						kv => kv.Key + ":\n" + UpdateUtils.FormatMessageList(kv.Value)));

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var locationKey = "{0} ({1})".F(actorNode.Key, actorNode.Location.Filename);

			var cost = 0;
			foreach (var valuedNode in actorNode.ChildrenMatching("Valued"))
			{
				var costNode = valuedNode.LastChildMatching("Cost");
				if (costNode != null && costNode.NodeValue<int>() != 0)
					cost = costNode.NodeValue<int>();
			}

			foreach (var deliversCashNode in actorNode.ChildrenMatching("DeliversCash"))
			{
				if (deliversCashNode.IsRemoval())
					continue;

				var payloadNode = deliversCashNode.LastChildMatching("Payload");
				if (payloadNode != null)
				{
					var payload = payloadNode.NodeValue<int>();
					if (payload != 0 && cost == payload)
					{
						deliversCashNode.RemoveNode(payloadNode);
						locations.GetOrAdd(locationKey).Add(deliversCashNode.Key);
					}
					else
					{
						deliversCashNode.AddNode("ValuePercentagePayload", FieldSaver.FormatValue(0));
						locations.GetOrAdd(locationKey).Add(deliversCashNode.Key);
					}
				}
				else
					locations.GetOrAdd(locationKey).Add(deliversCashNode.Key);
			}

			yield break;
		}
	}
}
