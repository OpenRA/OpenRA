#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor has a visual turret used to attack.")]
	public class AttackTurretedInfo : AttackFollowInfo, Requires<TurretedInfo>
	{
		public override object Create(ActorInitializer init) { return new AttackTurreted(init.Self, this); }
	}

	public class AttackTurreted : AttackFollow
	{
		protected Turreted[] turrets;

		public AttackTurreted(Actor self, AttackTurretedInfo info)
			: base(self, info)
		{
			turrets = self.TraitsImplementing<Turreted>().ToArray();
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (target.Type == TargetType.Invalid)
				return false;

			// Don't break early from this loop - we want to bring all turrets to bear!
			var turretReady = false;
			foreach (var t in turrets)
				if (t.FaceTarget(self, target))
					turretReady = true;

			return turretReady && base.CanAttack(self, target);
		}
	}
}
