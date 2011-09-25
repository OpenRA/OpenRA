#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.RA.Orders;

namespace OpenRA.Mods.RA.Render
{
	class RenderSpyInfo : RenderInfantryProneInfo
	{
		public override object Create(ActorInitializer init) { return new RenderSpy(init.self, this); }
	}

	class RenderSpy : RenderInfantryProne, IRenderModifier
	{
		string disguisedAsSprite;
		Spy spy;

		public RenderSpy(Actor self, RenderSpyInfo info) : base(self, info)
		{
			spy = self.Trait<Spy>();
			disguisedAsSprite = spy.disguisedAsSprite;
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return spy.disguisedAsPlayer != null ? r.Select(a => a.WithPalette(Palette(spy.disguisedAsPlayer))) : r;
		}

		public override void Tick(Actor self)
		{
			if (spy.disguisedAsSprite != disguisedAsSprite)
			{
				disguisedAsSprite = spy.disguisedAsSprite;
				if (disguisedAsSprite != null)
					anim.ChangeImage(disguisedAsSprite, "stand");
				else
					anim.ChangeImage(GetImage(self), "stand");
			}
			base.Tick(self);
		}
	}
}
