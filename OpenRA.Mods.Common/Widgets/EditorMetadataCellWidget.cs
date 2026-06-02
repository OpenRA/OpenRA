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

using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	/// <summary>Metadata spreadsheet cell container that does not capture mouse hits on empty padding.</summary>
	public class EditorMetadataCellWidget : ContainerWidget
	{
		public EditorMetadataCellWidget() { }

		protected EditorMetadataCellWidget(EditorMetadataCellWidget other)
			: base(other) { }

		public override EditorMetadataCellWidget Clone() => new(this);

		public override bool EventBoundsContains(int2 location)
		{
			foreach (var child in Children)
			{
				if (child.IsVisible() && child.EventBoundsContains(location))
					return true;
			}

			return false;
		}
	}
}
