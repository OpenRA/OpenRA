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
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ContrailInfo : ITraitInfo, Requires<LocalCoordinatesModelInfo>
	{
		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public readonly int TrailLength = 25;
		public readonly Color Color = Color.White;
		public readonly bool UsePlayerColor = true;

		public object Create(ActorInitializer init) { return new Contrail(init.self, this); }
	}

	class Contrail : ITick, IPostRender
	{
		ContrailInfo info;
		ContrailHistory history;
		ILocalCoordinatesModel coords;

		public Contrail(Actor self, ContrailInfo info)
		{
			this.info = info;
			history = new ContrailHistory(info.TrailLength,
				info.UsePlayerColor ? ContrailHistory.ChooseColor(self) : info.Color);

			coords = self.Trait<ILocalCoordinatesModel>();
		}

		public void Tick(Actor self)
		{
			var local = info.Offset.Rotate(coords.QuantizeOrientation(self, self.Orientation));
			history.Tick(self.CenterPosition + coords.LocalToWorld(local));
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self) { history.Render(wr, self); }
	}

	class ContrailHistory
	{
		List<WPos> positions = new List<WPos>();
		readonly int TrailLength;
		readonly Color Color;
		readonly int StartSkip;

		public static Color ChooseColor(Actor self)
		{
			var ownerColor = Color.FromArgb(255, self.Owner.ColorRamp.GetColor(0));
			return Exts.ColorLerp(0.5f, ownerColor, Color.White);
		}

		public ContrailHistory(int trailLength, Color color)
			: this(trailLength, color, 0) { }

		public ContrailHistory(int trailLength, Color color, int startSkip)
		{
			this.TrailLength = trailLength;
			this.Color = color;
			this.StartSkip = startSkip;
		}

		public void Tick(WPos currentPos)
		{
			positions.Add(currentPos);
			if (positions.Count >= TrailLength)
				positions.RemoveAt(0);
		}

		public void Render(WorldRenderer wr, Actor self)
		{
			Color trailStart = Color;
			Color trailEnd = Color.FromArgb(trailStart.A - 255 / TrailLength, trailStart.R, trailStart.G, trailStart.B);

			for (int i = positions.Count - 1 - StartSkip; i >= 4; --i)
			{
				// World positions
				var conPos = WPos.Average(positions[i], positions[i-1], positions[i-2], positions[i-3]);
				var nextPos = WPos.Average(positions[i-1], positions[i-2], positions[i-3], positions[i-4]);

				if (!self.World.FogObscures(new CPos(conPos)) &&
				    !self.World.FogObscures(new CPos(nextPos)))
				{
					Game.Renderer.WorldLineRenderer.DrawLine(wr.ScreenPosition(conPos), wr.ScreenPosition(nextPos), trailStart, trailEnd);

					trailStart = trailEnd;
					trailEnd = Color.FromArgb(trailStart.A - 255 / positions.Count, trailStart.R, trailStart.G, trailStart.B);
				}
			}
		}
	}
}
