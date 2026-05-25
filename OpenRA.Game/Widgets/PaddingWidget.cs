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
	/// Flutter-style widget that adds <see cref="PaddingInsets"/> around its single child.
	/// Equivalent to Flutter's <c>Padding</c> widget.
	/// <para>
	/// The child is measured with constraints deflated by the padding, then positioned
	/// at <c>(PaddingInsets.Left, PaddingInsets.Top)</c> relative to this widget's origin.
	/// This widget's size becomes <c>childSize + padding on each axis</c>.
	/// </para>
	/// </summary>
	public class PaddingWidget : Widget
	{
		/// <summary>
		/// Insets to apply around the single child.
		/// Named <c>PaddingInsets</c> in YAML to avoid shadowing the base-class
		/// <c>Padding</c> property which handles border-box spacing.
		/// </summary>
		public EdgeInsets PaddingInsets;

		public PaddingWidget() { }

		public PaddingWidget(PaddingWidget other)
			: base(other)
		{
			PaddingInsets = other.PaddingInsets;
		}

		public override PaddingWidget Clone() { return new PaddingWidget(this); }

		// ChildOrigin offsets child drawing by the inset amount.
		public override int2 ChildOrigin => RenderOrigin + new int2(PaddingInsets.Left, PaddingInsets.Top);

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			// Deflate constraints for the child.
			var childConstraints = constraints.Deflate(PaddingInsets);

			if (Children.Count > 0)
			{
				var child = Children[0];
				var (cw, ch) = child.Measure(childConstraints);

				// Position child at origin — ChildOrigin offsets it by PaddingInsets.
				child.Bounds.X = 0;
				child.Bounds.Y = 0;

				// This widget's size wraps the child plus padding.
				var (w, h) = constraints.Constrain(
					cw + PaddingInsets.Horizontal,
					ch + PaddingInsets.Vertical);
				Bounds.Width = w;
				Bounds.Height = h;
			}
			else
			{
				// No child: size is just the padding itself.
				var (w, h) = constraints.Constrain(
					PaddingInsets.Horizontal,
					PaddingInsets.Vertical);
				Bounds.Width = w;
				Bounds.Height = h;
			}

			return (Bounds.Width, Bounds.Height);
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
