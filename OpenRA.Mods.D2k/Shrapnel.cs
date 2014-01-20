#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Effects;
using OpenRA.GameRules;
using OpenRA.Effects;
using OpenRA.Graphics;
using System.Drawing;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.D2k
{
    class ShrapnelInfo : IProjectileInfo
    {
        public readonly WRange MaxRange = new WRange(5120);
        public readonly WRange MinRange = new WRange(1024);
        public readonly WRange MaxVelocity = new WRange(200);
        public readonly WRange MinVelocity = new WRange(100);
        public readonly WAngle MaxAngle = WAngle.FromDegrees(50);
        public readonly WAngle MinAngle = WAngle.FromDegrees(20);
        public readonly string Trail = null;
        public readonly int TrailDelay = 0;
        public readonly int TrailInterval = 0;
        public readonly string Image = null;

        public IEffect Create(ProjectileArgs args) { return new Shrapnel(this, args); }
    }

    //mostly stolen from bullet. I had a few problems with using bullet (c:
    class Shrapnel : IEffect
    {
        readonly public ShrapnelInfo info;
        readonly public ProjectileArgs args;

        Animation anim;

        [Sync] protected WPos pos, target;
        [Sync] protected int length, facing, ticks, smokeTicks;
        [Sync] public Actor SourceActor { get { return args.SourceActor; } }

        protected readonly WRange speed;
        protected readonly WAngle angle;

        public Shrapnel(ShrapnelInfo info, ProjectileArgs args)
        {

            this.info = info;
            this.args = args;
            this.pos = args.Source;
            Thirdparty.Random random = args.SourceActor.World.SharedRandom;
            this.speed = new WRange(random.Next(info.MinVelocity.Range, info.MaxVelocity.Range + 1));
            this.angle = WAngle.FromDegrees(random.Next(info.MinAngle.Angle, info.MaxAngle.Angle + 1));
            this.facing = random.Next(0, 512);

            {
                var x = random.Next(info.MinRange.Range, info.MaxRange.Range + 1);
                var y = random.Next(info.MinRange.Range, info.MaxRange.Range - x + 1);
                target = new WPos(x + args.Source.X, y + args.Source.Y, 0);
            }

            length = Math.Max((target - pos).Length / this.speed.Range, 1);

            if (info.Image != null)
            {
                anim = new Animation(info.Image, GetEffectiveFacing);
                anim.PlayRepeating("idle");
            }

            smokeTicks = info.TrailDelay;
        }

        int GetEffectiveFacing()
        {
            var at = (float)ticks / (length - 1);
            var attitude = this.angle.Tan() * (1 - 2 * at) / (4 * 1024);
            var u = (facing % 128) / 128f;
            var scale = 512 * u * (1 - u);

            return (int)(facing < 128
                ? facing - scale * attitude
                : facing + scale * attitude);
        }

        public void Tick(World world)
        {
            if (anim != null)
                anim.Tick();

            pos = WPos.LerpQuadratic(args.Source, target, angle, ticks, length);

            if (info.Trail != null && --smokeTicks < 0)
            {
                var delayedPos = WPos.LerpQuadratic(args.Source, target, angle, ticks - info.TrailDelay, length);
                world.AddFrameEndTask(w => w.Add(new Smoke(w, delayedPos, info.Trail)));
                smokeTicks = info.TrailInterval;
            }

            if (ticks++ >= length)
                Explode(world);
        }

        public IEnumerable<IRenderable> Render(WorldRenderer wr)
        {
            if (anim == null || ticks >= length)
                yield break;

            var cell = pos.ToCPos();
            if (!args.SourceActor.World.FogObscures(cell))
            {
                var shadowPos = pos - new WVec(0, 0, pos.Z);
                foreach (var r in anim.Render(shadowPos, wr.Palette("shadow")))
                    yield return r;

                var palette = wr.Palette(args.Weapon.Palette);
                foreach (var r in anim.Render(pos, palette))
                    yield return r;
            }
        }

        protected virtual void Explode(World world)
        {
            world.AddFrameEndTask(w => w.Remove(this));
            Combat.DoImpacts(pos, args.SourceActor, args.Weapon, args.FirepowerModifier);
        }
    }
}
