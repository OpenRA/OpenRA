#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class RenderUnitReloadInfo : RenderUnitInfo, Requires<ArmamentInfo>, Requires<AttackBaseInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		public override object Create(ActorInitializer init) { return new RenderUnitReload(init, this); }
	}

	class RenderUnitReload : RenderUnit
	{
		readonly AttackBase attack;
		readonly Armament armament;

		public RenderUnitReload(ActorInitializer init, RenderUnitReloadInfo info)
			: base(init, info)
		{
			attack = init.Self.Trait<AttackBase>();
			armament = init.Self.TraitsImplementing<Armament>()
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
