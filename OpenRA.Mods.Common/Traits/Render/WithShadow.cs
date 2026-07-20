#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Clones the actor sprite with another palette below it.")]
	public class WithShadowInfo : ConditionalTraitInfo
	{
		[Desc("Color to draw shadow.")]
		public readonly Color ShadowColor = Color.FromArgb(140, 0, 0, 0);

		[Desc("Shadow position offset relative to actor position (ground level).")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Shadow Z offset relative to actor sprite.")]
		public readonly int ZOffset = -5;

		public override object Create(ActorInitializer init) { return new WithShadow(this); }
	}

	public class WithShadow : ConditionalTrait<WithShadowInfo>, IRenderModifier
	{
		readonly WithShadowInfo info;
		readonly float3 shadowColor;
		readonly float shadowAlpha;
		readonly List<IRenderable> sourceRenderables = [];
		readonly List<IRenderable> modifiedRenderables = [];
		readonly List<Rectangle> sourceBounds = [];
		readonly List<Rectangle> modifiedBounds = [];

		public WithShadow(WithShadowInfo info)
			: base(info)
		{
			this.info = info;
			shadowColor = new float3(info.ShadowColor.R, info.ShadowColor.G, info.ShadowColor.B) / 255f;
			shadowAlpha = info.ShadowColor.A / 255f;
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled || !Game.Renderer.DrawWorldShadows)
				return r;

			sourceRenderables.Clear();
			sourceRenderables.AddRange(r);

			modifiedRenderables.Clear();
			var height = self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length;
			foreach (var renderable in sourceRenderables)
				if (!renderable.IsDecoration && renderable is IModifyableRenderable modifyable)
					modifiedRenderables.Add(modifyable.WithTint(shadowColor, modifyable.TintModifiers | TintModifiers.ReplaceColor)
					.WithAlpha(shadowAlpha)
					.OffsetBy(info.Offset - new WVec(0, 0, height))
					.WithZOffset(renderable.ZOffset + height + info.ZOffset)
					.AsDecoration());

			modifiedRenderables.AddRange(sourceRenderables);
			return modifiedRenderables;
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			if (IsTraitDisabled || !Game.Renderer.DrawWorldShadows)
				return bounds;

			sourceBounds.Clear();
			sourceBounds.AddRange(bounds);

			modifiedBounds.Clear();
			modifiedBounds.AddRange(sourceBounds);
			var height = self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length;
			var offset = wr.ScreenPxOffset(info.Offset - new WVec(0, 0, height));
			foreach (var bound in sourceBounds)
				modifiedBounds.Add(new Rectangle(bound.X + offset.X, bound.Y + offset.Y, bound.Width, bound.Height));

			return modifiedBounds;
		}
	}
}
