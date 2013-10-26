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
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Orders;

namespace OpenRA.Mods.RA.Render
{
	class RenderSpyInfo : RenderInfantryProneInfo
	{
		public override object Create(ActorInitializer init) { return new RenderSpy(init.self, this); }
	}

	class RenderSpy : RenderInfantryProne
	{
		RenderSpyInfo info;
		string disguisedAsSprite;
		Spy spy;

		public RenderSpy(Actor self, RenderSpyInfo info) : base(self, info)
		{
			this.info = info;
			spy = self.Trait<Spy>();
			disguisedAsSprite = spy.disguisedAsSprite;
		}

		protected override string PaletteName(Actor self)
		{
			var player = spy.disguisedAsPlayer != null ? spy.disguisedAsPlayer : self.Owner;
			return info.Palette ?? info.PlayerPalette + player.InternalName;
		}

		public override void Tick(Actor self)
		{
			if (spy.disguisedAsSprite != disguisedAsSprite)
			{
				disguisedAsSprite = spy.disguisedAsSprite;
				if (disguisedAsSprite != null)
					anim.ChangeImage(disguisedAsSprite, info.StandAnimations.Random(Game.CosmeticRandom));
				else
					anim.ChangeImage(GetImage(self), info.StandAnimations.Random(Game.CosmeticRandom));
				UpdatePalette();
			}
			base.Tick(self);
		}
	}
}
