#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class AttackLeapInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackLeap(init.self); }
	}

	class AttackLeap : AttackBase
	{
		internal bool IsLeaping;

		public AttackLeap(Actor self)
			: base(self) {}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (!target.IsValid) return;
			if (IsLeaping) return;

			var weapon = self.Trait<AttackBase>().Weapons[0].Info;
			if( !Combat.IsInRange( self.CenterLocation, weapon.Range, target ) ) return;

			self.CancelActivity();
			self.QueueActivity(new Leap(self, target));
		}
	}
}
