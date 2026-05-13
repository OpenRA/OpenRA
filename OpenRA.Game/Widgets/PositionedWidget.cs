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
	/// Flutter-style widget used as a direct child of a <see cref="StackWidget"/> to
	/// declare explicit edge offsets. Equivalent to Flutter's <c>Positioned</c> widget.
	/// <para>
	/// Specify any combination of <see cref="PositionLeft"/>, <see cref="PositionTop"/>,
	/// <see cref="PositionRight"/>, <see cref="PositionBottom"/>. When two opposing edges
	/// are both specified the child is sized to fit between them.
	/// </para>
	/// </summary>
	public class PositionedWidget : Widget, IPositioned
	{
		// YAML uses "PositionLeft" etc. to avoid clashing with Widget.Bounds.X/Y.
		public int? PositionLeft;
		public int? PositionTop;
		public int? PositionRight;
		public int? PositionBottom;

		int? IPositioned.Left => PositionLeft;
		int? IPositioned.Top => PositionTop;
		int? IPositioned.Right => PositionRight;
		int? IPositioned.Bottom => PositionBottom;

		public PositionedWidget() { }

		public PositionedWidget(PositionedWidget other)
			: base(other)
		{
			PositionLeft = other.PositionLeft;
			PositionTop = other.PositionTop;
			PositionRight = other.PositionRight;
			PositionBottom = other.PositionBottom;
		}

		public override PositionedWidget Clone() { return new PositionedWidget(this); }

		/// <summary>
		/// Positioned just passes the constraints to its single child and adopts its size.
		/// The actual edge-offset positioning is done by the parent <see cref="StackWidget"/>.
		/// </summary>
		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			var (w, h) = constraints.Constrain(
				Bounds.Width > 0 ? Bounds.Width : constraints.MinWidth,
				Bounds.Height > 0 ? Bounds.Height : constraints.MinHeight);

			Bounds.Width = w;
			Bounds.Height = h;

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
