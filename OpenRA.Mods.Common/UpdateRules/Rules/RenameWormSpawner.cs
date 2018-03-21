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
	public class RenameWormSpawner : UpdateRule
	{
		public override string Name { get { return "WormSpawner renamed and generalized to ActorSpawner"; } }
		public override string Description
		{
			get
			{
				return "The D2k-specific WormSpawner trait was renamed to ActorSpawner,\n" +
					"generalized, and moved into the common mod code.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var spawner in actorNode.ChildrenMatching("WormSpawner"))
				spawner.RenameKeyPreservingSuffix("ActorSpawner");

			foreach (var manager in actorNode.ChildrenMatching("WormManager"))
			{
				manager.RenameKeyPreservingSuffix("ActorSpawnManager");
				var signature = manager.LastChildMatching("WormSignature");
				if (signature != null)
					signature.RenameKeyPreservingSuffix("Actors");
			}

			yield break;
		}
	}
}
