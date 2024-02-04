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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render the actor with a given color when cloaked.")]
	public class RenderCloakAsColorInfo : RenderCloakAsBaseInfo
	{
		[Desc("The color to use when cloaked when using Color CloakStyle.")]
		public readonly Color Color = Color.FromArgb(140, 0, 0, 0);

		public override object Create(ActorInitializer init) { return new RenderCloakAsColor(init, this); }
	}

	public class RenderCloakAsColor : RenderCloakAsBase<RenderCloakAsColorInfo>
	{
		readonly float3 color;
		readonly float colorAlpha;

		public RenderCloakAsColor(ActorInitializer init, RenderCloakAsColorInfo info)
			: base(info)
		{
			color = new float3(info.Color.R, info.Color.G, info.Color.B) / 255f;
			colorAlpha = info.Color.A / 255f;
		}

		protected override IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return r.Select(a => !a.IsDecoration && a is IModifyableRenderable mr ?
				mr.WithTint(color, mr.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(colorAlpha) :
				a);
		}
	}
}
