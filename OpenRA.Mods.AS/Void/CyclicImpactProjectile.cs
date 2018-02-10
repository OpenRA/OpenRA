using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Flags]
	public enum FireMode
	{
		Spread = 0,
		Line = 1,
		Focus = 2
	}

	[Desc("Detonates all warheads attached to it over it's lifespan x amount of times.")]
	public class CyclicImpactProjectileInfo : IProjectileInfo, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Warhead explosion offsets")]
		public readonly WVec[] Offsets = { new WVec(0, 1, 0) };

		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist[] Speed = { new WDist(17) };

		[Desc("Maximum inaccuracy offset.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("How many ticks will pass between explosions.")]
		public readonly int ExplosionInterval = 8;

		[FieldLoader.Require]
		[WeaponReference]
		[Desc("Weapon that's detonated every interval.")]
		public readonly string Weapon = null;

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("If it's true then weapon won't continue firing past the target.")]
		public readonly bool KillProjectilesWhenReachedTargetLocation = false;

		[Desc("Where shall the bullets fly after instantiating? Possible values are Spread, Line and Focus")]
		public readonly FireMode FireMode = FireMode.Spread;

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Image to display.")]
		public readonly string Image = null;

		[Desc("Loop a randomly chosen sequence of Image from this list while this projectile is moving.")]
		[SequenceReference("Image")]
		public readonly string[] Sequences = { "idle" };

		[Desc("The palette used to draw this projectile.")]
		[PaletteReference]
		public readonly string Palette = "effect";

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Palette to use for this projectile's shadow if Shadow is true.")]
		[PaletteReference]
		public readonly string ShadowPalette = "shadow";

		[Desc("Trail animation.")]
		public readonly string TrailImage = null;

		[Desc("Loop a randomly chosen sequence of TrailImage from this list while this projectile is moving.")]
		[SequenceReference("TrailImage")]
		public readonly string[] TrailSequences = { "idle" };

		[Desc("Delay in ticks until trail animation is spawned.")]
		public readonly int TrailDelay = 1;

		[Desc("Palette used to render the trail sequence.")]
		[PaletteReference("TrailUsePlayerPalette")]
		public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

		public readonly int ContrailLength = 0;
		public readonly int ContrailZOffset = 2047;
		public readonly Color ContrailColor = Color.White;
		public readonly bool ContrailUsePlayerColor = false;
		public readonly int ContrailDelay = 1;
		public readonly WDist ContrailWidth = new WDist(64);

		[Desc("Is this blocked by actors with BlocksProjectiles trait.")]
		public readonly bool Blockable = true;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist Width = new WDist(1);

		[Desc("If projectile touches an actor with one of these stances during or after the first bounce, trigger explosion.")]
		public readonly Stance ValidBounceBlockerStances = Stance.Enemy | Stance.Neutral | Stance.Ally;

		[Desc("Scan radius for actors with projectile-blocking trait. If set to a negative value (default), it will automatically scale",
			"to the blocker with the largest health shape. Only set custom values if you know what you're doing.")]
		public WDist BlockerScanRadius = new WDist(-1);

		[Desc("Extra search radius beyond path for actors with ValidBounceBlockerStances. If set to a negative value (default), ",
			"it will automatically scale to the largest health shape. Only set custom values if you know what you're doing.")]
		public WDist BounceBlockerScanRadius = new WDist(-1);

		public IProjectile Create(ProjectileArgs args) { return new CyclicImpactProjectile(this, args); }

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			WeaponInfo weapon;
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
			WeaponInfo = weapon;

			if (BlockerScanRadius < WDist.Zero)
				BlockerScanRadius = Util.MinimumRequiredBlockerScanRadius(rules);

			if (BounceBlockerScanRadius < WDist.Zero)
				BounceBlockerScanRadius = Util.MinimumRequiredVictimScanRadius(rules);
		}
	}

	public class CyclicImpactProjectile : IProjectile, ISync
	{
		readonly CyclicImpactProjectileInfo info;
		readonly ProjectileArgs args;
		[Sync]
		readonly WDist speed;

		[Sync]
		WPos projectilepos, targetpos, sourcepos;
		WPos offsetSourcePos = WPos.Zero, offsetTargetPos = WPos.Zero;
		int lifespan;
		int ticks;
		int mindelay;
		WRot offsetRotation;
		CyclicImpactProjectileEffect[] projectiles; // offset projectiles

		public Actor SourceActor { get { return args.SourceActor; } }

		public CyclicImpactProjectile(CyclicImpactProjectileInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;

			projectilepos = args.Source;
			sourcepos = args.Source;

			var firedBy = args.SourceActor;

			var world = args.SourceActor.World;

			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			if (!info.KillProjectilesWhenReachedTargetLocation)
				targetpos = GetTargetPos();

			mindelay = args.Weapon.MinRange.Length / speed.Length;

			projectiles = new CyclicImpactProjectileEffect[info.Offsets.Count()];
			var range = Util.ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers);
			var mainfacing = (targetpos - sourcepos).Yaw.Facing;

			// target that will be assigned
			Target target = Target.Invalid;

			// main bullet facing
			int facing = 0;

			for (int i = 0; i < info.Offsets.Count(); i++)
			{
				switch (info.FireMode)
				{
					case FireMode.Focus:
						offsetRotation = WRot.FromFacing(mainfacing);
						offsetTargetPos = info.KillProjectilesWhenReachedTargetLocation
							? args.PassiveTarget
							: sourcepos + new WVec(range, 0, 0).Rotate(offsetRotation);
						offsetSourcePos = sourcepos + info.Offsets[i].Rotate(offsetRotation);
						break;
					case FireMode.Line:
						offsetRotation = WRot.FromFacing(mainfacing);
						offsetTargetPos = info.KillProjectilesWhenReachedTargetLocation
							? args.PassiveTarget
							: sourcepos + new WVec(range + info.Offsets[i].X, info.Offsets[i].Y, info.Offsets[i].Z).Rotate(offsetRotation);
						offsetSourcePos = sourcepos + info.Offsets[i].Rotate(offsetRotation);
						break;
					case FireMode.Spread:
						offsetRotation = WRot.FromFacing(info.Offsets[i].Yaw.Facing - 64) + WRot.FromFacing(mainfacing);
						offsetSourcePos = info.KillProjectilesWhenReachedTargetLocation
							? args.PassiveTarget
							: sourcepos + info.Offsets[i].Rotate(offsetRotation);
						offsetTargetPos = sourcepos + new WVec(range + info.Offsets[i].X, info.Offsets[i].Y, info.Offsets[i].Z).Rotate(offsetRotation);
						break;
				}

				if (info.Inaccuracy.Length > 0)
				{
					var inaccuracy = Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
					var maxOffset = inaccuracy * (args.PassiveTarget - projectilepos).Length / range;
					var inaccoffset = WVec.FromPDF(world.SharedRandom, 2) * maxOffset / 1024;
					offsetTargetPos += inaccoffset;
				}

				target = Target.FromPos(offsetTargetPos);

				// if it's true then lifespan is counted from source pos to target, instead of max range
				lifespan = info.KillProjectilesWhenReachedTargetLocation
					? Math.Max((offsetTargetPos - offsetSourcePos).Length / speed.Length, 1)
					: Math.Max(args.Weapon.Range.Length / speed.Length, 1);

				facing = (offsetTargetPos - offsetSourcePos).Yaw.Facing;
				var pargs = new ProjectileArgs
				{
					Weapon = args.Weapon,
					DamageModifiers = args.DamageModifiers,
					Facing = facing,
					Source = offsetSourcePos,
					SourceActor = firedBy,
					PassiveTarget = target.CenterPosition
				};

				projectiles[i] = new CyclicImpactProjectileEffect(info, pargs, lifespan);
				world.Add(projectiles[i]);
			}
		}

		WPos GetTargetPos() // gets where main missile should fly to
		{
			var targetpos = args.PassiveTarget;
			var actorpos = args.SourceActor.CenterPosition;
			var vector = targetpos - actorpos;

			var scaler = (args.Weapon.Range.Length * 1000) / vector.Length;

			var x = ((vector.X * scaler) / 1000) + actorpos.X;
			var y = ((vector.Y * scaler) / 1000) + actorpos.Y;
			var z = ((vector.Z * scaler) / 1000) + actorpos.Z;

			return new WPos(x, y, z);
		}

		public void Tick(World world)
		{
			if (ticks % info.ExplosionInterval == 0 && mindelay <= ticks)
				DoImpact();
			ticks++;

			if (ticks >= lifespan)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		void DoImpact()
		{
			foreach (CyclicImpactProjectileEffect p in projectiles)
				info.WeaponInfo.Impact(Target.FromPos(p.Position), SourceActor, args.DamageModifiers);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
