#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderUnitReloadInfo : RenderUnitInfo, Requires<ArmamentInfo>, Requires<AttackBaseInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		public override object Create(ActorInitializer init) { return new RenderUnitReload(init.self, this); }
	}

	class RenderUnitReload : RenderUnit
	{
		readonly AttackBase attack;
		readonly Armament armament;

		public RenderUnitReload(Actor self, RenderUnitReloadInfo info)
			: base(self)
		{
			attack = self.Trait<AttackBase>();
			armament = self.TraitsImplementing<Armament>()
				.Single(a => a.Info.Name == info.Armament);
		}

		public override void Tick(Actor self)
		{
			var sequence = (armament.IsReloading ? "empty-" : "") + (attack.IsAttacking ? "aim" : "idle");
			if (sequence != DefaultAnimation.CurrentSequence.Name)
				DefaultAnimation.ReplaceAnim(sequence);

			base.Tick(self);
		}
	}
}
