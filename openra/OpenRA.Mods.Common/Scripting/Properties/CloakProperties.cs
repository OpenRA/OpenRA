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
	[ScriptPropertyGroup("Cloak")]
	public class CloakProperties : ScriptActorProperties, Requires<CloakInfo>
	{
		readonly Cloak[] cloaks;

		public CloakProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			cloaks = self.TraitsImplementing<Cloak>().ToArray();
		}

		[Desc("Returns true if the actor is cloaked.")]
		public bool IsCloaked
		{
			get
			{
				return cloaks.Any(c => c.Cloaked);
			}
		}
	}
}
