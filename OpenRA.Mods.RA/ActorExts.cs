#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public static class ActorExts
	{
		static bool IsDisguisedSpy( this Actor a )
		{
			return a.HasTrait<Spy>() && a.Trait<Spy>().Disguised;
		}
		
		public static bool AppearsFriendlyTo(this Actor self, Actor toActor) 
		{
			var stance = toActor.Owner.Stances[ self.Owner ];
			
			if (self.IsDisguisedSpy() && !toActor.HasTrait<IgnoresDisguise>())
				if ( toActor.Owner.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Ally)
					return true;

			return stance == Stance.Ally;
		}
		
		public static bool AppearsHostileTo(this Actor self, Actor toActor) 
		{ 
			var stance = toActor.Owner.Stances[ self.Owner ];
			if (stance == Stance.Ally)
				return false;		/* otherwise, we'll hate friendly disguised spies */
			
			if (self.IsDisguisedSpy() && !toActor.HasTrait<IgnoresDisguise>())
				if (toActor.Owner.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Enemy)
					return true;

			return stance == Stance.Enemy;
		}
	}
}

