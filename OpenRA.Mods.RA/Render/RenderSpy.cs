#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderSpyInfo : RenderInfantryInfo
	{
		public override object Create(ActorInitializer init) { return new RenderSpy(init.self); }
	}

	class RenderSpy : RenderInfantry, IRenderModifier
	{
		public RenderSpy(Actor self) : base(self) { }

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			if (self.Owner == self.World.LocalPlayer)
				return r;

			return r.Select(a => a.WithPalette(self.World.LocalPlayer.Palette));
		}

		public override void Tick(Actor self)
		{
			anim.ChangeImage(self.Owner == self.World.LocalPlayer ? GetImage(self) : "e1");
			base.Tick(self);
		}
	}
}
