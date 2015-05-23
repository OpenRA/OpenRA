﻿#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Actor has a visual turret used to attack.")]
	public class AttackTurretedInfo : AttackFollowInfo, Requires<TurretedInfo>
	{
		public override object Create(ActorInitializer init) { return new AttackTurreted(init.self, this); }
	}

	public class AttackTurreted : AttackFollow, ITick, ISync
	{
		protected IEnumerable<Turreted> turrets;

		public AttackTurreted(Actor self, AttackTurretedInfo info)
			: base(self, info)
		{
			turrets = self.TraitsImplementing<Turreted>();
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (!base.CanAttack(self, target))
				return false;

			var canAttack = false;
			foreach (var t in turrets)
				if (t.FaceTarget(self, target))
					canAttack = true;

			return canAttack;
		}
	}
}
