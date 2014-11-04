#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Can be teleported via Chronoshift power.")]
	public class ChronoshiftableInfo : ITraitInfo
	{
		public readonly bool ExplodeInstead = false;
		public readonly string ChronoshiftSound = "chrono2.aud";

		public object Create(ActorInitializer init) { return new Chronoshiftable(init, this); }
	}

	public class Chronoshiftable : ITick, ISync, ISelectionBar
	{
		readonly ChronoshiftableInfo info;
		Actor chronosphere;
		bool killCargo;
		int duration;

		// Return-to-sender logic
		[Sync] public CPos Origin;
		[Sync] public int ReturnTicks = 0;

		public Chronoshiftable(ActorInitializer init, ChronoshiftableInfo info)
		{
			this.info = info;

			if (init.Contains<ChronoshiftReturnInit>())
				ReturnTicks = init.Get<ChronoshiftReturnInit, int>();

			if (init.Contains<ChronoshiftOriginInit>())
				Origin = init.Get<ChronoshiftOriginInit, CPos>();
		}

		public void Tick(Actor self)
		{
			if (ReturnTicks <= 0)
				return;

			// Return to original location
			if (--ReturnTicks == 0)
			{
				self.CancelActivity();
				self.QueueActivity(new Teleport(chronosphere, Origin, null, killCargo, true, info.ChronoshiftSound));
			}
		}

		// Can't be used in synced code, except with ignoreVis.
		public virtual bool CanChronoshiftTo(Actor self, CPos targetLocation)
		{
			// TODO: Allow enemy units to be chronoshifted into bad terrain to kill them
			return self.HasTrait<IPositionable>() && self.Trait<IPositionable>().CanEnterCell(targetLocation);
		}

		public virtual bool Teleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			// some things appear chronoshiftable, but instead they just die.
			if (info.ExplodeInstead)
			{
				self.World.AddFrameEndTask(w =>
				{
					// damage is inflicted by the chronosphere
					if (!self.Destroyed)
						self.InflictDamage(chronosphere, int.MaxValue, null);
				});
				return true;
			}

			/// Set up return-to-sender info
			Origin = self.Location;
			ReturnTicks = duration;
			this.duration = duration;
			this.chronosphere = chronosphere;
			this.killCargo = killCargo;

			// Set up the teleport
			self.CancelActivity();
			self.QueueActivity(new Teleport(chronosphere, targetLocation, null, killCargo, true, info.ChronoshiftSound));

			return true;
		}

		// Show the remaining time as a bar
		public float GetValue()
		{
			if (ReturnTicks == 0) // otherwise an empty bar is rendered all the time
				return 0f;

			return (float)ReturnTicks / duration;
		}

		public Color GetColor() { return Color.White; }
	}

	public class ChronoshiftReturnInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 0;
		public ChronoshiftReturnInit() { }
		public ChronoshiftReturnInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}

	public class ChronoshiftOriginInit : IActorInit<CPos>
	{
		[FieldFromYamlKey] readonly CPos value;
		public ChronoshiftOriginInit(CPos init) { value = init; }
		public CPos Value(World world) { return value; }
	}
}
