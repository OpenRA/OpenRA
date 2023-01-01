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

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays overlays from `ProductionIconOverlayManager` with matching types when defined prerequisites are granted.")]
	public class WithProductionIconOverlayInfo : TraitInfo<WithProductionIconOverlay>, IRulesetLoaded
	{
		[FieldLoader.Require]
		public readonly string[] Types = Array.Empty<string>();

		public readonly string[] Prerequisites = Array.Empty<string>();

		public virtual void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			foreach (var type in Types)
			{
				if (!rules.Actors[SystemActors.Player].TraitInfos<ProductionIconOverlayManagerInfo>().Where(piom => piom.Type == type).Any())
					throw new YamlException($"A 'ProductionIconOverlayManager' with type '{type}' doesn't exist.");

				if (ai.TraitInfos<WithProductionIconOverlayInfo>().Where(wpio => wpio != this && wpio.Types.Contains(type)).Any())
					throw new YamlException($"Multiple 'WithProductionIconOverlay's with type '{type}' exist on the actor.");
			}
		}
	}

	public class WithProductionIconOverlay { }
}
