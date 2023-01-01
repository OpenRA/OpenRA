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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveSmokeTrailWhenDamaged : UpdateRule
	{
		public override string Name { get { return "'SmokeTrailWhenDamaged' has been removed in favor of using 'LeavesTrails'."; } }
		public override string Description
		{
			get
			{
				return "'SmokeTrailWhenDamaged' was removed.";
			}
		}

		readonly Dictionary<string, List<string>> locations = new Dictionary<string, List<string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "Some actor(s) defined a MinDamage of neither 'Heavy' nor 'Undamaged' on SmokeTrailWhenDamaged before update.\n" +
					"Review the following definitions and add custom GrandConditionOnDamageState configs as required:\n" +
					UpdateUtils.FormatMessageList(locations.Select(
						kv => kv.Key + ":\n" + UpdateUtils.FormatMessageList(kv.Value)));

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var locationKey = $"{actorNode.Key} ({actorNode.Location.Filename})";
			var anyConditionalSmokeTrail = false;

			foreach (var smokeTrail in actorNode.ChildrenMatching("SmokeTrailWhenDamaged"))
			{
				var spriteNode = smokeTrail.LastChildMatching("Sprite");
				if (spriteNode != null)
					smokeTrail.RenameChildrenMatching("Sprite", "Image");
				else
					smokeTrail.AddNode("Image", FieldSaver.FormatValue("smokey"));

				var intervalNode = smokeTrail.LastChildMatching("Interval");
				if (intervalNode != null)
				{
					var interval = intervalNode.NodeValue<int>();
					smokeTrail.RenameChildrenMatching("Interval", "MovingInterval");
					smokeTrail.AddNode("StationaryInterval", FieldSaver.FormatValue(interval));
				}
				else
				{
					smokeTrail.AddNode("MovingInterval", FieldSaver.FormatValue(3));
					smokeTrail.AddNode("StationaryInterval", FieldSaver.FormatValue(3));
				}

				var minDamageNode = smokeTrail.LastChildMatching("MinDamage");
				var isConditional = true;
				if (minDamageNode != null)
				{
					var minDamage = minDamageNode.NodeValue<string>();
					if (minDamage == "Undamaged")
						isConditional = false;
					else if (minDamage != "Heavy")
						locations.GetOrAdd(locationKey).Add(smokeTrail.Key);

					smokeTrail.RemoveNode(minDamageNode);
				}

				smokeTrail.AddNode("SpawnAtLastPosition", FieldSaver.FormatValue(false));
				smokeTrail.AddNode("TrailWhileStationary", FieldSaver.FormatValue(true));
				smokeTrail.AddNode("Type", FieldSaver.FormatValue("CenterPosition"));

				if (isConditional)
				{
					smokeTrail.AddNode("RequiresCondition", FieldSaver.FormatValue("enable-smoke"));
					anyConditionalSmokeTrail = true;
				}

				smokeTrail.RenameChildrenMatching("Sequence", "Sequences");
				smokeTrail.RenameChildrenMatching("Offset", "Offsets");
				smokeTrail.RenameKey("LeavesTrails");
			}

			if (anyConditionalSmokeTrail)
			{
				var grantCondition = new MiniYamlNode("GrantConditionOnDamageState@SmokeTrail", "");
				grantCondition.AddNode("Condition", FieldSaver.FormatValue("enable-smoke"));
				actorNode.AddNode(grantCondition);
			}

			yield break;
		}
	}
}
