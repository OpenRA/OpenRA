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
	/// Flutter-style widget that lays out its children along the vertical axis.
	/// Equivalent to Flutter's <c>Column</c> widget.
	/// </summary>
	public class ColumnWidget : LinearWidget
	{
		protected override bool IsRow => false;

		public ColumnWidget() { }

		public ColumnWidget(ColumnWidget other)
			: base(other) { }

		public override ColumnWidget Clone() { return new ColumnWidget(this); }
	}
}
