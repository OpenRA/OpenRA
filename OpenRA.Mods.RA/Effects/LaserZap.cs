#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Graphics;

namespace OpenRA.Mods.RA.Effects
{
	[Desc("Not a sprite, but an engine effect.")]
	class LaserZapInfo : IProjectileInfo
	{
		public readonly int BeamWidth = 2;
		public readonly int BeamDuration = 10;
		public readonly bool UsePlayerColor = false;
		[Desc("Laser color in (A,)R,G,B.")]
		public readonly Color Color = Color.Red;
		[Desc("Impact animation. Requires a regular animation with idle: sequence instead of explosion special case.")]
		public readonly string HitAnim = null;
		public readonly string HitAnimPalette = "effect";

		public IEffect Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.SourceActor.Owner.Color.RGB : Color;
			return new LaserZap(args, this, c);
		}
	}

	class LaserZap : IEffect
	{
		ProjectileArgs args;
		LaserZapInfo info;
		int ticks = 0;
		Color color;
		bool doneDamage;
		bool animationComplete;
		Animation hitanim;
		WPos target;

		public LaserZap(ProjectileArgs args, LaserZapInfo info, Color color)
		{
			this.args = args;
			this.info = info;
			this.color = color;
			this.target = args.PassiveTarget;

			if (info.HitAnim != null)
				this.hitanim = new Animation(args.SourceActor.World, info.HitAnim);
		}

		public void Tick(World world)
		{
			// Beam tracks target
			if (args.GuidedTarget.IsValidFor(args.SourceActor))
				target = args.GuidedTarget.CenterPosition;

			if (!doneDamage)
			{
				if (hitanim != null)
					hitanim.PlayThen("idle", () => animationComplete = true);

				args.Weapon.Impact(target, args.SourceActor, args.FirepowerModifier);
				doneDamage = true;
			}

			if (hitanim != null)
				hitanim.Tick();

			if (++ticks >= info.BeamDuration && animationComplete)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.world.FogObscures(wr.world.Map.CellContaining(target)) &&
				wr.world.FogObscures(wr.world.Map.CellContaining(args.Source)))
				yield break;

			if (ticks < info.BeamDuration)
			{
				var rc = Color.FromArgb((info.BeamDuration - ticks) * 255 / info.BeamDuration, color);
				yield return new BeamRenderable(args.Source, 0, target - args.Source, info.BeamWidth, rc);
			}

			if (hitanim != null)
				foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
