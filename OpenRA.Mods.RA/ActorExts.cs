#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public static class ActorExts
	{
		public static bool AppearsFriendlyTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[self.Owner];
			if (stance == Stance.Ally)
				return true;

			if (self.IsDisguised() && !toActor.HasTrait<IgnoresDisguise>())
				return toActor.Owner.Stances[self.EffectiveOwner.Owner] == Stance.Ally;

			return stance == Stance.Ally;
		}

		public static bool AppearsHostileTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.Stances[self.Owner];
			if (stance == Stance.Ally)
				return false;		/* otherwise, we'll hate friendly disguised spies */

			if (self.IsDisguised() && !toActor.HasTrait<IgnoresDisguise>())
				return toActor.Owner.Stances[self.EffectiveOwner.Owner] == Stance.Enemy;

			return stance == Stance.Enemy;
		}
	}
}