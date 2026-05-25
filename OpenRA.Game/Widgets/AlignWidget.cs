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
	/// Flutter-style widget that aligns a single child within itself using fractional
	/// offsets on each axis. Equivalent to Flutter's <c>Align</c> widget.
	/// <para>
	/// <see cref="AlignX"/> and <see cref="AlignY"/> use Flutter's convention:
	/// -1.0 = start (left/top), 0.0 = center, 1.0 = end (right/bottom).
	/// The widget fills the available space from the parent by default (like Flutter).
	/// Set <see cref="ShrinkWrap"/> to true to wrap tightly around the child instead.
	/// </para>
	/// </summary>
	public class AlignWidget : Widget
	{
		/// <summary>
		/// Horizontal alignment factor in [-1, 1]: -1 = left, 0 = center, 1 = right.
		/// </summary>
		public float AlignX = -1f;

		/// <summary>
		/// Vertical alignment factor in [-1, 1]: -1 = top, 0 = center, 1 = bottom.
		/// </summary>
		public float AlignY = -1f;

		/// <summary>
		/// When true the widget sizes itself to the child plus the fractional offset
		/// (equivalent to Flutter's <c>widthFactor</c>/<c>heightFactor != null</c>).
		/// When false (default) the widget fills the parent's available space.
		/// </summary>
		public bool ShrinkWrap = false;

		public AlignWidget() { }

		public AlignWidget(AlignWidget other)
			: base(other)
		{
			AlignX = other.AlignX;
			AlignY = other.AlignY;
			ShrinkWrap = other.ShrinkWrap;
		}

		public override AlignWidget Clone() { return new AlignWidget(this); }

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			int w, h;

			if (Children.Count > 0)
			{
				var child = Children[0];
				var (cw, ch) = child.Measure(BoxConstraints.Loose(constraints.MaxWidth, constraints.MaxHeight));

				if (ShrinkWrap)
				{
					(w, h) = constraints.Constrain(cw, ch);
				}
				else
				{
					w = constraints.MaxWidth < BoxConstraints.Unbounded ? constraints.MaxWidth : cw;
					h = constraints.MaxHeight < BoxConstraints.Unbounded ? constraints.MaxHeight : ch;
					(w, h) = constraints.Constrain(w, h);
				}

				// AlignX/AlignY in [-1,1] — map to pixel offset within (w - cw, h - ch).
				// factor -1 => 0, factor 0 => (dim - childDim) / 2, factor 1 => dim - childDim.
				child.Bounds.X = (int)((AlignX + 1f) / 2f * (w - cw));
				child.Bounds.Y = (int)((AlignY + 1f) / 2f * (h - ch));
			}
			else
			{
				w = constraints.MaxWidth < BoxConstraints.Unbounded ? constraints.MaxWidth : 0;
				h = constraints.MaxHeight < BoxConstraints.Unbounded ? constraints.MaxHeight : 0;
				(w, h) = constraints.Constrain(w, h);
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
