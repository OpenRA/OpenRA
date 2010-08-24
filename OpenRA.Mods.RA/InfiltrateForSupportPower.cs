#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class InfiltrateForSupportPowerInfo : ITraitInfo
	{
		public readonly string Power = null;
		public object Create(ActorInitializer init) { return new InfiltrateForSupportPower(this); }
	}
	
	class InfiltrateForSupportPower : IAcceptSpy
	{
		InfiltrateForSupportPowerInfo info;
		public InfiltrateForSupportPower(InfiltrateForSupportPowerInfo info)
		{
			this.info = info;
		}

		public void OnInfiltrate(Actor self, Actor spy)
		{
			var p = spy.Owner.PlayerActor.TraitsImplementing<SupportPower>()
				.FirstOrDefault(sp => sp.GetType().Name == info.Power);

			if (p != null) p.Give(1);
		}
	}
}
