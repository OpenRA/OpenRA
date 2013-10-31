#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackBomberInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackBomber(init.self); }
	}

	class AttackBomber : AttackBase, ISync
	{
		[Sync] Target target;

		public AttackBomber(Actor self)
			: base(self) { }

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (!target.IsInRange(self.CenterPosition, GetMaximumRange()))
				return;

			var facing = self.TraitOrDefault<IFacing>();
			var cp = self.CenterPosition;
			var t = Target.FromPos(cp - new WVec(0, 0, cp.Z));
			foreach (var a in Armaments)
				a.CheckFire(self, this, facing, t);
		}

		public void SetTarget(WPos pos) { target = Target.FromPos(pos); }

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			// TODO: Player controlled units want this too!
			throw new NotImplementedException("CarpetBomb requires a scripted target");
		}
	}
}
