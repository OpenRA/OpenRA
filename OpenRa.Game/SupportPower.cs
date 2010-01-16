using System;
using System.Linq;
using OpenRa.GameRules;
using OpenRa.SupportPowers;
using OpenRa.Traits;

namespace OpenRa
{
	// todo: fix this to route Activate through the orders system (otherwise desync in netplay)

	public class SupportPower
	{
		public readonly SupportPowerInfo Info;
		public readonly Player Owner;
		public readonly ISupportPowerImpl Impl;
		public readonly string Name;

		static ISupportPowerImpl ConstructPowerImpl(string implName)
		{
			var type = typeof(ISupportPowerImpl).Assembly.GetType(
				typeof(ISupportPowerImpl).Namespace + "." + implName, true, false);
			var ctor = type.GetConstructor(Type.EmptyTypes);
			return (ISupportPowerImpl)ctor.Invoke(new object[] { });
		}

		public SupportPower(string name, SupportPowerInfo info, Player owner)
		{
			Name = name;
			Info = info;
			Owner = owner;
			RemainingTime = TotalTime = (int)(info.ChargeTime * 60 * 25);
			Impl = ConstructPowerImpl(info.Impl);
		}

		public bool IsUsed;
		public bool IsAvailable { get; private set; }
		public bool IsDone { get { return RemainingTime == 0; } }
		public int RemainingTime { get; private set; }
		public int TotalTime { get; private set; }
		bool notifiedReady = false;
		bool notifiedCharging = false;
		
		public void Tick()
		{
			if (Info.OneShot && IsUsed)
				return;

			if (Info.GivenAuto)
			{
				var buildings = Rules.TechTree.GatherBuildings(Owner);
				var effectivePrereq = Info.Prerequisite
					.Select( a => a.ToLowerInvariant() )
					.Where( a => Rules.NewUnitInfo[a].Traits.Get<BuildableInfo>().Owner.Contains( Owner.Race ));

				IsAvailable = Info.TechLevel > -1 
					&& effectivePrereq.Any()
					&& effectivePrereq.All(a => buildings[a].Count > 0);
			}

			if (IsAvailable && (!Info.Powered || Owner.GetPowerState() == PowerState.Normal))
			{
				if (RemainingTime > 0) --RemainingTime;
				if (!notifiedCharging)
				{
					Impl.IsChargingNotification(this);
					notifiedCharging = true;
				}
			}

			if (RemainingTime == 0
				&& Impl != null 
				&& !notifiedReady)
			{
				Impl.IsReadyNotification(this);
				notifiedReady = true;	
			}
		}

		public void Activate()
		{
			if (Impl != null)
				Impl.Activate(this);
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

		public void Give(bool requireCharge)		// called by crate/spy/etc code
		{
			IsAvailable = true;
			IsUsed = false;
			RemainingTime = requireCharge ? TotalTime : 0;
		}
	}
}
