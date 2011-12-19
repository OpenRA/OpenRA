﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ContrailInfo : ITraitInfo
	{
		public readonly int[] ContrailOffset = {0, 0};

		public readonly int TrailLength = 20;
		public readonly Color Color = Color.White;
		public readonly bool UsePlayerColor = true;

		public object Create(ActorInitializer init) { return new Contrail(init.self, this); }
	}

	class Contrail : ITick, IPostRender
	{
		Turret contrailTurret = null;
		ContrailHistory history;
		IFacing facing;
		IMove move;

		public Contrail(Actor self, ContrailInfo info)
		{
			contrailTurret = new Turret(info.ContrailOffset);
			history = new ContrailHistory(info.TrailLength,
				info.UsePlayerColor ? ContrailHistory.ChooseColor(self) : info.Color);
			facing = self.Trait<IFacing>();
			move = self.Trait<IMove>();
		}

		public void Tick(Actor self)
		{
			history.Tick(self.CenterLocation - new int2(0, move.Altitude)
				- Combat.GetTurretPosition(self, facing, contrailTurret));
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self) { history.Render(self); }
	}

	class ContrailHistory
	{
		List<float2> positions = new List<float2>();
		readonly int TrailLength;
		readonly Color Color;
		readonly int StartSkip;

		public static Color ChooseColor(Actor self)
		{
			var ownerColor = Color.FromArgb(255, self.Owner.ColorRamp.GetColor(0));
			return PlayerColorRemap.ColorLerp(0.5f, ownerColor, Color.White);
		}

		public ContrailHistory(int trailLength, Color color)
			: this(trailLength, color, 0) { }

		public ContrailHistory(int trailLength, Color color, int startSkip)
		{
			this.TrailLength = trailLength;
			this.Color = color;
			this.StartSkip = startSkip;
		}

		public void Tick(float2 currentPos)
		{
			positions.Add(currentPos);
			if (positions.Count >= TrailLength)
				positions.RemoveAt(0);
		}

		public void Render(Actor self)
		{
			Color trailStart = Color;
			Color trailEnd = Color.FromArgb(trailStart.A - 255 / TrailLength, trailStart.R,
									        trailStart.G, trailStart.B);

			for (int i = positions.Count - 1 - StartSkip; i >= 1; --i)
			{
				var conPos = positions[i];
				var nextPos = positions[i - 1];

				if (self.World.RenderedShroud.IsVisible(OpenRA.Traits.Util.CellContaining(conPos)) ||
					self.World.RenderedShroud.IsVisible(OpenRA.Traits.Util.CellContaining(nextPos)))
				{
					Game.Renderer.WorldLineRenderer.DrawLine(conPos, nextPos, trailStart, trailEnd);

					trailStart = trailEnd;
					trailEnd = Color.FromArgb(trailStart.A - 255 / positions.Count, trailStart.R,
									            trailStart.G, trailStart.B);
				}
			}
		}
	}
}
