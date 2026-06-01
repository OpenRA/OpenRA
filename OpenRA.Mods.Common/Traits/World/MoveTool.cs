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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public class MoveToolInfo : TraitInfo
	{
		[FluentReference]
		[Desc("The label to show in the tools menu.")]
		public readonly string Label = "label-tool-move";

		[Desc("The widget tree to open when the tool is selected.")]
		public readonly string PanelWidget = "MOVE_TOOL_PANEL";

		public override object Create(ActorInitializer init)
		{
			return new MoveTool(this);
		}
	}

	public class MoveTool : IEditorTool
	{
		public string Label { get; }
		public string PanelWidget { get; }
		public TraitInfo TraitInfo { get; }
		public bool IsEnabled => true;

		public MoveTool(MoveToolInfo info)
		{
			Label = info.Label;
			PanelWidget = info.PanelWidget;
			TraitInfo = info;
		}
	}
}
