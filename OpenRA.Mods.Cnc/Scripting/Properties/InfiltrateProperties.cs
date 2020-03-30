#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using Eluant;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class InfiltrateProperties : ScriptActorProperties, Requires<InfiltratesInfo>
	{
		readonly Infiltrates[] infiltratesTraits;

		public InfiltrateProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			infiltratesTraits = Self.TraitsImplementing<Infiltrates>().ToArray();
		}

		[Desc("Infiltrate the target actor.")]
		public void Infiltrate(Actor target)
		{
			var infiltrates = infiltratesTraits.FirstOrDefault(x => !x.IsTraitDisabled && x.Info.Types.Overlaps(target.GetEnabledTargetTypes()));

			if (infiltrates == null)
				throw new LuaException("{0} tried to infiltrate invalid target {1}!".F(Self, target));

			Self.QueueActivity(new Infiltrate(Self, Target.FromActor(target), infiltrates));
		}
	}
}
