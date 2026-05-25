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
	/// How children are placed along the main axis of a Row or Column.
	/// Mirrors Flutter's MainAxisAlignment.
	/// </summary>
	public enum MainAxisAlignment
	{
		/// <summary>Pack children at the start of the main axis.</summary>
		Start,

		/// <summary>Pack children at the end of the main axis.</summary>
		End,

		/// <summary>Pack children at the center of the main axis.</summary>
		Center,

		/// <summary>Place free space evenly between children.</summary>
		SpaceBetween,

		/// <summary>Place free space evenly between and half-space before/after.</summary>
		SpaceAround,

		/// <summary>Place free space evenly before, between and after children.</summary>
		SpaceEvenly
	}

	/// <summary>
	/// How children are aligned along the cross axis of a Row or Column.
	/// Mirrors Flutter's CrossAxisAlignment.
	/// </summary>
	public enum CrossAxisAlignment
	{
		/// <summary>Align children at the start of the cross axis.</summary>
		Start,

		/// <summary>Align children at the end of the cross axis.</summary>
		End,

		/// <summary>Center children along the cross axis.</summary>
		Center,

		/// <summary>Stretch children to fill the cross axis.</summary>
		Stretch
	}

	/// <summary>
	/// How much space a Row or Column should occupy on its main axis.
	/// Mirrors Flutter's MainAxisSize.
	/// </summary>
	public enum MainAxisSize
	{
		/// <summary>Claim all available space on the main axis (default).</summary>
		Max,

		/// <summary>Shrink-wrap to the children's combined size on the main axis.</summary>
		Min
	}

	/// <summary>
	/// Horizontal alignment of a child within its parent's content area.
	/// Used by <see cref="ContainerWidget"/>.
	/// Mirrors the horizontal component of Flutter's <c>Alignment</c>.
	/// </summary>
	public enum HorizontalAlignment
	{
		/// <summary>Align child to the left edge of the content area.</summary>
		Left,

		/// <summary>Center the child horizontally in the content area.</summary>
		Center,

		/// <summary>Align child to the right edge of the content area.</summary>
		Right
	}

	/// <summary>
	/// Vertical alignment of a child within its parent's content area.
	/// Used by <see cref="ContainerWidget"/>.
	/// Mirrors the vertical component of Flutter's <c>Alignment</c>.
	/// </summary>
	public enum VerticalAlignment
	{
		/// <summary>Align child to the top edge of the content area.</summary>
		Top,

		/// <summary>Center the child vertically in the content area.</summary>
		Center,

		/// <summary>Align child to the bottom edge of the content area.</summary>
		Bottom
	}

	/// <summary>
	/// How a child should be inscribed into the available space of a <see cref="FittedBoxWidget"/>.
	/// Mirrors Flutter's <c>BoxFit</c>.
	/// </summary>
	public enum BoxFit
	{
		/// <summary>Scale uniformly so the child fits entirely within the box (letterbox).</summary>
		Contain,

		/// <summary>Scale uniformly so the child covers the entire box (may crop).</summary>
		Cover,

		/// <summary>Distort the child to fill the box exactly.</summary>
		Fill,

		/// <summary>Force the child to its natural (unscaled) size, clipped to the box.</summary>
		None,

		/// <summary>Scale to fill the width; height may overflow or underflow.</summary>
		FitWidth,

		/// <summary>Scale to fill the height; width may overflow or underflow.</summary>
		FitHeight,

		/// <summary>Like <see cref="Contain"/> but never scales the child up.</summary>
		ScaleDown
	}

	/// <summary>
	/// How the children within each run of a <see cref="WrapWidget"/> are placed
	/// along the main axis. Mirrors Flutter's <c>WrapAlignment</c>.
	/// </summary>
	public enum WrapAlignment
	{
		/// <summary>Place children at the start of each run.</summary>
		Start,

		/// <summary>Place children at the end of each run.</summary>
		End,

		/// <summary>Place children at the center of each run.</summary>
		Center,

		/// <summary>Distribute free space evenly between children.</summary>
		SpaceBetween,

		/// <summary>Distribute free space evenly between children and half-space at both ends.</summary>
		SpaceAround,

		/// <summary>Distribute free space evenly before, between, and after children.</summary>
		SpaceEvenly
	}

	/// <summary>
	/// How children within each run of a <see cref="WrapWidget"/> are aligned on the
	/// cross axis. Mirrors Flutter's <c>WrapCrossAlignment</c>.
	/// </summary>
	public enum WrapCrossAlignment
	{
		/// <summary>Align children at the start of the cross axis within their run.</summary>
		Start,

		/// <summary>Center children along the cross axis within their run.</summary>
		Center,

		/// <summary>Align children at the end of the cross axis within their run.</summary>
		End
	}
}
