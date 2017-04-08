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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be captured by a unit with ExternalCaptures: trait.")]
	public class ExternalCapturableInfo : ITraitInfo
	{
		[Desc("Type of actor (the ExternalCaptures: trait defines what Types it can capture).")]
		public readonly string Type = "building";
		public readonly bool AllowAllies = false;
		public readonly bool AllowNeutral = true;
		public readonly bool AllowEnemies = true;
		[Desc("Seconds it takes to change the owner.", "You might want to add a ConditionBar: trait, too.")]
		public readonly int CaptureCompleteTime = 15;

		[Desc("Whether to prevent autotargeting this actor while it is being captured by an ally.")]
		public readonly bool PreventsAutoTarget = true;

		[Desc("Condition to grant while being captured.")]
		[GrantedConditionReference]
		public readonly string Condition;

		[Desc("Properties to attach to condition: duration, remaining, and/or progress.")]
		public readonly ConditionProgressProperties ConditionProperties;

		[GrantedConditionReference]
		public IEnumerable<string> GrantedConditionProperties
		{
			get { return ConditionProgressState.EnumerateProperties(Condition, ConditionProperties); }
		}

		public bool CanBeTargetedBy(Actor captor, Player owner)
		{
			var c = captor.Info.TraitInfoOrDefault<ExternalCapturesInfo>();
			if (c == null)
				return false;

			var playerRelationship = owner.Stances[captor.Owner];
			if (playerRelationship == Stance.Ally && !AllowAllies)
				return false;

			if (playerRelationship == Stance.Enemy && !AllowEnemies)
				return false;

			if (playerRelationship == Stance.Neutral && !AllowNeutral)
				return false;

			if (!c.CaptureTypes.Contains(Type))
				return false;

			return true;
		}

		public object Create(ActorInitializer init) { return new ExternalCapturable(init.Self, this); }
	}

	public class ExternalCapturable : ITick, ISync, IPreventsAutoTarget, INotifyCreated
	{
		[Sync] public int CaptureProgressTime = 0;
		[Sync] Actor captor;
		public ExternalCapturableInfo Info;
		public bool CaptureInProgress { get { return captor != null; } }
		public Actor Captor { get { return captor; } }
		Actor self;
		ConditionManager conditionManager;
		ConditionWithProgressState progress;

		public ExternalCapturable(Actor self, ExternalCapturableInfo info)
		{
			this.self = self;
			Info = info;
			progress = new ConditionWithProgressState(Info.ConditionProperties);
		}

		void INotifyCreated.Created(Actor self)
		{
			if (Info.Condition != null)
				conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public void BeginCapture(Actor captor)
		{
			var building = self.TraitOrDefault<Building>();
			if (building != null)
				building.Lock();

			this.captor = captor;
			if (conditionManager == null)
				return;

			var duration = Info.CaptureCompleteTime * 25;
			progress.Init(self, conditionManager, Info.Condition, duration, duration - CaptureProgressTime);
		}

		public void EndCapture()
		{
			var building = self.TraitOrDefault<Building>();
			if (building != null)
				building.Unlock();

			captor = null;
			CaptureProgressTime = 0;
			if (conditionManager != null)
				progress.Revoke(self, conditionManager);
		}

		public void Tick(Actor self)
		{
			if (captor != null && (!captor.IsInWorld || captor.IsDead))
				EndCapture();

			var duration = Info.CaptureCompleteTime * 25;
			if (CaptureInProgress && CaptureProgressTime < duration)
			{
				CaptureProgressTime++;
				if (conditionManager != null)
					progress.Update(self, conditionManager, duration, duration - CaptureProgressTime);
			}
		}

		public bool PreventsAutoTarget(Actor self, Actor attacker)
		{
			return Info.PreventsAutoTarget && captor != null && attacker.AppearsFriendlyTo(captor);
		}
	}
}
