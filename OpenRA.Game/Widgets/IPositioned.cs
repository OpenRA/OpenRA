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
	/// Marker interface implemented by <see cref="PositionedWidget"/> to tell a parent
	/// <see cref="StackWidget"/> that this child should be placed at explicit edge offsets
	/// rather than aligned with the default stack alignment.
	/// Equivalent to Flutter's <c>Positioned</c> widget concept.
	/// </summary>
	public interface IPositioned
	{
		/// <summary>Distance from the left edge of the stack. Null = unspecified.</summary>
		int? Left { get; }

		/// <summary>Distance from the top edge of the stack. Null = unspecified.</summary>
		int? Top { get; }

		/// <summary>Distance from the right edge of the stack. Null = unspecified.</summary>
		int? Right { get; }

		/// <summary>Distance from the bottom edge of the stack. Null = unspecified.</summary>
		int? Bottom { get; }
	}
}
