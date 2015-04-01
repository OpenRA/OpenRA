#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Traits
{
	public class CreatesShroudInfo : RevealsShroudInfo
	{
		public override object Create(ActorInitializer init) { return new CreatesShroud(init.Self, this); }
	}

	public class CreatesShroud : RevealsShroud
	{
		public CreatesShroud(Actor self, CreatesShroudInfo info)
			: base(self, info)
		{
			addCellsToPlayerShroud = (p, uv) => p.Shroud.AddProjectedShroudGeneration(self, uv);
			removeCellsFromPlayerShroud = p => p.Shroud.RemoveShroudGeneration(self);
			isDisabled = () => self.IsDisabled();
		}
	}
}