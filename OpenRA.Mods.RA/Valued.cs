#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("How much the unit is worth.")]
	public class ValuedInfo : TraitInfo<Valued>
	{
		[Desc("Used in production, but also for bounties so remember to set it > 0 even for NPCs.")]
		public readonly int Cost = 0;
	}

	public class Valued { }

	[Desc("Shown in the build palette widget.")]
	public class TooltipInfo : ITraitInfo
	{
		public readonly string Description = "";
		public readonly string Name = "";
		[Desc("Defaults to actor name + icon suffix.")]
		public readonly string Icon = null;

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
