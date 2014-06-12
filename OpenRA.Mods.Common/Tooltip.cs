#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Shown in the build palette widget.")]
	public class TooltipInfo : ITraitInfo
	{
		[Translate] public readonly string Description = "";
		[Translate] public readonly string Name = "";

		[Desc("Sequence of the actor that contains the cameo.")]
		public readonly string Icon = "icon";

		public virtual object Create(ActorInitializer init) { return new Tooltip(init.self, this); }
	}

	public class Tooltip : IToolTip
	{
		Actor self;
		TooltipInfo Info;

		public string Name() { return Info.Name; }
		public Player Owner() { return self.Owner; }

		public Tooltip(Actor self, TooltipInfo info)
		{
			this.self = self;
			Info = info;
		}
	}
}