#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Combat")]
	public class DemolitionProperties : ScriptActorProperties, Requires<IMoveInfo>, Requires<DemolitionInfo>
	{
		readonly DemolitionInfo info;

		public DemolitionProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			info = Self.Info.TraitInfo<DemolitionInfo>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Demolish the target actor.")]
		public void Demolish(Actor target)
		{
			// NB: Scripted actions get no visible targetlines.
			Self.QueueActivity(new Demolish(Self, Target.FromActor(target), info.EnterBehaviour, info.DetonationDelay,
				info.Flashes, info.FlashesDelay, info.FlashInterval, info.DamageTypes, null));
		}
	}
}
