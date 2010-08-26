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

namespace OpenRA.Traits
{
	public abstract class SupportPowerInfo : ITraitInfo, ITraitPrerequisite<TechTreeCacheInfo>
	{
		public readonly bool RequiresPower = true;
		public readonly bool OneShot = false;
		public readonly float ChargeTime = 0;
		public readonly string Image = null;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		[ActorReference]
		public readonly string[] Prerequisites = { };
		public readonly int TechLevel = -1;
		public readonly bool GivenAuto = true;

		public readonly string OrderName;
		
		public readonly string BeginChargeSound = null;
		public readonly string EndChargeSound = null;
		public readonly string SelectTargetSound = null;
		public readonly string LaunchSound = null;

		public abstract object Create(ActorInitializer init);

		public SupportPowerInfo() { OrderName = GetType().Name + "Order"; }
	}

	public class SupportPower : ITick, ITechTreeElement
	{
		public readonly SupportPowerInfo Info;
		public int RemainingTime { get; private set; }
		public int TotalTime { get { return (int)(Info.ChargeTime * 60 * 25); } }
		public bool IsUsed;
		public bool IsAvailable;
		public bool IsReady { get { return IsAvailable && RemainingTime == 0; } }

		protected readonly Actor Self;
		protected readonly Player Owner;

		bool notifiedCharging;
		bool notifiedReady;

		public SupportPower(Actor self, SupportPowerInfo info)
		{
			Info = info;
			RemainingTime = TotalTime;
			Self = self;
			Owner = self.Owner;

			self.Trait<TechTreeCache>().Add( Info.Prerequisites.Select( a => Rules.Info[ a.ToLowerInvariant() ] ).ToList(), this );
		}

		public void Tick(Actor self)
		{
			if (Info.OneShot && IsUsed)
				return;

			if (Info.GivenAuto)
				IsAvailable = Info.TechLevel > -1 && hasPrerequisites;
			
			if (IsAvailable && (!Info.RequiresPower || IsPowered()))
			{
				if (Game.LobbyInfo.GlobalSettings.AllowCheats && self.Trait<DeveloperMode>().FastCharge) RemainingTime = 0;
				if (RemainingTime > 0) --RemainingTime;
				if (!notifiedCharging)
				{
					Sound.PlayToPlayer(Owner, Info.BeginChargeSound);
					OnBeginCharging();
					notifiedCharging = true;
				}
			}

			if (RemainingTime == 0
				&& !notifiedReady)
			{
				Sound.PlayToPlayer(Owner, Info.EndChargeSound);
				OnFinishCharging();
				notifiedReady = true;
			}
		}

		bool IsPowered()
		{
			var buildings = Rules.TechTree.GatherBuildings(Owner);
			var effectivePrereq = Info.Prerequisites
				.Select(a => a.ToLowerInvariant())
				.Where(a => Rules.Info[a].Traits.Get<BuildableInfo>().Owner.Contains(Owner.Country.Race));

			if (Info.Prerequisites.Count() == 0) 
				return Owner.PlayerActor.Trait<PlayerResources>().GetPowerState() == PowerState.Normal;
			
			return effectivePrereq.Any() && 
				effectivePrereq.All(a => buildings[a].Any(b => !b.Trait<Building>().Disabled));
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

		public void Activate()
		{
			if (!IsAvailable || !IsReady)
				return;

			if (Info.RequiresPower && !IsPowered())
			{
				var eva = Owner.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
				Sound.Play(eva.AbilityInsufficientPower);
				return;
			}

			Sound.PlayToPlayer(Owner, Info.SelectTargetSound);
			OnActivate();
		}

		bool hasPrerequisites;

		public void Available()
		{
			hasPrerequisites = true;
		}

		public void Unavailable()
		{
			hasPrerequisites = false;
		}
	}
}
