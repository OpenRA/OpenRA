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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;

namespace OpenRA.Mods.AS.Effects
{
	public class AirstrikePowerASEffect : IEffect
	{
		readonly AirstrikePowerASInfo info;
		readonly Player Owner;
		readonly World world;
		readonly WPos pos;

		IEnumerable<Actor> planes;
		Actor camera = null;
		Beacon beacon = null;
		bool enteredRange;

		public AirstrikePowerASEffect(World world, Player p, WPos pos, IEnumerable<Actor> planes, AirstrikePowerASInfo info)
		{
			this.info = info;
			this.world = world;
			this.Owner = p;
			this.pos = pos;
			this.planes = planes;

			if (info.DisplayBeacon)
			{
				var distance = (planes.First().OccupiesSpace.CenterPosition - pos).HorizontalLength;

				beacon = new Beacon(
					Owner,
					pos - new WVec(WDist.Zero, WDist.Zero, world.Map.DistanceAboveTerrain(pos)),
					info.BeaconPaletteIsPlayerPalette,
					info.BeaconPalette,
					info.BeaconImage,
					info.BeaconPoster,
					info.BeaconPosterPalette,
					info.ArrowSequence,
					info.CircleSequence,
					info.ClockSequence,
						() => 1 - ((planes.First().OccupiesSpace.CenterPosition - pos).HorizontalLength - info.BeaconDistanceOffset.Length) * 1f / distance);

				world.AddFrameEndTask(w => w.Add(beacon));
			}
		}

		void IEffect.Tick(World world)
		{
			planes = planes.Where(p => !p.IsDead);

			if (!enteredRange && planes.Any(p => (p.OccupiesSpace.CenterPosition - pos).Length < info.BeaconDistanceOffset.Length))
			{
					onEnterRange();
					enteredRange = true;
			}

			if (!planes.Any() || (enteredRange && planes.All(p => (p.OccupiesSpace.CenterPosition - pos).Length > info.BeaconDistanceOffset.Length)))
			{
				onExitRange();
				world.AddFrameEndTask(w => w.Remove(this));
			}
		}

		void onEnterRange()
		{
			// Spawn a camera and remove the beacon when the first plane enters the target area
			if (info.CameraActor != null)
			{
				world.AddFrameEndTask(w =>
				{
					var camera = w.CreateActor(info.CameraActor, new TypeDictionary
						{
							new LocationInit(world.Map.CellContaining(pos)),
							new OwnerInit(Owner),
						});
				});
			}

			TryRemoveBeacon();
		}

		void onExitRange()
		{
			if (camera != null)
			{
				camera.QueueActivity(new Wait(info.CameraRemoveDelay));
				camera.QueueActivity(new RemoveSelf());
			}

			camera = null;

			TryRemoveBeacon();
		}

		void TryRemoveBeacon()
		{
			if (beacon != null)
			{
				world.AddFrameEndTask(w =>
				{
					w.Remove(beacon);
					beacon = null;
				});
			}
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer r)
		{
			return Enumerable.Empty<IRenderable>();
		}
	}
}
