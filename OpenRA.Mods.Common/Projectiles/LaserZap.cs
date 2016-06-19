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
using System.Drawing;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Not a sprite, but an engine effect.")]
	public class LaserZapInfo : IProjectileInfo
	{
		[Desc("The width of the zap.")]
		public readonly WDist Width = new WDist(86);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape Shape = BeamRenderableShape.Cylindrical;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		public readonly int BeamDuration = 10;

		public readonly bool UsePlayerColor = false;

		[Desc("Laser color in (A,)R,G,B.")]
		public readonly Color Color = Color.Red;

		[Desc("Impact animation.")]
		public readonly string HitAnim = null;

		[Desc("Sequence of impact animation to use.")]
		[SequenceReference("HitAnim")] public readonly string HitAnimSequence = "idle";

		[PaletteReference] public readonly string HitAnimPalette = "effect";

		public IProjectile Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.SourceActor.Owner.Color.RGB : Color;
			return new LaserZap(args, this, c);
		}
	}

	public class LaserZap : IProjectile
	{
		readonly ProjectileArgs args;
		readonly LaserZapInfo info;
		readonly Animation hitanim;
		int ticks = 0;
		Color color;
		bool doneDamage;
		bool animationComplete;
		WPos target;

		public LaserZap(ProjectileArgs args, LaserZapInfo info, Color color)
		{
			this.args = args;
			this.info = info;
			this.color = color;
			target = args.PassiveTarget;

			if (!string.IsNullOrEmpty(info.HitAnim))
				hitanim = new Animation(args.SourceActor.World, info.HitAnim);
		}

		public void Tick(World world)
		{
			// Beam tracks target
			if (args.GuidedTarget.IsValidFor(args.SourceActor))
				target = args.GuidedTarget.CenterPosition;

			if (!doneDamage)
			{
				if (hitanim != null)
					hitanim.PlayThen(info.HitAnimSequence, () => animationComplete = true);

				args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);
				doneDamage = true;
			}

			if (hitanim != null)
				hitanim.Tick();

			if (++ticks >= info.BeamDuration && animationComplete)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(args.Source))
				yield break;

			if (ticks < info.BeamDuration)
			{
				var rc = Color.FromArgb((info.BeamDuration - ticks) * 255 / info.BeamDuration, color);
				yield return new BeamRenderable(args.Source, info.ZOffset, target - args.Source, info.Shape, info.Width, rc);
			}

			if (hitanim != null)
				foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
