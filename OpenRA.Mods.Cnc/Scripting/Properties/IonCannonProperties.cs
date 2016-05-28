#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Scripting
{
	[ScriptPropertyGroup("Support Powers")]
	public class IonCannonProperties : ScriptActorProperties, Requires<IonCannonPowerInfo>
	{
		readonly Actor self;
		readonly IonCannonPower power;

		public IonCannonProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			this.self = self;
			power = self.Trait<IonCannonPower>();
		}

		[Desc("Activates the IonCannonPower of the actor, launching the ion cannon on the given 'targetLocation'.")]
		public void IonCannon(CPos targetLocation)
		{
			power.ActivateIonCannon(self, targetLocation);
		}
	}
}
