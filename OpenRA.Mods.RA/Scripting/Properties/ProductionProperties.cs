#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.RA.Activities;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Production")]
	public class ProductionProperties : ScriptActorProperties, Requires<ProductionInfo>
	{
		readonly Production p;

		public ProductionProperties(Actor self)
			: base(self)
		{
			p = self.Trait<Production>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Build a unit, ignoring the production queue. The activity will wait if the exit is blocked")]
		public void Produce(string actorType, string raceVariant = null)
		{
			ActorInfo actorInfo;
			if (!self.World.Map.Rules.Actors.TryGetValue(actorType, out actorInfo))
				throw new LuaException("Unknown actor type '{0}'".F(actorType));

			self.QueueActivity(new WaitFor(() => p.Produce(self, actorInfo, raceVariant)));
		}
	}
}