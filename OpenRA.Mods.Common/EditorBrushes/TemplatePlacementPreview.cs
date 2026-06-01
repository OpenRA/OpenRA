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

namespace OpenRA.Mods.Common.EditorBrushes
{
	public enum TilePlacementPreviewDisplayMode
	{
		Current,
		Original
	}

	public readonly struct TemplatePlacementPreview
	{
		public readonly CPos Anchor;
		public readonly ushort TemplateType;

		public TemplatePlacementPreview(CPos anchor, ushort templateType)
		{
			Anchor = anchor;
			TemplateType = templateType;
		}
	}

	public readonly struct TemplatePlacementPreviewDisplay
	{
		public readonly TemplatePlacementPreview Placement;
		public readonly TilePlacementPreviewDisplayMode Mode;

		public TemplatePlacementPreviewDisplay(
			TemplatePlacementPreview placement,
			TilePlacementPreviewDisplayMode mode)
		{
			Placement = placement;
			Mode = mode;
		}
	}
}
