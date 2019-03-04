#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Can be teleported via Chronoshift power.")]
	public class ChronoshiftableInfo : ConditionalTraitInfo
	{
		[Desc("Should the actor die instead of being teleported?")]
		public readonly bool ExplodeInstead = false;

		[Desc("Types of damage that this trait causes to self when 'ExplodeInstead' is true",
			"or the return-to-origin is blocked. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public readonly string ChronoshiftSound = "chrono2.aud";

		[Desc("Should the actor return to its previous location after the chronoshift wore out?")]
		public readonly bool ReturnToOrigin = true;

		[Desc("The color the bar of the 'return-to-origin' logic has.")]
		public readonly Color TimeBarColor = Color.White;

		public override object Create(ActorInitializer init) { return new Chronoshiftable(init, this); }
	}

	public class Chronoshiftable : ConditionalTrait<ChronoshiftableInfo>, ITick, ISync, ISelectionBar,
		IDeathActorInitModifier, ITransformActorInitModifier, INotifyCreated
	{
		readonly Actor self;
		Actor chronosphere;
		bool killCargo;
		int duration;
		IPositionable iPositionable;

		// Return-to-origin logic
		[Sync] public CPos Origin;
		[Sync] public int ReturnTicks = 0;

		public Chronoshiftable(ActorInitializer init, ChronoshiftableInfo info)
			: base(info)
		{
			self = init.Self;

			if (init.Contains<ChronoshiftReturnInit>())
				ReturnTicks = init.Get<ChronoshiftReturnInit, int>();

			if (init.Contains<ChronoshiftDurationInit>())
				duration = init.Get<ChronoshiftDurationInit, int>();

			if (init.Contains<ChronoshiftOriginInit>())
				Origin = init.Get<ChronoshiftOriginInit, CPos>();

			if (init.Contains<ChronoshiftChronosphereInit>())
				chronosphere = init.Get<ChronoshiftChronosphereInit, Actor>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !Info.ReturnToOrigin || ReturnTicks <= 0)
				return;

			// Return to original location
			if (--ReturnTicks == 0)
			{
				self.CancelActivity();

				// The Move activity is not immediately cancelled, which, combined
				// with Activity.Cancel discarding NextActivity without checking the
				// IsInterruptable flag, means that a well timed order can cancel the
				// Teleport activity queued below - an exploit / cheat of the return mechanic.
				// The Teleport activity queued below is guaranteed to either complete
				// (force-resetting the actor to the middle of the target cell) or kill
				// the actor. It is therefore safe to force-erase the Move activity to
				// work around the cancellation bug.
				// HACK: this is manipulating private internal actor state
				if (self.CurrentActivity is Move)
					typeof(Actor).GetProperty("CurrentActivity").SetValue(self, null);

				// The actor is killed using Info.DamageTypes if the teleport fails
				self.QueueActivity(new Teleport(chronosphere, Origin, null, true, killCargo, Info.ChronoshiftSound,
					false, true, Info.DamageTypes));
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			iPositionable = self.TraitOrDefault<IPositionable>();
		}

		// Can't be used in synced code, except with ignoreVis.
		public virtual bool CanChronoshiftTo(Actor self, CPos targetLocation)
		{
			// TODO: Allow enemy units to be chronoshifted into bad terrain to kill them
			return !IsTraitDisabled && iPositionable != null && iPositionable.CanEnterCell(targetLocation);
		}

		public virtual bool Teleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			if (IsTraitDisabled)
				return false;

			// Some things appear chronoshiftable, but instead they just die.
			if (Info.ExplodeInstead)
			{
				self.World.AddFrameEndTask(w =>
				{
					// Damage is inflicted by the chronosphere
					if (!self.Disposed)
						self.Kill(chronosphere, Info.DamageTypes);
				});
				return true;
			}

			// Set up return-to-origin info
			// If this actor is already counting down to return to
			// an existing location then we shouldn't override it
			if (ReturnTicks <= 0)
			{
				Origin = self.Location;
				ReturnTicks = duration;
			}

			this.duration = duration;
			this.chronosphere = chronosphere;
			this.killCargo = killCargo;

			// Set up the teleport
			self.CancelActivity();
			self.QueueActivity(new Teleport(chronosphere, targetLocation, null, killCargo, true, Info.ChronoshiftSound));

			return true;
		}

		// Show the remaining time as a bar
		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ReturnToOrigin)
				return 0f;

			// Otherwise an empty bar is rendered all the time
			if (ReturnTicks == 0 || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0f;

			return (float)ReturnTicks / duration;
		}

		Color ISelectionBar.GetColor() { return Info.TimeBarColor; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		void ModifyActorInit(TypeDictionary init)
		{
			if (IsTraitDisabled || !Info.ReturnToOrigin || ReturnTicks <= 0)
				return;

			init.Add(new ChronoshiftOriginInit(Origin));
			init.Add(new ChronoshiftReturnInit(ReturnTicks));
			init.Add(new ChronoshiftDurationInit(duration));
			if (chronosphere != self)
				init.Add(new ChronoshiftChronosphereInit(chronosphere));
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init) { ModifyActorInit(init); }
		void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init) { ModifyActorInit(init); }
	}

	public class ChronoshiftReturnInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 0;
		public ChronoshiftReturnInit() { }
		public ChronoshiftReturnInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}

	public class ChronoshiftDurationInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 0;
		public ChronoshiftDurationInit() { }
		public ChronoshiftDurationInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}

	public class ChronoshiftOriginInit : IActorInit<CPos>
	{
		[FieldFromYamlKey] readonly CPos value;
		public ChronoshiftOriginInit(CPos init) { value = init; }
		public CPos Value(World world) { return value; }
	}

	public class ChronoshiftChronosphereInit : IActorInit<Actor>
	{
		readonly Actor value;
		public ChronoshiftChronosphereInit(Actor init) { value = init; }
		public Actor Value(World world) { return value; }
	}
}
