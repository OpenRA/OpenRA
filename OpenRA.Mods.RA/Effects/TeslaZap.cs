#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class TeslaZapInfo : IProjectileInfo
	{
		public readonly string Image = "litning";
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
				var pos = Args.guidedTarget.IsValidFor(Args.sourceActor) ? Args.guidedTarget.CenterPosition : Args.passiveTarget;
				Combat.DoImpacts(pos, Args.sourceActor, Args.weapon, Args.firepowerModifier);
				doneDamage = true;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!initialized)
			{
				var pos = Args.guidedTarget.IsValidFor(Args.sourceActor) ? Args.guidedTarget.CenterPosition : Args.passiveTarget;
				zap = new TeslaZapRenderable(Args.source, 0, pos - Args.source, Info.Image, Info.BrightZaps, Info.DimZaps);
			}
			yield return zap;
		}
	}
}
