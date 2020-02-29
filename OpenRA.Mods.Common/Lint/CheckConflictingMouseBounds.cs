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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckConflictingMouseBounds : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				var selectable = actorInfo.Value.TraitInfos<SelectableInfo>().Count();
				var interactable = actorInfo.Value.TraitInfos<InteractableInfo>().Count();
				if (selectable > 0 && selectable != interactable)
					emitWarning("Actor {0} defines both Interactable and Selectable traits. This may cause unexpected results.".F(actorInfo.Value.Name));
			}
		}
	}
}
