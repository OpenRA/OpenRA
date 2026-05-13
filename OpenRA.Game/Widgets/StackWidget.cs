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
	/// Flutter-style widget that layers its children on top of each other.
	/// Equivalent to Flutter's <c>Stack</c> widget.
	/// <para>
	/// Non-positioned children are measured with loose constraints during the first pass
	/// to determine the stack size, then aligned according to <see cref="HorizontalAlignment"/>
	/// and <see cref="VerticalAlignment"/>. Their natural size is preserved; they are not
	/// forced to fill the stack.
	/// Children that implement <see cref="IPositioned"/> are placed at the
	/// explicitly declared offsets from the specified edges.
	/// The stack's own size is the bounding box of all non-positioned children
	/// (or the parent's tight constraint when available).
	/// </para>
	/// </summary>
	public class StackWidget : Widget
	{
		/// <summary>
		/// How non-positioned children are aligned within the stack.
		/// Defaults to top-left, mirroring Flutter's default.
		/// </summary>
		public HorizontalAlignment HorizontalAlignment = HorizontalAlignment.Left;

		/// <summary>How non-positioned children are aligned vertically.</summary>
		public VerticalAlignment VerticalAlignment = VerticalAlignment.Top;

		public StackWidget() { }

		public StackWidget(StackWidget other)
			: base(other)
		{
			HorizontalAlignment = other.HorizontalAlignment;
			VerticalAlignment = other.VerticalAlignment;
		}

		public override StackWidget Clone() { return new StackWidget(this); }

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			// Determine the stack size from parent constraints or non-positioned children.
			var stackW = constraints.MaxWidth < BoxConstraints.Unbounded ? constraints.MaxWidth : 0;
			var stackH = constraints.MaxHeight < BoxConstraints.Unbounded ? constraints.MaxHeight : 0;

			// First pass: measure non-positioned children to determine stack size.
			foreach (var child in Children)
			{
				if (!child.IsVisible() || child is IPositioned)
					continue;

				var (cw, ch) = child.Measure(BoxConstraints.Loose(
					constraints.MaxWidth, constraints.MaxHeight));
				stackW = Math.Max(stackW, cw);
				stackH = Math.Max(stackH, ch);
			}

			var (w, h) = constraints.Constrain(stackW, stackH);
			Bounds.Width = w;
			Bounds.Height = h;

			// Second pass: position non-positioned children.
			foreach (var child in Children)
			{
				if (!child.IsVisible() || child is IPositioned)
					continue;

				var cw = child.Bounds.Width;
				var ch = child.Bounds.Height;

				child.Bounds.X = HorizontalAlignment switch
				{
					HorizontalAlignment.Right => w - cw,
					HorizontalAlignment.Center => (w - cw) / 2,
					_ => 0
				};

				child.Bounds.Y = VerticalAlignment switch
				{
					VerticalAlignment.Bottom => h - ch,
					VerticalAlignment.Center => (h - ch) / 2,
					_ => 0
				};
			}

			// Third pass: measure and position Positioned children.
			foreach (var child in Children)
			{
				if (!child.IsVisible() || child is not IPositioned pos)
					continue;

				// Derive tight child constraints from declared edges.
				var childW = pos.Right.HasValue && pos.Left.HasValue
					? w - pos.Left.Value - pos.Right.Value
					: child.Bounds.Width > 0 ? child.Bounds.Width : constraints.MaxWidth;

				var childH = pos.Bottom.HasValue && pos.Top.HasValue
					? h - pos.Top.Value - pos.Bottom.Value
					: child.Bounds.Height > 0 ? child.Bounds.Height : constraints.MaxHeight;

				child.Measure(BoxConstraints.Tight(
					Math.Max(0, childW), Math.Max(0, childH)));

				// When only Right (or Bottom) is supplied we anchor to the opposite edge:
				// clamp to 0 so an over-sized child cannot end up at a negative position.
				child.Bounds.X = pos.Left ?? (pos.Right.HasValue ? Math.Max(0, w - pos.Right.Value - child.Bounds.Width) : 0);
				child.Bounds.Y = pos.Top ?? (pos.Bottom.HasValue ? Math.Max(0, h - pos.Bottom.Value - child.Bounds.Height) : 0);
			}

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
