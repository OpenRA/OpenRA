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
	public class RenameCircleContrast : UpdateRule
	{
		public override string Name => "Rename 'ContrastColor' to 'BorderColor'.";

		public override string Description => "RenderDetectionCircle and RenderShroudCircle ContrastColor have been renamed to BorderColor for consistency.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var rdc in actorNode.ChildrenMatching("RenderDetectionCircle"))
				rdc.RenameChildrenMatching("ContrastColor", "BorderColor");

			foreach (var rsc in actorNode.ChildrenMatching("RenderShroudCircle"))
				rsc.RenameChildrenMatching("ContrastColor", "BorderColor");

			yield break;
		}
	}
}
