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
	public class RenameArmamentLocalOffset : UpdateRule
	{
		public override string Name => "Rename 'LocalOffset' field on `Armament` trait to 'LocalOffsets'.";

		public override string Description => "The 'LocalOffset' field on `Armament` trait has been renamed to 'LocalOffsets'.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var armamentNode in actorNode.ChildrenMatching("Armament"))
				foreach (var localOffsetNode in armamentNode.ChildrenMatching("LocalOffset"))
					localOffsetNode.RenameKey("LocalOffsets");

			yield break;
		}
	}
}
