using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits
{
	public abstract class SupportPowerInfo : ITraitInfo
	{
		public readonly bool RequiresPower = true;
		public readonly bool OneShot = false;
		public readonly float ChargeTime = 0;
		public readonly string Image = null;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly string[] Prerequisites = { };
		public readonly int TechLevel = -1;
		public readonly bool GivenAuto = true;

		public abstract object Create(Actor self);
	}

	public class SupportPower : ITick
	{
		public readonly SupportPowerInfo Info;
		public int RemainingTime { get; private set; }
		public int TotalTime { get { return (int)(Info.ChargeTime * 60 * 25); } }
		public bool IsUsed;
		public bool IsAvailable;
		public bool IsReady { get { return RemainingTime == 0; } }
		public readonly Player Owner;

		bool notifiedCharging;
		bool notifiedReady;

		public SupportPower(Actor self, SupportPowerInfo info)
		{
			Info = info;
			RemainingTime = TotalTime;
			Owner = self.Owner;
		}

		public void Tick(Actor self)
		{
			if (Info.OneShot && IsUsed)
				return;

			var buildings = Rules.TechTree.GatherBuildings(self.Owner);
			var effectivePrereq = Info.Prerequisites
				.Select(a => a.ToLowerInvariant())
				.Where(a => Rules.Info[a].Traits.Get<BuildableInfo>().Owner.Contains(self.Owner.Race));
			
			if (Info.GivenAuto)
			{
				IsAvailable = Info.TechLevel > -1
					&& effectivePrereq.Any()
					&& effectivePrereq.All(a => buildings[a].Count > 0);
			}
			
			// Do we have enough powered prerequisites?
			// Hack in support for special powers without prereqs
			var isPowered = (Info.Prerequisites.Count() == 0) ? self.Owner.GetPowerState() == PowerState.Normal : effectivePrereq.Any() && effectivePrereq.All(a => buildings[a].Any(b => !b.traits.Get<Building>().Disabled));
			
			if (IsAvailable && (!Info.RequiresPower || isPowered))
			{
				if (RemainingTime > 0) --RemainingTime;
				if (!notifiedCharging)
				{
					OnBeginCharging();
					notifiedCharging = true;
				}
			}

			if (RemainingTime == 0
				&& !notifiedReady)
			{
				OnFinishCharging();
				notifiedReady = true;
			}
		}

		public void FinishActivate()
		{
			if (Info.OneShot)
			{
				IsUsed = true;
				IsAvailable = false;
			}
			RemainingTime = TotalTime;
			notifiedReady = false;
			notifiedCharging = false;
		}

		public void Give(float charge)
		{
			IsAvailable = true;
			IsUsed = false;
			RemainingTime = (int)(charge * TotalTime);
		}

		protected virtual void OnBeginCharging() { }
		protected virtual void OnFinishCharging() { }
		protected virtual void OnActivate() { }

		public void Activate() { OnActivate(); }	// todo: some more hax
	}
}
