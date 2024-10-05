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

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render the actor with alpha when cloaked.")]
	public class RenderCloakAsAlphaInfo : RenderCloakAsBaseInfo
	{
		[Desc("The alpha level to use when cloaked when using Alpha CloakStyle.")]
		public readonly float Alpha = 0.55f;

		public override object Create(ActorInitializer init) { return new RenderCloakAsAlpha(init, this); }
	}

	public class RenderCloakAsAlpha : RenderCloakAsBase<RenderCloakAsAlphaInfo>
	{
		public RenderCloakAsAlpha(ActorInitializer init, RenderCloakAsAlphaInfo info)
			: base(info) { }

		protected override IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return r.Select(a => !a.IsDecoration && a is IModifyableRenderable mr ? mr.WithAlpha(Info.Alpha) : a);
		}
	}
}
