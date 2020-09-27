#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The actor stays invisible under the shroud.")]
	public class HiddenUnderShroudInfo : TraitInfo, IDefaultVisibilityInfo
	{
		[Desc("Players with these relationships can always see the actor.")]
		public readonly PlayerRelationship AlwaysVisibleRelationships = PlayerRelationship.Ally;

		[Desc("Possible values are CenterPosition (reveal when the center is visible) and ",
			"Footprint (reveal when any footprint cell is visible).")]
		public readonly VisibilityType Type = VisibilityType.Footprint;

		public override object Create(ActorInitializer init) { return new HiddenUnderShroud(this); }
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

			var pos = self.CenterPosition;
			if (Info.Type == VisibilityType.GroundPosition)
				pos -= new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(pos));

			return byPlayer.Shroud.IsExplored(pos);
		}

		public bool IsVisible(Actor self, Player byPlayer)
		{
			if (byPlayer == null)
				return true;

			var stance = self.Owner.RelationshipWith(byPlayer);
			return Info.AlwaysVisibleRelationships.HasStance(stance) || IsVisibleInner(self, byPlayer);
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return IsVisible(self, self.World.RenderPlayer) ? r : SpriteRenderable.None;
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}
}
