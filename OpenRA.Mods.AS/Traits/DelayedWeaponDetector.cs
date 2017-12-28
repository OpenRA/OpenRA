#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This trait can reveal DelayedWeapon progressbars on DelayedWeaponAttachable traits.")]
	public class DelayedWeaponDetectorInfo : ConditionalTraitInfo
	{
		[Desc("Type of DelayedWeapons that can be detected.")]
		public readonly HashSet<string> Types = new HashSet<string> { "bomb" };

		[Desc("Range of detection.")]
		public readonly WDist Range = WDist.FromCells(1);

		public override object Create(ActorInitializer init) { return new DelayedWeaponDetector(init.Self, this); }
	}

	public class DelayedWeaponDetector : ConditionalTrait<DelayedWeaponDetectorInfo>, ITick, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		private WPos cachedPosition;
		private WDist cachedRange;
		private WDist desiredRange;
		private WDist cachedVRange = new WDist(1536);

		private int proximityTrigger;
		bool cachedDisabled = true;
		private Actor self;

		public DelayedWeaponDetector(Actor self, DelayedWeaponDetectorInfo info) : base(info)
		{
			this.self = self;
			cachedRange = info.Range;
		}

		public void Tick(Actor self)
		{
			var disabled = IsTraitDisabled;

			if (cachedDisabled != disabled)
			{
				desiredRange = disabled ? WDist.Zero : Info.Range;
				cachedDisabled = disabled;
			}

			if (self.CenterPosition != cachedPosition || desiredRange != cachedRange)
			{
				cachedPosition = self.CenterPosition;
				cachedRange = desiredRange;
				self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange, cachedVRange);
			}
		}

		public void AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
		}

		private void ActorEntered(Actor a)
		{
			if (a == self || a.Disposed || self.Disposed)
				return;
			var attachables = a.TraitsImplementing<DelayedWeaponAttachable>().Where(t => Info.Types.Contains(t.Info.Type));

			foreach (var attachable in attachables)
			{
				attachable.AddDetector(self);
			}
		}

		private void ActorExited(Actor a)
		{
			if (a.IsDead)
				return;

			var attachables = a.TraitsImplementing<DelayedWeaponAttachable>().Where(t => Info.Types.Contains(t.Info.Type));

			foreach (var attachable in attachables)
			{
				attachable.RemoveDetector(self);
			}
		}
	}
}
