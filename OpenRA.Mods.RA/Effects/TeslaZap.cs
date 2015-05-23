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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class TeslaZapInfo : IProjectileInfo
	{
		public readonly string Image = "litning";
		public readonly string Palette = "effect";
		public readonly int BrightZaps = 1;
		public readonly int DimZaps = 2;
		public IEffect Create(ProjectileArgs args) { return new TeslaZap(this, args); }
	}

	class TeslaZap : IEffect
	{
		readonly ProjectileArgs args;
		readonly TeslaZapInfo info;
		TeslaZapRenderable zap;
		int timeUntilRemove = 2; // # of frames
		bool doneDamage = false;
		bool initialized = false;

		public TeslaZap(TeslaZapInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
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
				zap = new TeslaZapRenderable(args.Source, 0, pos - args.Source, info.Image, info.BrightZaps, info.DimZaps, info.Palette);
			}

			yield return zap;
		}
	}
}
