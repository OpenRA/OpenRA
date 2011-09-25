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
			var spy = a.TraitOrDefault<Spy>();
			return spy != null && spy.Disguised;
		}

		public static bool AppearsFriendlyTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[ self.Owner ];
			if (stance == Stance.Ally)
				return true;

			if (self.IsDisguisedSpy() && !toActor.HasTrait<IgnoresDisguise>())
				return toActor.Owner.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Ally;

			return stance == Stance.Ally;
		}

		public static bool AppearsHostileTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[ self.Owner ];
			if (stance == Stance.Ally)
				return false;		/* otherwise, we'll hate friendly disguised spies */

			if (self.IsDisguisedSpy() && !toActor.HasTrait<IgnoresDisguise>())
				return toActor.Owner.Stances[self.Trait<Spy>().disguisedAsPlayer] == Stance.Enemy;

			return stance == Stance.Enemy;
		}
	}
}

