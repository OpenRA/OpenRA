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

namespace OpenRA.Widgets
{
	/// <summary>
	/// Flutter-style all-in-one layout widget that combines padding, sizing constraints,
	/// and child alignment in a single node. Equivalent to Flutter's <c>Container</c> widget.
	/// </summary>
	public class ContainerWidget : Widget
	{
		// -----------------------------------------------------------------------
		// YAML-configurable layout properties
		// -----------------------------------------------------------------------

		/// <summary>
		/// Insets applied inside the container, around the child.
		/// Named <c>ContainerPadding</c> in YAML to avoid shadowing the base-class
		/// <c>Padding</c> property which handles border-box spacing.
		/// </summary>
		public EdgeInsets ContainerPadding;

		/// <summary>
		/// How the child is aligned horizontally within the container's content area.
		/// Mirrors Flutter's <c>Alignment.x</c> axis (-1 = left, 0 = center, 1 = right).
		/// </summary>
		public HorizontalAlignment HorizontalAlignment = HorizontalAlignment.Left;

		/// <summary>
		/// How the child is aligned vertically within the container's content area.
		/// Mirrors Flutter's <c>Alignment.y</c> axis (-1 = top, 0 = center, 1 = bottom).
		/// </summary>
		public VerticalAlignment VerticalAlignment = VerticalAlignment.Top;

		public readonly bool ClickThrough = true;

		public ContainerWidget() { IgnoreMouseOver = true; }

		public ContainerWidget(ContainerWidget other)
			: base(other)
		{
			ContainerPadding = other.ContainerPadding;
			HorizontalAlignment = other.HorizontalAlignment;
			VerticalAlignment = other.VerticalAlignment;
			ClickThrough = other.ClickThrough;
			IgnoreMouseOver = true;
		}

		public override ContainerWidget Clone() { return new ContainerWidget(this); }

		public override string GetCursor(int2 pos) { return null; }

		public override bool HandleMouseInput(MouseInput mi)
		{
			return !ClickThrough && EventBounds.Contains(mi.Location);
		}

		// ChildOrigin offsets child rendering by the padding amount.
		public override int2 ChildOrigin => RenderOrigin + new int2(
			Border.Left + Padding.Left + ContainerPadding.Left,
			Border.Top + Padding.Top + ContainerPadding.Top);

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			// Intersect parent constraints with our own declared min/max.
			var ownConstraints = new BoxConstraints(
				System.Math.Max(constraints.MinWidth, MinWidth),
				System.Math.Min(constraints.MaxWidth, MaxWidth == int.MaxValue ? BoxConstraints.Unbounded : MaxWidth),
				System.Math.Max(constraints.MinHeight, MinHeight),
				System.Math.Min(constraints.MaxHeight, MaxHeight == int.MaxValue ? BoxConstraints.Unbounded : MaxHeight));

			// If a specific Width/Height is declared in YAML, tighten that axis — but never
			// override a tighter constraint already imposed by the parent.
			if (Bounds.Width > 0)
			{
				var clampedW = System.Math.Clamp(Bounds.Width, ownConstraints.MinWidth, ownConstraints.MaxWidth);
				ownConstraints = new BoxConstraints(clampedW, clampedW, ownConstraints.MinHeight, ownConstraints.MaxHeight);
			}

			if (Bounds.Height > 0)
			{
				var clampedH = System.Math.Clamp(Bounds.Height, ownConstraints.MinHeight, ownConstraints.MaxHeight);
				ownConstraints = new BoxConstraints(ownConstraints.MinWidth, ownConstraints.MaxWidth, clampedH, clampedH);
			}

			// Account for the base Widget Padding/Border (box model) + ContainerPadding.
			var totalInsetH = Padding.Horizontal + Border.Horizontal + ContainerPadding.Horizontal;
			var totalInsetV = Padding.Vertical + Border.Vertical + ContainerPadding.Vertical;

			// Pass infinite constraints to children so they can size themselves to their own
			// declared dimensions — tight constraints on the container define its own size only,
			// not the children's.  Children are then aligned within the available content area.
			// This matches Flutter semantics: a fixed-size Container does not clip its children.
			var childConstraints = BoxConstraints.Infinite();

			int w, h;
			if (Children.Count > 0)
			{
				// Size this container: wrap the largest child plus insets, then constrain.
				var maxCW = 0;
				var maxCH = 0;
				foreach (var child in Children)
				{
					if (!child.IsVisible())
						continue;
					var (cw, ch) = child.Measure(childConstraints);
					maxCW = System.Math.Max(maxCW, cw);
					maxCH = System.Math.Max(maxCH, ch);
				}

				(w, h) = ownConstraints.Constrain(maxCW + totalInsetH, maxCH + totalInsetV);

				// Only reposition children when a non-default alignment is requested.
				// When alignment is Left/Top (default), children keep their YAML-declared
				// Bounds.X/Y so that legacy absolute-positioned layouts are preserved.
				if (HorizontalAlignment != HorizontalAlignment.Left || VerticalAlignment != VerticalAlignment.Top)
				{
					var contentW = w - totalInsetH;
					var contentH = h - totalInsetV;

					foreach (var child in Children)
					{
						if (!child.IsVisible())
							continue;

						var cw = child.Bounds.Width;
						var ch = child.Bounds.Height;

						if (HorizontalAlignment != HorizontalAlignment.Left)
							child.Bounds.X = HorizontalAlignment == HorizontalAlignment.Right
								? contentW - cw
								: (contentW - cw) / 2;

						if (VerticalAlignment != VerticalAlignment.Top)
							child.Bounds.Y = VerticalAlignment == VerticalAlignment.Bottom
								? contentH - ch
								: (contentH - ch) / 2;
					}
				}
			}
			else
			{
				// No child: size is just the insets (or the declared size if tight).
				(w, h) = ownConstraints.Constrain(totalInsetH, totalInsetV);
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
			Measure(new BoxConstraints(0, maxW, 0, maxH));
			layoutDirty = false;

			foreach (var child in Children)
				child.PerformLayoutIfNeeded();
		}
	}
}
