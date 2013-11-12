#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA.Render
{
	class RenderUnitReloadInfo : RenderUnitInfo
	{
		public override object Create(ActorInitializer init) { return new RenderUnitReload(init.self); }
	}

	class RenderUnitReload : RenderUnit
	{
		public RenderUnitReload(Actor self)
			: base(self) { }

		public override void Tick(Actor self)
		{
			var attack = self.TraitOrDefault<AttackBase>();

			if (attack != null)
				anim.ReplaceAnim((attack.IsReloading() ? "empty-" : "")
					+ (attack.IsAttacking ? "aim" : "idle"));
			base.Tick(self);
		}
	}
}
