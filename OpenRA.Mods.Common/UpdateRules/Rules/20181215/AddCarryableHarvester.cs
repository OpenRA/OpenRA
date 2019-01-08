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
	public class AddCarryableHarvester : UpdateRule
	{
		public override string Name { get { return "Inform about the 'CarryableHarvester' trait."; } }
		public override string Description
		{
			get
			{
				return "A 'CarryableHarvester' trait was added for harvesters that use 'AutoCarryable'.";
			}
		}

		bool hasAutoCarryable;
		readonly List<string> harvesters = new List<string>();

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			harvesters.Clear();
			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (!hasAutoCarryable)
				hasAutoCarryable = actorNode.ChildrenMatching("AutoCarryable").Any();

			var harvester = actorNode.LastChildMatching("Harvester");
			if (harvester != null)
				harvesters.Add("{0} ({1})".F(actorNode.Key, harvester.Location.Filename));

			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!hasAutoCarryable || !harvesters.Any())
				yield break;

			yield return "Detected an 'AutoCarryable' trait.\n" +
				"Review the following definitions and, if required,\n" +
				"add the new 'CarryableHarvester' trait.\n" +
					UpdateUtils.FormatMessageList(harvesters, 1);
		}
	}
}
