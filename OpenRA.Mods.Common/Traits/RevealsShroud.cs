#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Traits
{
	public class RevealsShroudInfo : AffectsShroudInfo
	{
		public override object Create(ActorInitializer init) { return new RevealsShroud(init.Self, this); }
	}

	public class RevealsShroud : AffectsShroud
	{
		public RevealsShroud(Actor self, RevealsShroudInfo info)
			: base(self, info) { }
		protected override void AddCellsToPlayerShroud(Actor self, Player p, PPos[] uv) { p.Shroud.AddProjectedVisibility(self, uv); }
		protected override void RemoveCellsFromPlayerShroud(Actor self, Player p) { p.Shroud.RemoveVisibility(self); }
	}
}
