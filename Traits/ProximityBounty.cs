#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class ProximityBountyInfo : ITraitInfo
	{
		[Desc("The range within bounty gets collected.")]
		public readonly WDist Range = WDist.FromCells(7);

		[Desc("The maximum vertical range above terrain within bounty gets collected.",
		      "Ignored if 0 (actors are upgraded regardless of vertical distance).")]
		public readonly WDist MaximumVerticalOffset = WDist.Zero;

		[Desc("What killer diplomatic stances gathers bounty.")]
		public readonly Stance ValidStances = Stance.Ally | Stance.Neutral | Stance.Enemy;

		[Desc("Delay between awarding the bounty.")]
		public readonly int Delay = 50;

		[Desc("The type which allows the actor to collect nearby bounty.")]
		public readonly string BountyType = "Bounty";

		[Desc("Whether to show a floating text announcing the won bounty.")]
		public readonly bool ShowBounty = true;

		public readonly string EnableSound = null;
		public readonly string DisableSound = null;

		public object Create(ActorInitializer init) { return new ProximityBounty(init.Self, this); }
	}

	public class ProximityBounty : ITick, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction
	{
		public readonly ProximityBountyInfo Info;
		readonly Actor self;

		int proximityTrigger;
		WPos cachedPosition;
		WDist cachedRange;
		WDist desiredRange;
		WDist cachedVRange;
		WDist desiredVRange;

		bool cachedDisabled = true;
		int currentBounty;
		int ticks;

		public ProximityBounty(Actor self, ProximityBountyInfo info)
		{
			this.Info = info;
			this.self = self;
			cachedRange = info.Range;
			cachedVRange = info.MaximumVerticalOffset;
			ticks = Info.Delay;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
			GrantBounty();
		}

		void ITick.Tick(Actor self)
		{
			var disabled = self.IsDisabled();

			if (cachedDisabled != disabled)
			{
				Game.Sound.Play(disabled ? Info.DisableSound : Info.EnableSound, self.CenterPosition);
				desiredRange = disabled ? WDist.Zero : Info.Range;
				desiredVRange = disabled ? WDist.Zero : Info.MaximumVerticalOffset;
				cachedDisabled = disabled;
			}

			if (self.CenterPosition != cachedPosition || desiredRange != cachedRange || desiredVRange != cachedVRange)
			{
				cachedPosition = self.CenterPosition;
				cachedRange = desiredRange;
				cachedVRange = desiredVRange;
				self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange, cachedVRange);
			}

			if (--ticks < 0)
			{
				GrantBounty();

				ticks = Info.Delay;
			}
		}

		void GrantBounty()
		{
			if (currentBounty > 0)
			{
				var grantedBounty = currentBounty;

				if (Info.ShowBounty && self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(grantedBounty), 30)));

				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(currentBounty);

				currentBounty = 0;
			}
		}

		public void AddBounty(int bounty)
		{
			currentBounty += bounty;
		}

		void ActorEntered(Actor a)
		{
			if (a == self || a.Disposed || self.Disposed)
				return;

			var stance = self.Owner.Stances[a.Owner];
			if (!Info.ValidStances.HasStance(stance))
				return;

			var gpbs = a.TraitsImplementing<GivesProximityBounty>();
			foreach (var gpb in gpbs)
				gpb.Collectors.Add(this);
		}

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced)
		{
			// If the produced Actor doesn't occupy space, it can't be in range
			if (produced.OccupiesSpace == null)
				return;

			// We don't grant upgrades when disabled
			if (self.IsDisabled())
				return;

			// Work around for actors produced within the region not triggering until the second tick
			if ((produced.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= Info.Range.LengthSquared)
			{
				var stance = self.Owner.Stances[produced.Owner];
				if (!Info.ValidStances.HasStance(stance))
					return;

				var gpbs = produced.TraitsImplementing<GivesProximityBounty>();
				foreach (var gpb in gpbs)
					gpb.Collectors.Add(this);
			}
		}

		void ActorExited(Actor a)
		{
			if (a == self || a.Disposed || self.Disposed)
				return;

			var stance = self.Owner.Stances[a.Owner];
			if (!Info.ValidStances.HasStance(stance))
				return;

			var gpbs = a.TraitsImplementing<GivesProximityBounty>();
			foreach (var gpb in gpbs)
				gpb.Collectors.Remove(this);
		}
	}
}
