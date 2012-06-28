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
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Scripting
{
	public class RASpecialPowers
	{
		public static void Chronoshift(World world, List<Pair<Actor, CPos>> units, Actor chronosphere, int duration, bool killCargo)
		{
			if (chronosphere != null)
				chronosphere.Trait<RenderBuilding>().PlayCustomAnim(chronosphere, "active");

			// Trigger screen desaturate effect
			foreach (var a in world.ActorsWithTrait<ChronoshiftPaletteEffect>())
				a.Trait.Enable();

			Sound.Play("chrono2.aud", units.First().First.CenterLocation);

			foreach (var kv in units)
			{
				var target = kv.First;
				var targetCell = kv.Second;
				var cs = target.Trait<Chronoshiftable>();
				if (cs.CanChronoshiftTo(target, targetCell, true))
					cs.Teleport(target, targetCell, duration, killCargo,chronosphere);
			}


		}
	}
}
