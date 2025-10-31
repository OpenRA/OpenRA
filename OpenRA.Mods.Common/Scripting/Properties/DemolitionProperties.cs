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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Combat")]
	public class DemolitionProperties : ScriptActorProperties, Requires<IMoveInfo>, Requires<DemolitionInfo>
	{
		readonly Demolition[] demolitions;

		public DemolitionProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			demolitions = Self.TraitsImplementing<Demolition>().ToArray();
		}

		[ScriptActorPropertyActivity]
		[Desc("Demolish the target actor.")]
		public void Demolish(Actor target)
		{
			// NB: Scripted actions get no visible targetlines.
			var demolition = demolitions.FirstEnabledConditionalTraitOrDefault();
			if (demolition != null)
				Self.QueueActivity(demolition.GetDemolishActivity(Self, Target.FromActor(target), null));
		}
	}
}
