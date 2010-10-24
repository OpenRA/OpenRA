#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;
using System.Drawing;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	class ContrailInfo : ITraitInfo
	{
		public readonly int[] ContrailOffset = {0, 0};

		public readonly int TrailLength = 20;
		public readonly int[] TrailColor = null;

		public object Create(ActorInitializer init) { return new Contrail(init.self, this); }
	}

	class Contrail : ITick, IPostRender
	{
		private ContrailInfo Info = null;

		private List<float2> positions = new List<float2>();

		private Turret ContrailTurret = null;

		private int TrailLength = 0;
		private Color TrailColor = Color.White;

		public Contrail(Actor self, ContrailInfo info)
		{
			Info = info;

			ContrailTurret = new Turret(Info.ContrailOffset);

			TrailLength = Info.TrailLength;

			// if no color specified or wrong format, blend with owner color
			if (Info.TrailColor == null || Info.TrailColor.Length != 4)
			{
				var ownerColor = Color.FromArgb(255, self.Owner.Color);
				TrailColor = PlayerColorRemap.ColorLerp(0.5f, ownerColor, Color.White);
			}
			else
			{
				// otherwise, blend with specified color
				var blendColor = Color.FromArgb(255, Info.TrailColor[1].Clamp(0, 255),
					Info.TrailColor[2].Clamp(0, 255), Info.TrailColor[3].Clamp(0, 255));
				TrailColor = PlayerColorRemap.ColorLerp(0.5f, blendColor, Color.White);
			}
		}

		public void Tick(Actor self)
		{
			var facing = self.Trait<IFacing>();
			var altitude = new float2(0, self.Trait<IMove>().Altitude);

			float2 pos = self.CenterLocation - Combat.GetTurretPosition(self, facing, ContrailTurret) - altitude;
		
			positions.Add(pos);

			if (positions.Count >= TrailLength)
			{
				positions.RemoveAt(0);
			}
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			Color trailStart = TrailColor;
			Color trailEnd = Color.FromArgb(trailStart.A - 255 / TrailLength, trailStart.R,
											trailStart.G, trailStart.B);

			for (int i = positions.Count - 1; i >= 1; --i)
			{
				var conPos = positions[i];
				var nextPos = positions[i - 1];
				ShroudRenderer shroud = null;

				// LocalPlayer is null on shellmap
				if (self.World.LocalPlayer != null)
				{
					shroud = self.World.LocalPlayer.Shroud;
				}

				if (shroud == null ||
					shroud.IsVisible(OpenRA.Traits.Util.CellContaining(conPos)) ||
					shroud.IsVisible(OpenRA.Traits.Util.CellContaining(nextPos)))
				{
					Game.Renderer.LineRenderer.DrawLine(conPos, nextPos, trailStart, trailEnd);

					trailStart = trailEnd;
					trailEnd = Color.FromArgb(trailStart.A - 255 / positions.Count, trailStart.R,
												trailStart.G, trailStart.B);
				}
			}
		}
	}
}
