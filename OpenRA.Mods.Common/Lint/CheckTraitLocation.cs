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

namespace OpenRA.Mods.Common.Lint
{
	public class CheckTraitLocation : ILintRulesPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var traitLocation = traitInfo.GetType().GetCustomAttributes<TraitLocationAttribute>(true).FirstOrDefault();
					if (traitLocation == null)
						continue;

					if (!Enum.TryParse(actorInfo.Key, true, out SystemActors systemActor) || !traitLocation.SystemActors.HasFlag(systemActor))
					{
						// Remove the "Info" suffix
						var traitName = traitInfo.GetType().Name;
						traitName = traitName.Remove(traitName.Length - 4);
						var locations = traitLocation.SystemActors.ToString().Replace(", ", " or ");
						emitError($"{traitName} does not belong on {actorInfo.Key}. It is a system trait meant for {locations}.");
					}
				}
			}
		}
	}
}
