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
	/// Flutter-style layout constraints passed from parent to child during layout.
	/// Constraints go down (parent → child), sizes go up (child → parent).
	/// </summary>
	public readonly struct BoxConstraints : IEquatable<BoxConstraints>
	{
		// Sentinel value used for unconstrained axes (FitContent / wrap content).
		// Kept well below int.MaxValue to avoid overflow in arithmetic.
		public const int Unbounded = 100_000;

		public readonly int MinWidth;
		public readonly int MaxWidth;
		public readonly int MinHeight;
		public readonly int MaxHeight;

		public BoxConstraints(int minWidth, int maxWidth, int minHeight, int maxHeight)
		{
			MinWidth = minWidth;
			MaxWidth = maxWidth;
			MinHeight = minHeight;
			MaxHeight = maxHeight;
		}

		/// <summary>Both axes are exactly the given size (tight constraint).</summary>
		public static BoxConstraints Tight(int width, int height)
			=> new(width, width, height, height);

		/// <summary>Both axes are free from 0 to the given maxima (loose constraint).</summary>
		public static BoxConstraints Loose(int maxWidth, int maxHeight)
			=> new(0, maxWidth, 0, maxHeight);

		/// <summary>No constraint on either axis. Used when measuring intrinsic content size.</summary>
		public static BoxConstraints Infinite()
			=> new(0, Unbounded, 0, Unbounded);

		/// <summary>True when min == max on both axes (parent dictates exact size).</summary>
		public bool IsTight => MinWidth == MaxWidth && MinHeight == MaxHeight;

		/// <summary>
		/// Returns tighter constraints by subtracting the given insets from both axes.
		/// The result is clamped to remain non-negative.
		/// </summary>
		public BoxConstraints Deflate(EdgeInsets insets)
		{
			return new BoxConstraints(
				Math.Max(0, MinWidth - insets.Horizontal),
				Math.Max(0, MaxWidth - insets.Horizontal),
				Math.Max(0, MinHeight - insets.Vertical),
				Math.Max(0, MaxHeight - insets.Vertical));
		}

		/// <summary>
		/// Clamps (width, height) so they satisfy these constraints.
		/// </summary>
		public (int Width, int Height) Constrain(int width, int height)
		{
			return (Math.Clamp(width, MinWidth, MaxWidth),
				Math.Clamp(height, MinHeight, MaxHeight));
		}

		/// <summary>
		/// Returns constraints for children on the cross axis while leaving the
		/// main axis unconstrained (used by Row/Column to measure non-Expanded children).
		/// </summary>
		public BoxConstraints WithUnboundedMain(bool isRow)
		{
			// Cross-axis min is 0 so children are not forced to fill the container.
			// The Row/Column parent will align or stretch them afterwards.
			return isRow
				? new BoxConstraints(0, Unbounded, 0, MaxHeight)
				: new BoxConstraints(0, MaxWidth, 0, Unbounded);
		}

		/// <summary>
		/// Returns a tight constraint for the given main-axis allocation while
		/// preserving the cross-axis constraint (used by Row/Column for Expanded children).
		/// </summary>
		public BoxConstraints WithTightMain(bool isRow, int mainSize)
		{
			return isRow
				? new BoxConstraints(mainSize, mainSize, MinHeight, MaxHeight)
				: new BoxConstraints(MinWidth, MaxWidth, mainSize, mainSize);
		}

		/// <summary>
		/// Returns a loose constraint (0 .. allocation) for the main axis while
		/// preserving the cross-axis constraint (used by Row/Column for Flexible children
		/// with <c>FlexFit.loose</c>).
		/// </summary>
		public BoxConstraints WithLooseMain(bool isRow, int mainSize)
		{
			return isRow
				? new BoxConstraints(0, mainSize, MinHeight, MaxHeight)
				: new BoxConstraints(MinWidth, MaxWidth, 0, mainSize);
		}

		public bool Equals(BoxConstraints other)
			=> MinWidth == other.MinWidth && MaxWidth == other.MaxWidth
			&& MinHeight == other.MinHeight && MaxHeight == other.MaxHeight;

		public override bool Equals(object obj)
			=> obj is BoxConstraints other && Equals(other);

		public override int GetHashCode()
			=> HashCode.Combine(MinWidth, MaxWidth, MinHeight, MaxHeight);

		public static bool operator ==(BoxConstraints left, BoxConstraints right) => left.Equals(right);
		public static bool operator !=(BoxConstraints left, BoxConstraints right) => !left.Equals(right);

		public override string ToString()
			=> $"BoxConstraints({MinWidth}..{MaxWidth} x {MinHeight}..{MaxHeight})";
	}
}
