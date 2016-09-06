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

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Effects
{
	class RallyPointIndicator : IEffect, IEffectAboveShroud
	{
		readonly Actor building;
		readonly RallyPoint rp;
		readonly Animation flag;
		readonly Animation circles;
		readonly ExitInfo[] exits;

		readonly WPos[] targetLine = new WPos[2];
		CPos cachedLocation;

		public RallyPointIndicator(Actor building, RallyPoint rp, ExitInfo[] exits)
		{
			this.building = building;
			this.rp = rp;
			this.exits = exits;

			flag = new Animation(building.World, rp.Info.Image);
			flag.PlayRepeating(rp.Info.FlagSequence);

			circles = new Animation(building.World, rp.Info.Image);
			circles.Play(rp.Info.CirclesSequence);
		}

		void IEffect.Tick(World world)
		{
			flag.Tick();
			circles.Tick();

			if (cachedLocation != rp.Location)
			{
				cachedLocation = rp.Location;

				var rallyPos = world.Map.CenterOfCell(cachedLocation);
				var exitPos = building.CenterPosition;

				// Find closest exit
				var dist = int.MaxValue;
				foreach (var exit in exits)
				{
					var ep = building.CenterPosition + exit.SpawnOffset;
					var len = (rallyPos - ep).Length;
					if (len < dist)
					{
						dist = len;
						exitPos = ep;
					}
				}

				targetLine[0] = exitPos;
				targetLine[1] = rallyPos;

				circles.Play(rp.Info.CirclesSequence);
			}

			if (!building.IsInWorld || building.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer wr)
		{
			if (!building.IsInWorld || !building.Owner.IsAlliedWith(building.World.LocalPlayer))
				return SpriteRenderable.None;

			if (!building.World.Selection.Contains(building))
				return SpriteRenderable.None;

			return RenderInner(wr);
		}

		IEnumerable<IRenderable> RenderInner(WorldRenderer wr)
		{
			var palette = wr.Palette(rp.PaletteName);

			if (Game.Settings.Game.DrawTargetLine)
				yield return new TargetLineRenderable(targetLine, building.Owner.Color.RGB);

			foreach (var r in circles.Render(targetLine[1], palette))
				yield return r;

			foreach (var r in flag.Render(targetLine[1], palette))
				yield return r;
		}
	}
}
