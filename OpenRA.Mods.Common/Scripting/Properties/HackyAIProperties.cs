#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.AI;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("HackyAI")]
	public class HackyAIProperties : ScriptActorProperties
	{
		readonly HackyAI hackyAI;

		public HackyAIProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			hackyAI = self.Owner.PlayerActor.TraitsImplementing<HackyAI>().Where(b => b.IsEnabled).FirstOrDefault();
		}

		[ScriptContext(ScriptContextType.AI)]
		[ScriptActorPropertyActivity]
		[Desc("Mark this actor as occupied by lua scripting and disable control from Hacky AI.")]
		public bool HackyAIOccupied
		{
			get
			{
				return hackyAI.IsLuaOccupied(Self);
			}

			set
			{
				hackyAI.SetLuaOccupied(Self, value);
			}
		}
	}
}