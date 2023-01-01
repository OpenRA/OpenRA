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
	[ScriptPropertyGroup("Support Powers")]
	public class NukeProperties : ScriptActorProperties, Requires<NukePowerInfo>
	{
		readonly NukePower np;

		public NukeProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			np = self.TraitsImplementing<NukePower>().First();
		}

		[Desc("Activate the actor's NukePower.")]
		public void ActivateNukePower(CPos target)
		{
			np.Activate(Self, Self.World.Map.CenterOfCell(target));
		}
	}
}
