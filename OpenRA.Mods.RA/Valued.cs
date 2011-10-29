#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ValuedInfo : TraitInfo<Valued>
	{
		public readonly int Cost = 0;
	}

	public class Valued { }

	public class TooltipInfo : ITraitInfo
	{
		public readonly string Description = "";
		public readonly string Name = "";
		public readonly string Icon = null;

		public virtual object Create(ActorInitializer init) { return new Tooltip(init.self, this); }
	}

	public class Tooltip : IToolTip
	{
		Actor self;
		TooltipInfo Info;

		public string Name() { return Info.Name; }
		public Player Owner() { return self.Owner; }
		public Stance Stance() { return self.World.LocalPlayer.Stances[self.Owner]; }

		public Tooltip(Actor self, TooltipInfo info)
		{
			this.self = self;
			Info = info;
		}
	}
}
