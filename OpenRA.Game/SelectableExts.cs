#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	public static class SelectableExts
	{
		public static int SelectionPriority(this ActorInfo a, Modifiers modifiers)
		{
			var selectableInfo = a.TraitInfoOrDefault<SelectableInfo>();
			return selectableInfo != null ? BaseSelectionPriority(selectableInfo, modifiers) : int.MinValue;
		}

		const int PriorityRange = 30;

		public static int SelectionPriority(this Actor a, Modifiers modifiers)
		{
			var info = a.Info.TraitInfo<SelectableInfo>();
			var basePriority = BaseSelectionPriority(info, modifiers);

			var viewer = (a.World.LocalPlayer == null || a.World.LocalPlayer.Spectating) ? a.World.RenderPlayer : a.World.LocalPlayer;

			if (a.Owner == viewer || viewer == null)
				return basePriority;

			switch (viewer.Stances[a.Owner])
			{
				case Stance.Ally: return basePriority - PriorityRange;
				case Stance.Neutral: return basePriority - 2 * PriorityRange;
				case Stance.Enemy: return basePriority - 3 * PriorityRange;

				default:
					throw new InvalidOperationException();
			}
		}

		static int BaseSelectionPriority(SelectableInfo info, Modifiers modifiers)
		{
			var priority = info.Priority;

			if (modifiers.HasModifier(Modifiers.Ctrl) && !modifiers.HasModifier(Modifiers.Alt) && info.PriorityModifiers.HasFlag(SelectionPriorityModifiers.Ctrl))
				priority = int.MaxValue;

			if (modifiers.HasModifier(Modifiers.Alt) && !modifiers.HasModifier(Modifiers.Ctrl) && info.PriorityModifiers.HasFlag(SelectionPriorityModifiers.Alt))
				priority = int.MaxValue;

			return priority;
		}

		public static Actor WithHighestSelectionPriority(this IEnumerable<ActorBoundsPair> actors, int2 selectionPixel, Modifiers modifiers)
		{
			if (!actors.Any())
				return null;

			return actors.MaxBy(a => CalculateActorSelectionPriority(a.Actor.Info, a.Bounds, selectionPixel, modifiers)).Actor;
		}

		public static FrozenActor WithHighestSelectionPriority(this IEnumerable<FrozenActor> actors, int2 selectionPixel, Modifiers modifiers)
		{
			return actors.MaxByOrDefault(a => CalculateActorSelectionPriority(a.Info, a.MouseBounds, selectionPixel, modifiers));
		}

		static long CalculateActorSelectionPriority(ActorInfo info, Rectangle bounds, int2 selectionPixel, Modifiers modifiers)
		{
			if (bounds.IsEmpty)
				return info.SelectionPriority(modifiers);

			var centerPixel = new int2(
				bounds.Left + bounds.Size.Width / 2,
				bounds.Top + bounds.Size.Height / 2);

			var pixelDistance = (centerPixel - selectionPixel).Length;
			return ((long)-pixelDistance << 32) + info.SelectionPriority(modifiers);
		}

		static readonly Actor[] NoActors = { };

		public static IEnumerable<Actor> SubsetWithHighestSelectionPriority(this IEnumerable<Actor> actors, Modifiers modifiers)
		{
			return actors.GroupBy(x => x.SelectionPriority(modifiers))
				.OrderByDescending(g => g.Key)
				.Select(g => g.AsEnumerable())
				.DefaultIfEmpty(NoActors)
				.FirstOrDefault();
		}
	}
}
