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
	public class RenameReservableToDock : UpdateRule
	{
		public override string Name { get { return "Reservable renamed to Dock"; } }
		public override string Description
		{
			get
			{
				return "'Reservable' trait is now called 'Dock'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var reservable in actorNode.ChildrenMatching("Reservable"))
				reservable.RenameKey("Dock");

			yield break;
		}
	}
}
