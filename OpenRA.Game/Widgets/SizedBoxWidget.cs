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
	/// Flutter-style widget that forces its single child to have a specific size,
	/// or creates fixed-size empty space when it has no children.
	/// Equivalent to Flutter's <c>SizedBox</c> widget.
	/// <para>
	/// The desired width and height are declared via the standard YAML <c>Width</c>
	/// and <c>Height</c> properties (already present on the base <see cref="Widget"/>
	/// class).  The child is measured with tight constraints equal to those dimensions.
	/// </para>
	/// Use <c>SizedBox</c> to:
	/// <list type="bullet">
	///   <item>Add a fixed-size gap between siblings in a <see cref="RowWidget"/> or <see cref="ColumnWidget"/>.</item>
	///   <item>Constrain a child to an exact size.</item>
	/// </list>
	/// </summary>
	public class SizedBoxWidget : Widget
	{
		public SizedBoxWidget() { }

		public SizedBoxWidget(SizedBoxWidget other)
			: base(other) { }

		public override SizedBoxWidget Clone() { return new SizedBoxWidget(this); }

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			// Our declared size (from YAML Width/Height) is the desired tight size.
			// Clamp it to parent constraints in case the parent is tighter.
			var (w, h) = constraints.Constrain(Bounds.Width, Bounds.Height);
			Bounds.Width = w;
			Bounds.Height = h;

			// Measure the single child with tight constraints equal to our size.
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
