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
	sealed class RemoveBarracksTypesAndVehiclesTypesInBaseBuilderBotModule : UpdateRule
	{
		public override string Name => "Remove BarracksTypes and VehiclesTypes in BaseBuilderBotModule";
		public override string Description => "BarracksTypes and VehiclesTypes were removed and now BaseBuilderBotModule check by using ProductionTypes.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var baseBuilder in actorNode.ChildrenMatching("BaseBuilderBotModule"))
			{
				baseBuilder.RemoveNodes("BarracksTypes");
				baseBuilder.RemoveNodes("VehiclesFactoryTypes");
			}

			yield break;
		}
	}
}
