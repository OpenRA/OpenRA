#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class RenderDisguiseInfo : RenderInfantryInfo, Requires<DisguiseInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderDisguise(init.Self, this); }
	}

	class RenderDisguise : RenderInfantry
	{
		RenderDisguiseInfo info;
		string intendedSprite;
		Disguise disguise;

		public RenderDisguise(Actor self, RenderDisguiseInfo info)
			: base(self, info)
		{
			this.info = info;
			disguise = self.Trait<Disguise>();
			intendedSprite = disguise.AsSprite;
		}

		public override void Tick(Actor self)
		{
			if (disguise.AsSprite != intendedSprite)
			{
				intendedSprite = disguise.AsSprite;
				DefaultAnimation.ChangeImage(intendedSprite ?? GetImage(self), info.StandAnimations.Random(Game.CosmeticRandom));
				UpdatePalette();
			}

			base.Tick(self);
		}
	}
}
