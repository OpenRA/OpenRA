#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Projectiles
{
	[Desc("Not a sprite, but an engine effect.")]
	public class ArcLaserZapInfo : IProjectileInfo, IRulesetLoaded<WeaponInfo>
	{
		[Desc("The width of the zap.")]
		public readonly WDist Width = new WDist(86);

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		public readonly int Duration = 10;

		[Desc("The angle of the arc of the beam.")]
		public readonly WAngle Angle = WAngle.FromDegrees(30);

		[Desc("Controls how fine-grained the resulting arc should be.")]
		public readonly int QuantizedSegments = 32;

		public readonly bool UsePlayerColor = false;

		[Desc("Color of the beam.")]
		public readonly Color Color = Color.Red;

		[Desc("Beam follows the target.")]
		public readonly bool TrackTarget = true;

		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Beam can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("Impact animation.")]
		public readonly string HitAnim = null;

		[Desc("Sequence of impact animation to use.")]
		[SequenceReference("HitAnim")]
		public readonly string HitAnimSequence = "idle";

		[PaletteReference]
		public readonly string HitAnimPalette = "effect";

		[Desc("Scan radius for actors with projectile-blocking trait. If set to a negative value (default), it will automatically scale",
			"to the blocker with the largest health shape. Only set custom values if you know what you're doing.")]
		public WDist BlockerScanRadius = new WDist(-1);

		public IProjectile Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.SourceActor.Owner.Color.RGB : Color;
			return new ArcLaserZap(this, args, c);
		}

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo wi)
		{
			if (BlockerScanRadius < WDist.Zero)
				BlockerScanRadius = OpenRA.Mods.Common.Util.MinimumRequiredBlockerScanRadius(rules);
		}
	}

	public class ArcLaserZap : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly ArcLaserZapInfo info;
		readonly Animation hitanim;
		readonly Color color;
		int ticks = 0;
		bool doneDamage;
		bool animationComplete;
		[Sync] WPos target;
		[Sync] WPos source;

		public ArcLaserZap(ArcLaserZapInfo info, ProjectileArgs args, Color color)
		{
			this.args = args;
			this.info = info;
			this.color = color;
			target = args.PassiveTarget;
			source = args.Source;

			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = OpenRA.Mods.Common.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				var maxOffset = inaccuracy * (target - source).Length / args.Weapon.Range.Length;
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxOffset / 1024;
			}

			if (!string.IsNullOrEmpty(info.HitAnim))
				hitanim = new Animation(args.SourceActor.World, info.HitAnim);
		}

		public void Tick(World world)
		{
			// Beam tracks target
			if (info.TrackTarget && args.GuidedTarget.IsValidFor(args.SourceActor))
				target = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.PositionClosestTo(source);

			// Check for blocking actors
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, source, target,
				info.Width, info.BlockerScanRadius, out blockedPos))
			{
				target = blockedPos;
			}

			if (!doneDamage)
			{
				if (hitanim != null)
					hitanim.PlayThen(info.HitAnimSequence, () => animationComplete = true);
				else
					animationComplete = true;

				args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);
				doneDamage = true;
			}

			if (hitanim != null)
				hitanim.Tick();

			if (++ticks >= info.Duration && animationComplete)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(args.Source))
				yield break;

			if (ticks < info.Duration)
			{
				var rc = Color.FromArgb((info.Duration - ticks) * color.A / info.Duration, color);
				yield return new ArcRenderable(args.Source, target, info.ZOffset, info.Angle, rc, info.Width, info.QuantizedSegments);
			}

			if (hitanim != null)
				foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
