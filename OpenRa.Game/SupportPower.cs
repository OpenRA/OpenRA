using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	class SupportPower
	{
		public readonly SupportPowerInfo Info;
		public readonly Player Owner;

		public SupportPower(SupportPowerInfo info, Player owner)
		{
			Info = info;
			Owner = owner;

			RemainingTime = TotalTime = (int)info.ChargeTime * 60 * 25;
		}

		public bool IsAvailable { get; private set; }
		public bool IsDone { get { return RemainingTime == 0; } }
		public int RemainingTime { get; private set; }
		public int TotalTime { get; private set; }

		public void Tick()
		{
			if (Info.GivenAuto)
			{
				var buildings = Rules.TechTree.GatherBuildings(Owner);
				var effectivePrereq = Info.Prerequisite
					.Select( a => a.ToLowerInvariant() )
					.Where( a => Rules.UnitInfo[a].Owner
						.Any( r => r == Owner.Race ));

				IsAvailable = Info.TechLevel > -1 
					&& effectivePrereq.Any()
					&& effectivePrereq.All(a => buildings[a].Count > 0);
			}

			if (IsAvailable && (!Info.Powered || Owner.GetPowerState() == PowerState.Normal))
			{
				if (RemainingTime > 0) --RemainingTime;
			}
		}

		public void Activate()
		{
		}
	}
}
