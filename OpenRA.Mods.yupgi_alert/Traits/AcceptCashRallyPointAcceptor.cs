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

using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

/*
// Modifications for Mod.yupgi_alert:
// Add sound effect parameter and INotifyCashTransfer.
*/

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Accepts rally point for cash deliverers.")]
	public class AcceptCashRallyPointAcceptorInfo : TraitInfo<AcceptCashRallyPointAcceptor>
	{
		public AcceptCashRallyPointAcceptorInfo() { }
	}

	public class AcceptCashRallyPointAcceptor : IAcceptsRallyPoint
	{
		public AcceptCashRallyPointAcceptor() { }

		bool IAcceptsRallyPoint.IsAcceptableActor(Actor produced, Actor dest)
		{
			return produced.TraitOrDefault<DeliversCash>() != null;
		}

		Activity IAcceptsRallyPoint.RallyActivities(Actor produced, Actor dest)
		{
			var info = produced.Info.TraitInfo<DeliversCashInfo>();
			produced.SetTargetLine(Target.FromActor(dest), Color.Yellow);
			return new DonateCash(produced, dest, info.Payload, info.PlayerExperience);
		}
	}
}
