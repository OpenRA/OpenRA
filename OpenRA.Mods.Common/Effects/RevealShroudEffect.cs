#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class RevealShroudEffect : IEffect
	{
		static readonly PPos[] NoCells = { };

		readonly WPos pos;
		readonly Player player;
		readonly Shroud.SourceType sourceType;
		readonly WDist revealRadius;
		readonly PlayerRelationship validStances;
		readonly int duration;

		int ticks;

		public RevealShroudEffect(WPos pos, WDist radius, Shroud.SourceType type, Player forPlayer, PlayerRelationship stances, int delay = 0, int duration = 50)
		{
			this.pos = pos;
			player = forPlayer;
			revealRadius = radius;
			validStances = stances;
			sourceType = type;
			this.duration = duration;
			ticks = -delay;
		}

		void AddCellsToPlayerShroud(Player p, PPos[] uv)
		{
			if (validStances.HasStance(player.RelationshipWith(p)))
				p.Shroud.AddSource(this, sourceType, uv);
		}

		void RemoveCellsFromPlayerShroud(Player p) { p.Shroud.RemoveSource(this); }

		PPos[] ProjectedCells(World world)
		{
			var map = world.Map;
			var range = revealRadius;
			if (range == WDist.Zero)
				return NoCells;

			return Shroud.ProjectedCellsInRange(map, pos, WDist.Zero, range).ToArray();
		}

		public void Tick(World world)
		{
			if (ticks == 0)
			{
				var cells = ProjectedCells(world);
				foreach (var p in world.Players)
					AddCellsToPlayerShroud(p, cells);
			}

			if (ticks == duration)
			{
				foreach (var p in world.Players)
					RemoveCellsFromPlayerShroud(p);

				world.AddFrameEndTask(w => w.Remove(this));
			}

			ticks++;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr) { return SpriteRenderable.None; }
	}
}
