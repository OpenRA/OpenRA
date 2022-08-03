#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class UnhardcodeBaseBuilderBotModule : UpdateRule
	{
		public override string Name => "BaseBuilderBotModule got new fields to configure buildings that are defenses.";

		public override string Description => "DefenseTypes were added.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var addNodes = new List<MiniYamlNode>();

			var defense = modData.DefaultRules.Actors.Values.Where(a => a.HasTraitInfo<BuildingInfo>() && a.HasTraitInfo<AttackBaseInfo>()).Select(a => a.Name);
			var defensetypes = new MiniYamlNode("DefenseTypes", FieldSaver.FormatValue(defense.ToList()));
			addNodes.Add(defensetypes);

			foreach (var baseBuilderManager in actorNode.ChildrenMatching("BaseBuilderBotModule"))
				foreach (var addNode in addNodes)
					baseBuilderManager.AddNode(addNode);

			yield break;
		}
	}
}
