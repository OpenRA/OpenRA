#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Radar")]
	public class RadarGlobal : ScriptGlobal
	{
		readonly RadarPings radarPings;

		public RadarGlobal(ScriptContext context)
			: base(context)
		{
			radarPings = context.World.WorldActor.TraitOrDefault<RadarPings>();
		}

		[Desc("Creates a new radar ping that stays for the specified time at the specified WPos.")]
		public void Ping(Player player, WPos position, Color color, int duration = 750)
		{
			radarPings?.Add(() => player.World.RenderPlayer == player, position, color, duration);
		}
	}
}
