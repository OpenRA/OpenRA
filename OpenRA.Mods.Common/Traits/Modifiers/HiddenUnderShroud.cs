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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum VisibilityType { Footprint, CenterPosition }

	[Desc("The actor stays invisible under the shroud.")]
	public class HiddenUnderShroudInfo : ITraitInfo, IDefaultVisibilityInfo
	{
		[Desc("Players with these stances can always see the actor.")]
		public readonly Stance AlwaysVisibleStances = Stance.Ally;

		[Desc("Possible values are CenterPosition (reveal when the center is visible) and ",
			"Footprint (reveal when any footprint cell is visible).")]
		public readonly VisibilityType Type = VisibilityType.Footprint;

		public virtual object Create(ActorInitializer init) { return new HiddenUnderShroud(this); }
	}

	public class HiddenUnderShroud : IDefaultVisibility, IRenderModifier
	{
		protected readonly HiddenUnderShroudInfo Info;

		public HiddenUnderShroud(HiddenUnderShroudInfo info)
		{
			Info = info;
		}

		protected virtual bool IsVisibleInner(Actor self, Player byPlayer)
		{
			if (Info.Type == VisibilityType.Footprint)
				return byPlayer.Shroud.AnyExplored(self.OccupiesSpace.OccupiedCells());

			return byPlayer.Shroud.IsExplored(self.CenterPosition);
		}

		public bool IsVisible(Actor self, Player byPlayer)
		{
			if (byPlayer == null)
				return true;

			var stance = self.Owner.Stances[byPlayer];
			return Info.AlwaysVisibleStances.HasStance(stance) || IsVisibleInner(self, byPlayer);
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return IsVisible(self, self.World.RenderPlayer) ? r : SpriteRenderable.None;
		}
	}
}
