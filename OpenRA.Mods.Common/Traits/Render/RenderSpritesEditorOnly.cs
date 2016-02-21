#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Traits
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
