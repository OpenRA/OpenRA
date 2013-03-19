﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackOmniInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackOmni(init.self); }
	}

	class AttackOmni : AttackBase, INotifyBuildComplete, ISync
	{
		[Sync] bool buildComplete = false;
		public void BuildingComplete(Actor self) { buildComplete = true; }

		public AttackOmni(Actor self) : base(self) { }

		protected override bool CanAttack( Actor self, Target target )
		{
			var isBuilding = ( self.HasTrait<Building>() && !buildComplete );
			return base.CanAttack( self, target ) && !isBuilding;
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new SetTarget( newTarget );
		}

		class SetTarget : Activity
		{
			readonly Target target;
			public SetTarget( Target target ) { this.target = target; }

			public override Activity Tick( Actor self )
			{
				if( IsCanceled || !target.IsValid )
					return NextActivity;

				self.Trait<AttackOmni>().DoAttack(self, target);
				return this;
			}
		}
	}
}
