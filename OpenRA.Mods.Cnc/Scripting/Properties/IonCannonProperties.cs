#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * Information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.CnC.Scripting
{
	[ScriptPropertyGroup("Support Powers")]
	public class IonCannonProperties : ScriptActorProperties, Requires<IonCannonPowerInfo>
	{
		readonly IonCannonPower icp;

		public IonCannonProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			icp = self.TraitsImplementing<IonCannonPower>().First();
		}

		[Desc("Activate the actor's IonCannonPower.")]
		public void ActivateIonCannon(CPos target)
		{
			icp.Activate(Self, target);
		}
	}
}
