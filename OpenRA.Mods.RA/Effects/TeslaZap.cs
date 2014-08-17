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

namespace OpenRA.Mods.RA.Effects
{
	class TeslaZapInfo : IProjectileInfo
	{
		public readonly string Image = "litning";
		public readonly string Palette = "effect";
		public readonly int BrightZaps = 1;
		public readonly int DimZaps = 2;
		public IEffect Create(ProjectileArgs args) { return new TeslaZap( this, args ); }
	}

	class TeslaZap : IEffect
	{
		readonly ProjectileArgs Args;
		readonly TeslaZapInfo Info;
		TeslaZapRenderable zap;
		int timeUntilRemove = 2; // # of frames
		bool doneDamage = false;
		bool initialized = false;

		public TeslaZap(TeslaZapInfo info, ProjectileArgs args)
		{
			Args = args;
			Info = info;
		}

		public void Tick(World world)
		{
			if (timeUntilRemove-- <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			if (!doneDamage)
			{
				var pos = Args.GuidedTarget.IsValidFor(Args.SourceActor) ? Args.GuidedTarget.CenterPosition : Args.PassiveTarget;
				Args.Weapon.Impact(pos, Args.SourceActor, Args.DamageModifiers);
				doneDamage = true;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!initialized)
			{
				var pos = Args.GuidedTarget.IsValidFor(Args.SourceActor) ? Args.GuidedTarget.CenterPosition : Args.PassiveTarget;
				zap = new TeslaZapRenderable(Args.Source, 0, pos - Args.Source, Info.Image, Info.BrightZaps, Info.DimZaps, Info.Palette);
			}
			yield return zap;
		}
	}
}
