#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Guard")]
	public class GuardProperties : ScriptActorProperties, Requires<GuardInfo>, Requires<IMoveInfo>
	{
		Guard guard;
		public GuardProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			guard = self.Trait<Guard>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Guard the target actor.")]
		public void Guard(Actor targetActor)
		{
			if (targetActor.HasTrait<Guardable>())
				guard.GuardTarget(self, Target.FromActor(targetActor));
		}
	}
}