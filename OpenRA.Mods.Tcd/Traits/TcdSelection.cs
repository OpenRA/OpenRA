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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Tcd.Traits
{
	[Desc("Drop-in replacement for Selection. Clicking any member of a squad selects the",
		"whole squad. Hold Alt to select the single actor instead. Requires SquadManager.")]
	public sealed class TcdSelectionInfo : SelectionInfo
	{
		public override object Create(ActorInitializer init) { return new TcdSelection(); }
	}

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public sealed class TcdSelection : Selection
	{
		SquadManager squads;

		public override void Combine(World world, IEnumerable<Actor> newSelection, bool isCombine, bool isClick)
		{
			// Materialised once. The base implementation enumerates this too, and
			// enumerating an IEnumerable parameter twice trips CA1851.
			var selection = newSelection as IReadOnlyList<Actor> ?? newSelection.ToList();

			// Resolved lazily: the world actor's traits are not all constructed yet when we are.
			squads ??= world.WorldActor.TraitOrDefault<SquadManager>();

			if (isClick && selection.Count > 0 && squads != null
				&& !Game.GetModifierKeys().HasModifier(Modifiers.Alt)
				&& squads.TryGetSquad(selection[0], out var squad))
			{
				// isClick: false so the base takes the whole list rather than just the first actor.
				base.Combine(world, squad.Members, isCombine, false);
				return;
			}

			base.Combine(world, selection, isCombine, isClick);
		}
	}
}
