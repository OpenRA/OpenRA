#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class RenameUseLocation : UpdateRule
	{
		public override string Name { get { return "AppearsOnRadar.UseLocation renamed and refactored to AppearanceType"; } }
		public override string Description
		{
			get
			{
				return "The AppearsOnRadar property UseLocation was renamed to AppearanceType,\n" +
					"and refactored to support four different sources for radar display:\n" +
					"'CenterPosition' (new), 'Location' (old 'true'), 'OccupiedCells' (old 'false')\n" +
					"and 'EntireFootprint' (new).";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var appOnRadar in actorNode.ChildrenMatching("AppearsOnRadar"))
			{
				var useLocation = appOnRadar.LastChildMatching("UseLocation");
				if (useLocation != null)
				{
					var oldValue = FieldLoader.GetValue<bool>("UseLocation", useLocation.Value.Value);
					if (oldValue)
						useLocation.ReplaceValue("Location");
					else
						useLocation.ReplaceValue("OccupiedCells");

					useLocation.RenameKeyPreservingSuffix("AppearanceType");
				}
			}

			yield break;
		}
	}
}
