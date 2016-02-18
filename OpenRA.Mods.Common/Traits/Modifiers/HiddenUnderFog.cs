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
	[Desc("The actor stays invisible under fog of war.")]
	public class HiddenUnderFogInfo : HiddenUnderShroudInfo
	{
		public override object Create(ActorInitializer init) { return new HiddenUnderFog(this); }
	}

	public class HiddenUnderFog : HiddenUnderShroud
	{
		public HiddenUnderFog(HiddenUnderFogInfo info)
			: base(info) { }

		protected override bool IsVisibleInner(Actor self, Player byPlayer)
		{
			// If fog is disabled visibility is determined by shroud
			if (!byPlayer.Shroud.FogEnabled)
				return base.IsVisibleInner(self, byPlayer);

			if (Info.Type == VisibilityType.Footprint)
				return byPlayer.Shroud.AnyVisible(self.OccupiesSpace.OccupiedCells());

			return byPlayer.Shroud.IsVisible(self.CenterPosition);
		}
	}
}
