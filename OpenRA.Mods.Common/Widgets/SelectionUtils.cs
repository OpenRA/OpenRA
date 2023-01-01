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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public static class SelectionUtils
	{
		public static IEnumerable<Actor> SelectActorsOnScreen(World world, WorldRenderer wr, IEnumerable<string> selectionClasses, IEnumerable<Player> players)
		{
			var actors = world.ScreenMap.ActorsInMouseBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight).Select(a => a.Actor);
			return SelectActorsByOwnerAndSelectionClass(actors, players, selectionClasses);
		}

		public static IEnumerable<Actor> SelectActorsInWorld(World world, IEnumerable<string> selectionClasses, IEnumerable<Player> players)
		{
			return SelectActorsByOwnerAndSelectionClass(world.Actors.Where(a => a.IsInWorld), players, selectionClasses);
		}

		public static IEnumerable<Actor> SelectActorsByOwnerAndSelectionClass(IEnumerable<Actor> actors, IEnumerable<Player> owners, IEnumerable<string> selectionClasses)
		{
			return actors.Where(a =>
			{
				if (!owners.Contains(a.Owner))
					return false;

				var s = a.TraitOrDefault<ISelectable>();

				// selectionClasses == null means that units, that meet all other criteria, get selected
				return s != null && (selectionClasses == null || selectionClasses.Contains(s.Class));
			});
		}

		public static IEnumerable<Actor> SelectHighestPriorityActorAtPoint(World world, int2 a, Modifiers modifiers)
		{
			var selected = world.ScreenMap.ActorsAtMouse(a)
				.Where(x => x.Actor.Info.HasTraitInfo<ISelectableInfo>() && (x.Actor.Owner.IsAlliedWith(world.RenderPlayer) || !world.FogObscures(x.Actor)))
				.WithHighestSelectionPriority(a, modifiers);

			if (selected != null)
				yield return selected;
		}

		public static IEnumerable<Actor> SelectActorsInBoxWithDeadzone(World world, int2 a, int2 b, Modifiers modifiers)
		{
			// For dragboxes that are too small, shrink the dragbox to a single point (point b)
			if ((a - b).Length <= Game.Settings.Game.SelectionDeadzone)
				a = b;

			if (a == b)
				return SelectHighestPriorityActorAtPoint(world, a, modifiers);

			return world.ScreenMap.ActorsInMouseBox(a, b)
				.Select(x => x.Actor)
				.Where(x => x.Info.HasTraitInfo<ISelectableInfo>() && (x.Owner.IsAlliedWith(world.RenderPlayer) || !world.FogObscures(x)))
				.SubsetWithHighestSelectionPriority(modifiers);
		}

		public static Player[] GetPlayersToIncludeInSelection(World world)
		{
			// Players to be included in the selection (the viewer or all players in "Disable shroud" / "All players" mode)
			var viewer = world.RenderPlayer ?? world.LocalPlayer;
			var isShroudDisabled = viewer == null || (world.RenderPlayer == null && world.LocalPlayer.Spectating);
			var isEveryone = viewer != null && viewer.NonCombatant && viewer.Spectating;

			return isShroudDisabled || isEveryone ? world.Players : new[] { viewer };
		}
	}
}
