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
	[Desc("Invisible during games.")]
	class RenderSpritesEditorOnlyInfo : RenderSpritesInfo
	{
		public override object Create(ActorInitializer init) { return new RenderSpritesEditorOnly(init, this); }
	}

	class RenderSpritesEditorOnly : RenderSprites
	{
		public RenderSpritesEditorOnly(ActorInitializer init, RenderSpritesEditorOnlyInfo info)
			: base(init, info) { }

		public override IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr) { return SpriteRenderable.None; }
	}
}
