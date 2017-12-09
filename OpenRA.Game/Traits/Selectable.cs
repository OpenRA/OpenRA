#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	[Desc("This actor is selectable. Defines bounds of selectable area, selection class and selection priority.")]
	public class SelectableInfo : InteractableInfo
	{
		public readonly int Priority = 10;

		[Desc("All units having the same selection class specified will be selected with select-by-type commands (e.g. double-click). "
		+ "Defaults to the actor name when not defined or inherited.")]
		public readonly string Class = null;

		[VoiceReference] public readonly string Voice = "Select";

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
