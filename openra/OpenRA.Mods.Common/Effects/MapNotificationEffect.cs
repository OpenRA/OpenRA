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

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Effects
{
	public class MapNotificationEffect : IEffect
	{
		readonly RadarPings radarPings;

		readonly WPos pos;
		readonly Player player;
		readonly int duration;
		readonly string category;
		readonly string notification;
		readonly bool visible;
		readonly Color color;

		int remainingDelay;

		public MapNotificationEffect(Player player, string category, string notification, int delay,
			bool pingVisible, WPos pos, Color pingColor, int pingDuration = 50)
		{
			this.player = player;
			remainingDelay = delay;
			this.category = category;
			this.notification = notification;
			this.pos = pos;
			duration = pingDuration;
			visible = pingVisible;
			color = pingColor;

			radarPings = player.World.WorldActor.TraitOrDefault<RadarPings>();
		}

		public void Tick(World world)
		{
			if (remainingDelay-- > 0)
				return;

			Game.Sound.PlayNotification(player.World.Map.Rules, player, category, notification, player.Faction.InternalName);

			if (visible && radarPings != null && player == player.World.RenderPlayer)
				radarPings.Add(() => true, pos, color, duration);

			world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr) { return SpriteRenderable.None; }
	}
}
