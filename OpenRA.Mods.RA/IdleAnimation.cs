#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA
{
	class IdleAnimationInfo : ITraitInfo
	{
		public readonly int IdleWaitTicks = 50;
		public readonly string[] Animations = {};
		public object Create(ActorInitializer init) { return new IdleAnimation(this); }
	}

	// infantry prone behavior
	class IdleAnimation : ITick, INotifyIdle
	{
		IdleAnimationInfo Info;
		string sequence;
		int delay;
		bool active, waiting;

		public IdleAnimation(IdleAnimationInfo info)
		{
			Info = info;
		}

		public void Tick(Actor self)
		{		
			if (active)
			{
				//System.Console.WriteLine("active: {0} ({1})", self.Info.Name, self.ActorID);
				return;
			}
			if (!self.IsIdle)
			{
				//System.Console.WriteLine("notidle: {0} ({1}) ",self.Info.Name, self.ActorID);
				active = false;
				waiting = false;
				return;
			}
			
			if (delay > 0 && --delay == 0)
			{
				System.Console.WriteLine("activate: {0} ({1})",self.Info.Name, self.ActorID);
				active = true;
				waiting = false;
				self.Trait<RenderInfantry>().anim.PlayThen(sequence, () =>
				{
					//System.Console.WriteLine("finished: {0} ({1}) ",self.Info.Name, self.ActorID);
					active = false;
				});
			}
		}
		
		public void TickIdle(Actor self)
		{
			//if (active)
			//	System.Console.WriteLine("idleactive: {0} ({1}) ",self.Info.Name, self.ActorID);
			
			//if (waiting)
			//	System.Console.WriteLine("idlewaiting: {0} ({1}) ",self.Info.Name, self.ActorID);
			if (active || waiting)
				return;
			
			waiting = true;
			sequence = Info.Animations.Random(self.World.SharedRandom);
			delay = Info.IdleWaitTicks;
			//System.Console.WriteLine("TickIdle: {2} ({3}) set {0} {1}",sequence,delay, self.Info.Name, self.ActorID);

		}
	}
}
