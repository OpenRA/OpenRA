#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class IdleAnimationInfo : ITraitInfo, ITraitPrerequisite<RenderInfantryInfo>
	{
		public readonly int IdleWaitTicks = 50;
		public readonly string[] Animations = {};
		public object Create(ActorInitializer init) { return new IdleAnimation(this); }
	}

	class IdleAnimation : ITick, INotifyIdle
	{
		enum IdleState
		{
			None,
			Waiting,
			Active
		};
		
		IdleAnimationInfo Info;
		string sequence;
		int delay;
		IdleState state;
		
		public IdleAnimation(IdleAnimationInfo info)
		{
			Info = info;
		}

		public void Tick(Actor self)
		{		
			if (!self.IsIdle)
			{
				state = IdleState.None;
				return;
			}
			
			if (state == IdleState.Active)
				return;
				
			else if (delay > 0 && --delay == 0)
			{
				state = IdleState.Active;
				var ri = self.TraitOrDefault<RenderInfantry>();

				if (ri.anim.HasSequence(sequence))
					ri.anim.PlayThen(sequence, () => state = IdleState.None);
				else
					state = IdleState.None;
			}
		}
		
		public void TickIdle(Actor self)
		{
			if (state != IdleState.None)
				return;
			
			state = IdleState.Waiting;
			sequence = Info.Animations.Random(self.World.SharedRandom);
			delay = Info.IdleWaitTicks;
		}
	}
}
