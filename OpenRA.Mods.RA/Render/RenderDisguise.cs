#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA.Render
{
	class RenderDisguiseInfo : RenderInfantryProneInfo
	{
		public override object Create(ActorInitializer init) { return new RenderDisguise(init.self, this); }
	}

	class RenderDisguise : RenderInfantryProne
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

		protected override string PaletteName(Actor self)
		{
			var player = disguise.AsPlayer ?? self.Owner;
			return info.Palette ?? info.PlayerPalette + player.InternalName;
		}

		public override void Tick(Actor self)
		{
			if (disguise.AsSprite != intendedSprite)
			{
				intendedSprite = disguise.AsSprite;
				anim.ChangeImage(intendedSprite ?? GetImage(self), info.StandAnimations.Random(Game.CosmeticRandom));
				UpdatePalette();
			}

			base.Tick(self);
		}
	}
}
