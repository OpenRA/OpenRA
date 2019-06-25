#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Traits
{
	[Flags]
	public enum SelectionPriorityModifiers
	{
		None = 0,
		Ctrl = 1,
		Alt = 2
	}

	[Desc("This actor is selectable. Defines bounds of selectable area, selection class, selection priority and selection priority modifiers.")]
	public class SelectableInfo : InteractableInfo
	{
		public readonly int Priority = 10;

		[Desc("Allow selection priority to be modified using a hotkey.",
			"Valid values are None (priority is not affected by modifiers)",
			"Ctrl (priority is raised when Ctrl pressed) and",
			"Alt (priority is raised when Alt pressed).")]
		public readonly SelectionPriorityModifiers PriorityModifiers = SelectionPriorityModifiers.None;

		[Desc("All units having the same selection class specified will be selected with select-by-type commands (e.g. double-click). "
		+ "Defaults to the actor name when not defined or inherited.")]
		public readonly string Class = null;

		[VoiceReference]
		public readonly string Voice = "Select";

		public override object Create(ActorInitializer init) { return new Selectable(init.Self, this); }
	}

	public class Selectable : Interactable
	{
		public readonly string Class = null;
		public readonly SelectableInfo Info;

		public Selectable(Actor self, SelectableInfo info)
			: base(info)
		{
			Class = string.IsNullOrEmpty(info.Class) ? self.Info.Name : info.Class;
			Info = info;
		}
	}
}
