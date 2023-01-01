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
	class UpdateMapInits : UpdateRule
	{
		public override string Name => "Update map actor definitions";

		public override string Description
		{
			get
			{
				return "Several changes have been made to initial actor state in maps:\n" +
					UpdateUtils.FormatMessageList(new[]
					{
						"Facing is now defined as a world angle",
						"TurretFacing is now defined as a world angle relative to Facing",
						"Plugs has been removed (use Plug instead)",
						"TurretFacings has been removed (use TurretFacing instead)"
					}) +
					"\nMaps are automatically updated to keep the previous actor facings.";
			}
		}

		public override IEnumerable<string> UpdateMapActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.RemoveNodes("Plugs") > 0)
				yield return $"Initial plugs for actor {actorNode.Key} will need to be reconfigured using the map editor.";

			if (actorNode.RemoveNodes("TurretFacings") > 0)
				yield return $"Initial turret facings for actor {actorNode.Key} will need to be reconfigured using the map editor.";

			var bodyFacing = WAngle.Zero;
			foreach (var facing in actorNode.ChildrenMatching("Facing"))
			{
				bodyFacing = WAngle.FromFacing(facing.NodeValue<int>());
				facing.ReplaceValue(FieldSaver.FormatValue(bodyFacing));
			}

			var removeNodes = new List<MiniYamlNode>();
			foreach (var facing in actorNode.ChildrenMatching("TurretFacing"))
			{
				var turretFacing = WAngle.FromFacing(facing.NodeValue<int>()) - bodyFacing;
				if (turretFacing == WAngle.Zero)
					removeNodes.Add(facing);
				else
					facing.ReplaceValue(FieldSaver.FormatValue(turretFacing));
			}

			foreach (var node in removeNodes)
				actorNode.Value.Nodes.Remove(node);
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var turret in actorNode.ChildrenMatching("Turreted"))
				turret.RemoveNodes("PreviewFacing");

			yield break;
		}
	}
}
