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
	/// Marker interface implemented by widgets that want to claim a proportional
	/// share of the free main-axis space inside a <see cref="LinearWidget"/>.
	/// Equivalent to Flutter's <c>Flexible</c> / <c>Expanded</c> concept.
	/// </summary>
	public interface IFlexible
	{
		/// <summary>
		/// The flex factor: the fraction of remaining space this widget should receive
		/// relative to other <see cref="IFlexible"/> siblings.
		/// A value of 1 means "take one equal share".
		/// </summary>
		float Flex { get; }

		/// <summary>
		/// When true the widget is measured with a tight main-axis constraint equal to
		/// its allocation (<c>FlexFit.tight</c>, equivalent to <see cref="ExpandedWidget"/>).
		/// When false it is measured with a loose constraint (0 .. allocation),
		/// allowing the child to be smaller (<c>FlexFit.loose</c>).
		/// </summary>
		bool FitTight { get; }
	}
}
