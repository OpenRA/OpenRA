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
	class SpawnActorPowerDefaultEffect : UpdateRule
	{
		public override string Name { get { return "Set SpawnActorPower EffectSequence to it's previous default value."; } }
		public override string Description
		{
			get
			{
				return "The 'EffectSequence' of 'SpawnActorPower' is unset by default.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var spawnActorPower in actorNode.ChildrenMatching("SpawnActorPower"))
			{
				var effectNode = spawnActorPower.LastChildMatching("EffectSequence");
				if (effectNode == null)
					spawnActorPower.AddNode("EffectSequence", "idle");
			}

			yield break;
		}
	}
}
