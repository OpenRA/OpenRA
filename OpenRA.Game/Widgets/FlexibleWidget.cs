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
	/// Flutter-style widget that takes a proportional share of the remaining main-axis
	/// space inside a <see cref="RowWidget"/> or <see cref="ColumnWidget"/>, but
	/// unlike <see cref="ExpandedWidget"/> it does NOT force its child to fill that
	/// allocation — the child may be smaller (<c>FlexFit.loose</c>).
	/// Equivalent to Flutter's <c>Flexible</c> widget with <c>FlexFit.loose</c>.
	/// <para>
	/// The parent <see cref="LinearWidget"/> detects this widget via <see cref="IFlexible"/>
	/// and allocates it a proportional share of the free space, then measures it with a
	/// loose constraint on the main axis (0 .. allocation) so the child can be smaller.
	/// </para>
	/// </summary>
	public class FlexibleWidget : Widget, IFlexible
	{
		/// <summary>
		/// Flex factor relative to sibling <see cref="IFlexible"/> widgets.
		/// Defaults to 1 (equal share). Must be greater than zero.
		/// </summary>
		public float FlexFactor = 1f;

		float IFlexible.Flex => FlexFactor;
		bool IFlexible.FitTight => Tight;

		/// <summary>
		/// When true the widget behaves like <see cref="ExpandedWidget"/> (tight fit).
		/// When false (default) the child may be smaller than the allocated space.
		/// </summary>
		public bool Tight = false;

		public FlexibleWidget() { }

		public FlexibleWidget(FlexibleWidget other)
			: base(other)
		{
			FlexFactor = other.FlexFactor;
			Tight = other.Tight;
		}

		public override FlexibleWidget Clone() { return new FlexibleWidget(this); }

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			int w, h;
			if (Children.Count > 0)
			{
				var child = Children[0];
				var (cw, ch) = child.Measure(constraints);
				(w, h) = constraints.Constrain(cw, ch);
				child.Bounds.X = 0;
				child.Bounds.Y = 0;
			}
			else
			{
				(w, h) = constraints.Constrain(constraints.MinWidth, constraints.MinHeight);
			}

			Bounds.Width = w;
			Bounds.Height = h;
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
