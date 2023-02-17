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
	public class ReplaceCrateSecondsWithTicks : UpdateRule
	{
		public override string Name => "Changed Crate Lifetime to Duration and use ticks and renamed PanicLength to PanicDuration.";

		public override string Description =>
			"Crate.Lifetime was the last non-sound-related place to still use 'seconds'\n" +
			"by multiplying with 25 internally. Converted to use ticks like everything else.\n" +
			"Also renamed Lifetime to Duration and ScaredyCat.PanicLength to PanicDuration to match other places.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var crateNode in actorNode.ChildrenMatching("Crate"))
			{
				foreach (var lifetimeNode in crateNode.ChildrenMatching("Lifetime"))
				{
					var lifetime = lifetimeNode.NodeValue<int>();
					lifetimeNode.Value.Value = FieldSaver.FormatValue(lifetime * 25);
					lifetimeNode.RenameKey("Duration");
				}
			}

			foreach (var scNode in actorNode.ChildrenMatching("ScaredyCat"))
				scNode.RenameChildrenMatching("PanicLength", "PanicDuration");

			yield break;
		}
	}
}
