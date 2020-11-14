#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class RemoveLaysTerrain : UpdateRule
	{
		public override string Name { get { return "'LaysTerrain' has been removed in favor of the new 'D2kBuilding' trait."; } }
		public override string Description
		{
			get
			{
				return "'LaysTerrain' was removed.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.RemoveNodes("LaysTerrain") > 0)
				yield return "'LaysTerrain' was removed from {0} ({1}) without replacement.\n".F(actorNode.Key, actorNode.Location.Filename);

			yield break;
		}
	}
}
