#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Combat")]
	public class DemolitionProperties : ScriptActorProperties, Requires<IMoveInfo>, Requires<C4DemolitionInfo>
	{
		readonly C4DemolitionInfo info;

		public DemolitionProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			info = Self.Info.TraitInfo<C4DemolitionInfo>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Demolish the target actor.")]
		public void Demolish(Actor target)
		{
			Self.QueueActivity(new Demolish(Self, target, info.EnterBehaviour, info.C4Delay,
				info.Flashes, info.FlashesDelay, info.FlashInterval, info.FlashDuration));
		}
	}
}
