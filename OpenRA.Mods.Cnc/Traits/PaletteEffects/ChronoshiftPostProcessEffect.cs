#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Apply palette full screen rotations during chronoshifts. Add this to the world actor.")]
	public class ChronoshiftPostProcessEffectInfo : TraitInfo
	{
		[Desc("Measured in ticks.")]
		public readonly int ChronoEffectLength = 60;

		public override object Create(ActorInitializer init) { return new ChronoshiftPostProcessEffect(this); }
	}

	public class ChronoshiftPostProcessEffect : RenderPostProcessPassBase, ITick
	{
		readonly ChronoshiftPostProcessEffectInfo info;
		int remainingFrames;

		public ChronoshiftPostProcessEffect(ChronoshiftPostProcessEffectInfo info)
			: base("chronoshift", PostProcessPassType.AfterWorld)
		{
			this.info = info;
		}

		public void Enable()
		{
			remainingFrames = info.ChronoEffectLength;
		}

		void ITick.Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		protected override bool Enabled => remainingFrames > 0;
		protected override void PrepareRender(WorldRenderer wr, IShader shader)
		{
			shader.SetVec("Blend", (float)remainingFrames / info.ChronoEffectLength);
		}
	}
}
