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
	/// Flutter-style widget that expands to fill the remaining main-axis space
	/// inside a <see cref="RowWidget"/> or <see cref="ColumnWidget"/>.
	/// Equivalent to Flutter's <c>Expanded</c> widget (which is <c>Flexible</c> with
	/// <c>FlexFit.tight</c>).
	/// <para>
	/// The parent <see cref="LinearWidget"/> detects this widget via <see cref="IFlexible"/>
	/// and allocates it a proportional share of the free space before measuring it
	/// with tight constraints on the main axis.
	/// </para>
	/// </summary>
	public class ExpandedWidget : Widget, IFlexible
	{
		/// <summary>
		/// Flex factor relative to sibling <see cref="IFlexible"/> widgets.
		/// Defaults to 1 (equal share). Must be greater than zero.
		/// </summary>
		public float FlexFactor = 1f;

		float IFlexible.Flex => FlexFactor;
		bool IFlexible.FitTight => true;

		public ExpandedWidget() { }

		public ExpandedWidget(ExpandedWidget other)
			: base(other)
		{
			FlexFactor = other.FlexFactor;
		}

		public override ExpandedWidget Clone() { return new ExpandedWidget(this); }

		/// <summary>
		/// Expanded does not draw anything itself; it merely acts as a size-and-position
		/// proxy for its single child.
		/// </summary>
		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			// The parent Row/Column already determined our main-axis size via constraints.
			var (w, h) = constraints.Constrain(
				Bounds.Width > 0 ? Bounds.Width : constraints.MinWidth,
				Bounds.Height > 0 ? Bounds.Height : constraints.MinHeight);

			Bounds.Width = w;
			Bounds.Height = h;

			// Measure and position the single child (if any) with tight constraints.
			if (Children.Count > 0)
			{
				var child = Children[0];
				child.Measure(BoxConstraints.Tight(w, h));
				child.Bounds.X = 0;
				child.Bounds.Y = 0;
			}

			return (w, h);
		}

		public override void PerformLayoutIfNeeded()
		{
			if (!layoutDirty)
				return;

			Measure(BoxConstraints.Tight(Bounds.Width, Bounds.Height));
			layoutDirty = false;

			foreach (var child in Children)
				child.PerformLayoutIfNeeded();
		}
	}
}
