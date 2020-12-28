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
	[Desc("Display a colored overlay when a timed condition is active.")]
	public class WithColoredOverlayInfo : ConditionalTraitInfo
	{
		[Desc("Color to overlay.")]
		public readonly Color Color = Color.FromArgb(128, 128, 0, 0);

		public override object Create(ActorInitializer init) { return new WithColoredOverlay(this); }
	}

	public class WithColoredOverlay : ConditionalTrait<WithColoredOverlayInfo>, IRenderModifier
	{
		readonly float3 tint;
		readonly float alpha;

		public WithColoredOverlay(WithColoredOverlayInfo info)
			: base(info)
		{
			tint = new float3(info.Color.R, info.Color.G, info.Color.B) / 255f;
			alpha = info.Color.A / 255f;
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				return r;

			return ModifiedRender(self, wr, r);
		}

		IEnumerable<IRenderable> ModifiedRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			foreach (var a in r)
			{
				yield return a;

				if (!a.IsDecoration && a is IModifyableRenderable ma)
					yield return ma.WithTint(tint, ma.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(alpha);
			}
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}
}
