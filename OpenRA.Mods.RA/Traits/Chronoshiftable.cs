#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Can be teleported via Chronoshift power.")]
	public class ChronoshiftableInfo : ITraitInfo
	{
		public readonly bool ExplodeInstead = false;
		public readonly string ChronoshiftSound = "chrono2.aud";

		public object Create(ActorInitializer init) { return new Chronoshiftable(init, this); }
	}

	public class Chronoshiftable : ITick, ISync, ISelectionBar, IDeathActorInitModifier
	{
		readonly ChronoshiftableInfo info;
		readonly Actor self;
		Actor chronosphere;
		bool killCargo;
		int duration;

		// Return-to-sender logic
		[Sync] public CPos Origin;
		[Sync] public int ReturnTicks = 0;

		public Chronoshiftable(ActorInitializer init, ChronoshiftableInfo info)
		{
			this.info = info;
			self = init.Self;

			if (init.Contains<ChronoshiftReturnInit>())
				ReturnTicks = init.Get<ChronoshiftReturnInit, int>();

			if (init.Contains<ChronoshiftOriginInit>())
				Origin = init.Get<ChronoshiftOriginInit, CPos>();

			if (init.Contains<ChronoshiftChronosphereInit>())
				chronosphere = init.Get<ChronoshiftChronosphereInit, Actor>();
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
			return self.Info.HasTraitInfo<IPositionableInfo>() && self.Trait<IPositionable>().CanEnterCell(targetLocation);
		}

		public virtual bool Teleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			// some things appear chronoshiftable, but instead they just die.
			if (info.ExplodeInstead)
			{
				self.World.AddFrameEndTask(w =>
				{
					// damage is inflicted by the chronosphere
					if (!self.Disposed)
						self.InflictDamage(chronosphere, int.MaxValue, null);
				});
				return true;
			}

			// Set up return-to-sender info
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
			// otherwise an empty bar is rendered all the time
			if (ReturnTicks == 0 || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0f;

			return (float)ReturnTicks / duration;
		}

		public Color GetColor() { return Color.White; }

		public void ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			if (ReturnTicks <= 0)
				return;
			init.Add(new ChronoshiftOriginInit(Origin));
			init.Add(new ChronoshiftReturnInit(ReturnTicks));
			if (chronosphere != self)
				init.Add(new ChronoshiftChronosphereInit(chronosphere));
		}
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

	public class ChronoshiftChronosphereInit : IActorInit<Actor>
	{
		readonly Actor value;
		public ChronoshiftChronosphereInit(Actor init) { value = init; }
		public Actor Value(World world) { return value; }
	}
}
