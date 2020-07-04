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
	class RefactorAircraftTurnSpeed : UpdateRule
	{
		public override string Name { get { return "Split up aircraft TurnSpeed into flight and body turn speeds."; } }
		public override string Description
		{
			get
			{
				return "Aircraft TurnSpeed has been split into TurnSpeed and BodyTurnSpeed to allow aircraft with CanSlide: true to have" +
					"independent flight direction and body orientation. If BodyTurnSpeed is defined, TurnSpeed only controls rate of turn" +
					" for flight direction. Aircraft with CanSlide: true should use BodyTurnSpeed and leave TurnSpeed undefined to maintain" +
					"the old sliding behaviour.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var rp in actorNode.ChildrenMatching("Aircraft"))
			{
				if (rp.LastChildMatching("CanSlide").Key == "true")
					rp.RenameChildrenMatching("TurnSpeed", "BodyTurnSpeed");
			}

			yield break;
		}
	}
}
