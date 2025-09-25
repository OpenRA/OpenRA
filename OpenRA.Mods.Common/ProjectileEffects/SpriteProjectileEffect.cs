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

using System;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.ProjectileEffects
{
	public sealed class SpriteProjectileEffectInfo : IProjectileEffectInfo
	{
		[FieldLoader.Require]
		[Desc("Image to display.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Sequence to play when launched. Skipped if null or empty.")]
		public readonly string StartSequence = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of Image from this list while this projectile is moving.")]
		public readonly string[] Sequences = ["idle"];

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("The palette used to draw this projectile.")]
		public readonly string Palette = "effect";

		[Desc("Palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Color to draw shadow if Shadow is true.")]
		public readonly Color ShadowColor = Color.FromArgb(140, 0, 0, 0);

		IProjectileEffect IProjectileEffectInfo.Create(ProjectileArgs args, Func<WAngle> facing)
		{
			return new SpriteProjectileEffect(args, this, facing);
		}
	}

	public sealed class SpriteProjectileEffect : IProjectileEffect
	{
		readonly SpriteProjectileEffectInfo info;
		readonly Animation animation;
		readonly string paletteName;
		readonly float3 shadowColor;
		readonly float shadowAlpha;
		PaletteReference palette;
		WPos pos;

		public SpriteProjectileEffect(ProjectileArgs args, SpriteProjectileEffectInfo info, Func<WAngle> facing)
		{
			this.info = info;
			paletteName = info.Palette;
			if (paletteName != null && info.IsPlayerPalette)
				paletteName += args.SourceActor.Owner.InternalName;

			shadowColor = new float3(info.ShadowColor.R, info.ShadowColor.G, info.ShadowColor.B) / 255f;
			shadowAlpha = info.ShadowColor.A / 255f;

			animation = new Animation(args.SourceActor.Owner.World, info.Image, facing);
			if (!string.IsNullOrEmpty(info.StartSequence))
				animation.PlayThen(info.StartSequence, () => animation.PlayRepeating(this.info.Sequences.Random(args.SourceActor.World.SharedRandom)));
			else
				animation.PlayRepeating(info.Sequences.Random(args.SourceActor.World.SharedRandom));

			pos = args.Source;
		}

		void IProjectileEffect.Tick(World world, WPos pos, WRot orientation)
		{
			this.pos = pos;
			animation.Tick();
		}

		IEnumerable<IRenderable> IProjectileEffect.Render(WorldRenderer wr)
		{
			var world = wr.World;
			if (!world.FogObscures(pos))
			{
				palette ??= wr.Palette(paletteName);
				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					var shadowPos = pos - new WVec(0, 0, dat.Length);
					foreach (var r in animation.Render(shadowPos, palette))
						yield return ((IModifyableRenderable)r)
							.WithTint(shadowColor, ((IModifyableRenderable)r).TintModifiers | TintModifiers.ReplaceColor)
							.WithAlpha(shadowAlpha);
				}

				foreach (var r in animation.Render(pos, palette))
					yield return r;
			}
		}

		void IProjectileEffect.Destroy(World world, WPos pos) { }
	}
}
