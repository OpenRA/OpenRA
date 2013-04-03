#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class DesertShellmapScriptInfo : TraitInfo<DesertShellmapScript>, Requires<SpawnMapActorsInfo> { }

	class DesertShellmapScript : ITick, IWorldLoaded
	{
		World world;
		Player allies;
		Player soviets;

		List<int2> viewportTargets = new List<int2>();
		int2 viewportTarget;
		int viewportTargetNumber;
		int2 viewportOrigin;
		float mul;
		float div = 400;
		int waitTicks = 0;

		public void Tick(Actor self)
		{
			if (--waitTicks > 0)
				return;
			if (++mul <= div)
				Game.MoveViewport(float2.Lerp(viewportOrigin, viewportTarget, mul / div));
			else
			{
				mul = 0;
				viewportOrigin = viewportTarget;
				viewportTarget = viewportTargets[(viewportTargetNumber = (viewportTargetNumber + 1) % viewportTargets.Count)];
				waitTicks = 100;
			}
		}

		public void WorldLoaded(World w)
		{
			world = w;

			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");

			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			for (var i = 1; i <= 5; i++)
				viewportTargets.Add(actors["ViewportTarget" + i].Location.ToInt2());

			AutoTarget at = null;
			foreach (var actor in actors.Values.Where(a => a.Owner == allies && (at = a.TraitOrDefault<AutoTarget>()) != null))
				at.stance = UnitStance.Defend;

			viewportOrigin = viewportTargets[0];
			viewportTargetNumber = 1;
			viewportTarget = viewportTargets[1];
			Game.viewport.Center(viewportOrigin);
			Sound.SoundVolumeModifier = 0.25f;
		}
	}
}
