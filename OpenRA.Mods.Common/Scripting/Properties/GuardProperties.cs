#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Combat")]
	public class GuardProperties : ScriptActorProperties, Requires<GuardInfo>, Requires<IMoveInfo>
	{
		readonly Guard guard;
		public GuardProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			guard = self.Trait<Guard>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Guard the target actor.")]
		public void Guard(Actor targetActor)
		{
			if (targetActor.Info.HasTraitInfo<GuardableInfo>())
				guard.GuardTarget(Self, Target.FromActor(targetActor));
		}
	}
}
