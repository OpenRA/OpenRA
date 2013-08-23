#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	[Desc("Not a sprite, but an engine effect.")]
	class LaserZapInfo : IProjectileInfo
	{
		public readonly int BeamWidth = 2;
		public readonly int BeamDuration = 10;
		public readonly bool UsePlayerColor = false;
		public readonly Color Color = Color.Red;
		public readonly string HitAnim = null;

		public IEffect Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.sourceActor.Owner.Color.RGB : Color;
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
			this.target = args.passiveTarget;

			if (info.HitAnim != null)
				this.hitanim = new Animation(info.HitAnim);
		}

		public void Tick(World world)
		{
			// Beam tracks target
			if (args.guidedTarget.IsValidFor(args.sourceActor))
				target = args.guidedTarget.CenterPosition;

			if (!doneDamage)
			{
				if (hitanim != null)
					hitanim.PlayThen("idle", () => animationComplete = true);

				Combat.DoImpacts(target, args.sourceActor, args.weapon, args.firepowerModifier);
				doneDamage = true;
			}

			if (hitanim != null)
				hitanim.Tick();

			if (++ticks >= info.BeamDuration && animationComplete)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (ticks < info.BeamDuration)
			{
				var rc = Color.FromArgb((info.BeamDuration - ticks) * 255 / info.BeamDuration, color);
				yield return new BeamRenderable(args.source, 0, target - args.source, info.BeamWidth, rc);
			}

			if (hitanim != null)
				foreach (var r in hitanim.Render(target, wr.Palette("effect")))
					yield return r;
		}
	}
}
