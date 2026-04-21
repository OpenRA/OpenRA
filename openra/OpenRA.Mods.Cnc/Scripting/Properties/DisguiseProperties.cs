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

using OpenRA.Mods.Cnc.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class DisguiseProperties : ScriptActorProperties, Requires<DisguiseInfo>
	{
		readonly Disguise disguise;

		public DisguiseProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			disguise = Self.Trait<Disguise>();
		}

		[Desc("Disguises as the target actor.")]
		public void DisguiseAs(Actor target)
		{
			disguise.DisguiseAs(target);
		}

		[Desc("Disguises as the target type with the specified owner.")]
		public void DisguiseAsType(string actorType, Player newOwner)
		{
			var actorInfo = Self.World.Map.Rules.Actors[actorType];
			disguise.DisguiseAs(actorInfo, newOwner);
		}
	}
}
