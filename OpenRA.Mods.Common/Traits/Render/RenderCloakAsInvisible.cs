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
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render the actor as invisible when cloaked.")]
	public class RenderCloakAsInvisibleInfo : RenderCloakAsBaseInfo
	{
		public override object Create(ActorInitializer init) { return new RenderCloakAsInvisible(init, this); }
	}

	public class RenderCloakAsInvisible : RenderCloakAsBase<RenderCloakAsInvisibleInfo>
	{
		public RenderCloakAsInvisible(ActorInitializer init, RenderCloakAsInvisibleInfo info)
			: base(info) { }

		protected override IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return SpriteRenderable.None;
		}
	}
}
