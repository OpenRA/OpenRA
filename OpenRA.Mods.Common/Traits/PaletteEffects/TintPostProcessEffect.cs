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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used for day/night effects.")]
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class TintPostProcessEffectInfo : TraitInfo, ILobbyCustomRulesIgnore
	{
		public readonly float Red = 1f;
		public readonly float Green = 1f;
		public readonly float Blue = 1f;
		public readonly float Ambient = 1f;

		public override object Create(ActorInitializer init) { return new TintPostProcessEffect(this); }
	}

	public class TintPostProcessEffect : RenderPostProcessPassBase
	{
		public float Red;
		public float Green;
		public float Blue;
		public float Ambient;

		public TintPostProcessEffect(TintPostProcessEffectInfo info)
			: base("tint", PostProcessPassType.AfterActors)
		{
			Red = info.Red;
			Green = info.Green;
			Blue = info.Blue;
			Ambient = info.Ambient;
		}

		protected override bool Enabled => true;
		protected override void PrepareRender(WorldRenderer wr, IShader shader)
		{
			shader.SetVec("Tint", Ambient * Red, Ambient * Green, Ambient * Blue);
		}
	}
}
