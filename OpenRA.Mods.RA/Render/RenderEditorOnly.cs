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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderEditorOnlyInfo : RenderSimpleInfo
	{
		public override object Create(ActorInitializer init) { return new RenderEditorOnly(init.self); }
	}

	class RenderEditorOnly : RenderSimple
	{
		public RenderEditorOnly(Actor self) : base(self, () => 0) { }

		public override IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr) { return SpriteRenderable.None; }
	}
}
