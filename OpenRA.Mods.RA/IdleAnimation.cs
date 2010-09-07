#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class IdleAnimationInfo : ITraitInfo
	{
		public readonly int IdleWaitTicks = 50;
		public readonly string[] Animations = {};
		public object Create(ActorInitializer init) { return new IdleAnimation(this); }
	}

	// infantry prone behavior
	class IdleAnimation : INotifyDamage, INotifyIdle
	{
		IdleAnimationInfo Info;
		public IdleAnimation(IdleAnimationInfo info)
		{
			Info = info;
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.GetCurrentActivity() is IdleAnimation)
				self.CancelActivity();
		}

		public void Idle(Actor self)
		{
			self.QueueActivity(new Activities.IdleAnimation(Info.Animations.Random(Game.CosmeticRandom),
			                                                Info.IdleWaitTicks));
		}
	}
}
