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
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	public static class SelectableExts
	{
		public static int SelectionPriority(this ActorInfo a, Modifiers modifiers)
		{
			var selectableInfo = a.TraitInfoOrDefault<ISelectableInfo>();
			return selectableInfo != null ? BaseSelectionPriority(selectableInfo, modifiers) : int.MinValue;
		}

		const int PriorityRange = 30;

		public static int SelectionPriority(this Actor a, Modifiers modifiers)
		{
			var info = a.Info.TraitInfo<ISelectableInfo>();
			var basePriority = BaseSelectionPriority(info, modifiers);

			var viewer = (a.World.LocalPlayer == null || a.World.LocalPlayer.Spectating) ? a.World.RenderPlayer : a.World.LocalPlayer;

			if (a.Owner == viewer || viewer == null)
				return basePriority;

			switch (viewer.RelationshipWith(a.Owner))
			{
				case PlayerRelationship.Ally: return basePriority - PriorityRange;
				case PlayerRelationship.Neutral: return basePriority - 2 * PriorityRange;
				case PlayerRelationship.Enemy: return basePriority - 3 * PriorityRange;

				default:
					throw new InvalidOperationException();
			}
		}

		static int BaseSelectionPriority(ISelectableInfo info, Modifiers modifiers)
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

		static long CalculateActorSelectionPriority(ActorInfo info, in Polygon bounds, int2 selectionPixel, Modifiers modifiers)
		{
			if (bounds.IsEmpty)
				return info.SelectionPriority(modifiers);

			// Assume that the center of the polygon is the same as the center of the bounding box
			// This isn't necessarily true for arbitrary polygons, but is fine for the hexagonal and diamond
			// shapes that are currently implemented
			var br = bounds.BoundingRect;
			var centerPixel = new int2(
				br.Left + br.Size.Width / 2,
				br.Top + br.Size.Height / 2);

			var pixelDistance = (centerPixel - selectionPixel).Length;
			return info.SelectionPriority(modifiers) - (long)pixelDistance << 16;
		}

		static readonly Actor[] NoActors = Array.Empty<Actor>();

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
