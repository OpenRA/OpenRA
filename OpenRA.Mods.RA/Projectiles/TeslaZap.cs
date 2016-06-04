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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Projectiles
{
	public class TeslaZapInfo : IProjectileInfo
	{
		public readonly string Image = "litning";

		[SequenceReference("Image")] public readonly string BrightSequence = "bright";
		[SequenceReference("Image")] public readonly string DimSequence = "dim";

		[PaletteReference] public readonly string Palette = "effect";

		public readonly int BrightZaps = 1;
		public readonly int DimZaps = 2;

		public readonly int Duration = 2;

		public IProjectile Create(ProjectileArgs args) { return new TeslaZap(this, args); }
	}

	public class TeslaZap : IProjectile
	{
		readonly ProjectileArgs args;
		readonly TeslaZapInfo info;
		TeslaZapRenderable zap;
		int timeUntilRemove; // # of frames
		bool doneDamage = false;
		bool initialized = false;

		public TeslaZap(TeslaZapInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			this.timeUntilRemove = info.Duration;
		}

		public void Tick(World world)
		{
			if (timeUntilRemove-- <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			if (!doneDamage)
			{
				var pos = args.GuidedTarget.IsValidFor(args.SourceActor) ? args.GuidedTarget.CenterPosition : args.PassiveTarget;
				args.Weapon.Impact(Target.FromPos(pos), args.SourceActor, args.DamageModifiers);
				doneDamage = true;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!initialized)
			{
				var pos = args.GuidedTarget.IsValidFor(args.SourceActor) ? args.GuidedTarget.CenterPosition : args.PassiveTarget;
				zap = new TeslaZapRenderable(args.Source, 0, pos - args.Source,
					info.Image, info.BrightSequence, info.BrightZaps, info.DimSequence, info.DimZaps, info.Palette);
			}

			yield return zap;
		}
	}
}
