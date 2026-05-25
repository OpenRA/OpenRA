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

namespace OpenRA.Widgets
{
	/// <summary>
	/// Flutter-style widget that imposes additional size constraints on its single child.
	/// Equivalent to Flutter's <c>ConstrainedBox</c> widget.
	/// <para>
	/// The additional constraints are intersected with the parent's constraints so that
	/// the resulting constraints are always satisfiable. This allows setting minimum or
	/// maximum sizes that are tighter than the parent would otherwise impose.
	/// </para>
	/// </summary>
	public class ConstrainedBoxWidget : Widget
	{
		/// <summary>Minimum width to impose on the child (0 = no extra minimum).</summary>
		public int AdditionalMinWidth;

		/// <summary>Maximum width to impose on the child (0 = no extra maximum).</summary>
		public int AdditionalMaxWidth;

		/// <summary>Minimum height to impose on the child (0 = no extra minimum).</summary>
		public int AdditionalMinHeight;

		/// <summary>Maximum height to impose on the child (0 = no extra maximum).</summary>
		public int AdditionalMaxHeight;

		public ConstrainedBoxWidget() { }

		public ConstrainedBoxWidget(ConstrainedBoxWidget other)
			: base(other)
		{
			AdditionalMinWidth = other.AdditionalMinWidth;
			AdditionalMaxWidth = other.AdditionalMaxWidth;
			AdditionalMinHeight = other.AdditionalMinHeight;
			AdditionalMaxHeight = other.AdditionalMaxHeight;
		}

		public override ConstrainedBoxWidget Clone() { return new ConstrainedBoxWidget(this); }

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			// Build the additional constraints, resolving 0 as "no limit".
			var addMaxW = AdditionalMaxWidth > 0 ? AdditionalMaxWidth : BoxConstraints.Unbounded;
			var addMaxH = AdditionalMaxHeight > 0 ? AdditionalMaxHeight : BoxConstraints.Unbounded;

			// Intersect parent constraints with the additional ones.
			var tightened = new BoxConstraints(
				Math.Max(constraints.MinWidth, AdditionalMinWidth),
				Math.Min(constraints.MaxWidth, addMaxW),
				Math.Max(constraints.MinHeight, AdditionalMinHeight),
				Math.Min(constraints.MaxHeight, addMaxH));

			// Clamp so min <= max (parent wins when there is a conflict).
			tightened = new BoxConstraints(
				Math.Min(tightened.MinWidth, tightened.MaxWidth),
				tightened.MaxWidth,
				Math.Min(tightened.MinHeight, tightened.MaxHeight),
				tightened.MaxHeight);

			int w, h;
			if (Children.Count > 0)
			{
				var child = Children[0];
				(w, h) = child.Measure(tightened);
				child.Bounds.X = 0;
				child.Bounds.Y = 0;
			}
			else
			{
				(w, h) = tightened.Constrain(0, 0);
			}

			Bounds.Width = w;
			Bounds.Height = h;
			return (w, h);
		}

		public override void PerformLayoutIfNeeded()
		{
			if (!layoutDirty)
				return;

			var maxW = Bounds.Width > 0 ? Bounds.Width : BoxConstraints.Unbounded;
			var maxH = Bounds.Height > 0 ? Bounds.Height : BoxConstraints.Unbounded;
			Measure(new BoxConstraints(Bounds.Width, maxW, Bounds.Height, maxH));
			layoutDirty = false;

			foreach (var child in Children)
				child.PerformLayoutIfNeeded();
		}
	}
}
