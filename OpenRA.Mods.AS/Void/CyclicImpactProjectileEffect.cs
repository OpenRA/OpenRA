using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
namespace OpenRA.Mods.Common.Effects
{
	public class CyclicImpactProjectileEffect : IEffect
	{
		readonly CyclicImpactProjectileInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;

		ContrailRenderable contrail;
		string trailPalette;

		[Sync]
		WPos projectilepos, targetpos, source;
		int lifespan;
		[Sync]
		int facing;
		int ticks, smokeTicks;
		World world;
		public WPos Position { get { return projectilepos; } }

		public CyclicImpactProjectileEffect(CyclicImpactProjectileInfo info, ProjectileArgs args, int lifespan)
		{
			this.info = info;
			this.args = args;
			this.lifespan = lifespan;
			projectilepos = args.Source;
			source = args.Source;

			world = args.SourceActor.World;
			targetpos = args.PassiveTarget;
			facing = args.Facing;

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, new Func<int>(GetEffectiveFacing));
				anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
			}

			if (info.ContrailLength > 0)
			{
				var color = info.ContrailUsePlayerColor ? ContrailRenderable.ChooseColor(args.SourceActor) : info.ContrailColor;
				contrail = new ContrailRenderable(world, color, info.ContrailWidth, info.ContrailLength, info.ContrailDelay, info.ContrailZOffset);
			}

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;

			smokeTicks = info.TrailDelay;
		}

		int GetEffectiveFacing()
		{
			var at = (float)ticks / (lifespan - 1);
			var attitude = WAngle.Zero.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = (facing % 128) / 128f;
			var scale = 512 * u * (1 - u);

			return (int)(facing < 128
				? facing - scale * attitude
				: facing + scale * attitude);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return contrail;

			if (anim == null || ticks >= lifespan)
				yield break;

			if (!world.FogObscures(projectilepos))
			{
				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(projectilepos);
					var shadowPos = projectilepos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, wr.Palette(info.ShadowPalette)))
						yield return r;
				}

				var palette = wr.Palette(info.Palette);
				foreach (var r in anim.Render(projectilepos, palette))
					yield return r;
			}
		}

		public void Tick(World world)
		{
			ticks++;
			if (anim != null)
				anim.Tick();

			var lastPos = projectilepos;
			projectilepos = WPos.LerpQuadratic(source, targetpos, WAngle.Zero, ticks, lifespan);

			// Check for walls or other blocking obstacles
			var shouldExplode = false;
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, lastPos, projectilepos, info.Width,
				info.BlockerScanRadius, out blockedPos))
			{
				projectilepos = blockedPos;
				shouldExplode = true;
			}

			if (!string.IsNullOrEmpty(info.TrailImage) && --smokeTicks < 0)
			{
				var delayedPos = WPos.LerpQuadratic(source, targetpos, WAngle.Zero, ticks - info.TrailDelay, lifespan);
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(delayedPos, w, info.TrailImage, info.TrailSequences.Random(world.SharedRandom),
					trailPalette, false, false, GetEffectiveFacing())));

				smokeTicks = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(projectilepos);

			var flightLengthReached = ticks >= lifespan;

			if (flightLengthReached)
				shouldExplode = true;

			// Driving into cell with higher height level
			shouldExplode |= world.Map.DistanceAboveTerrain(projectilepos).Length < 0;

			if (shouldExplode)
				Explode(world);
		}

		void Explode(World world)
		{
			args.Weapon.Impact(Target.FromPos(projectilepos), args.SourceActor, args.DamageModifiers);

			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(projectilepos, contrail)));

			world.AddFrameEndTask(w => w.Remove(this));
		}
	}
}
