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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ConvertSupportPowerRangesToFootprint : UpdateRule
	{
		public override string Name => "Convert support power ranges to footprint";

		public override string Description =>
			"ChronoshiftPower and GrantExternalConditionPower use footprint areas\n" +
			"instead of a circular range and they no longer have a fallback default area value.\n" +
			"The old Range values will be converted to footprints as part of this update.";

		static readonly string[] AffectedTraits = new string[] { "GrantExternalConditionPower", "ChronoshiftPower" };

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var at in AffectedTraits)
				foreach (var trait in actorNode.ChildrenMatching(at))
					UpdatePower(trait);

			yield break;
		}

		void UpdatePower(MiniYamlNode power)
		{
			var range = 1;
			var rangeNode = power.LastChildMatching("Range");
			if (rangeNode != null)
			{
				range = rangeNode.NodeValue<int>();
				if (range > 3)
					locations.Add($"{rangeNode.Key} ({rangeNode.Location.Filename})");

				power.RemoveNode(rangeNode);
			}

			var size = 2 * range + 1;
			power.AddNode(new MiniYamlNode("Dimensions", size.ToString() + ", " + size.ToString()));

			var footprint = string.Empty;

			var emptycell = range;
			var affectedcell = 1;

			for (var i = 0; i < size; i++)
			{
				for (var e = 0; e < emptycell; e++)
					footprint += '_';

				for (var a = 0; a < affectedcell; a++)
					footprint += 'x';

				for (var e = 0; e < emptycell; e++)
					footprint += '_';

				if (i < range)
				{
					emptycell--;
					affectedcell += 2;
				}
				else
				{
					emptycell++;
					affectedcell -= 2;
				}

				footprint += ' ';
			}

			power.AddNode(new MiniYamlNode("Footprint", footprint));
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "Please check and adjust the new auto-generated dimensions.\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}
	}
}
