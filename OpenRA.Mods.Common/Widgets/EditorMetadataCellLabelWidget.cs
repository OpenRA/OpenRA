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
	/// <summary>Label that draws metadata cells but does not block scrollbar or row chrome mouse input.</summary>
	public class EditorMetadataCellLabelWidget : LabelWidget
	{
		[ObjectCreator.UseCtor]
		public EditorMetadataCellLabelWidget(ModData modData)
			: base(modData)
		{
			IgnoreMouseOver = true;
		}

		protected EditorMetadataCellLabelWidget(EditorMetadataCellLabelWidget other)
			: base(other)
		{
			IgnoreMouseOver = true;
		}

		public override EditorMetadataCellLabelWidget Clone() => new(this);

		public override bool EventBoundsContains(int2 location) => false;
	}
}
