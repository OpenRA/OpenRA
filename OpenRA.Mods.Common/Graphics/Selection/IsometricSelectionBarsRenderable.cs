#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Graphics
{
	public struct IsometricSelectionBarsRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly Actor actor;
		readonly bool displayHealth;
		readonly bool displayExtra;
		readonly uint isometricHeight;
		readonly float2 barDim;

		public IsometricSelectionBarsRenderable(Actor actor, bool displayHealth, bool displayExtra, uint isometricHeight, float2 barDim)
			: this(actor.CenterPosition, actor)
		{
			this.displayHealth = displayHealth;
			this.displayExtra = displayExtra;
			this.isometricHeight = isometricHeight;
			this.barDim = barDim;
		}

		public IsometricSelectionBarsRenderable(WPos pos, Actor actor)
			: this()
		{
			this.pos = pos;
			this.actor = actor;
		}

		public WPos Pos { get { return pos; } }
		public bool DisplayHealth { get { return displayHealth; } }
		public bool DisplayExtra { get { return displayExtra; } }

		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return new IsometricSelectionBarsRenderable(pos + vec, actor); }
		public IRenderable AsDecoration() { return this; }

		void DrawExtraBars(WorldRenderer wr, float3 start, float3 end)
		{
			int c = 1;
			foreach (var extraBar in actor.TraitsImplementing<ISelectionBar>())
			{
				var value = extraBar.GetValue();
				if (value != 0 || extraBar.DisplayWhenEmpty)
					DrawBar(wr, extraBar.GetValue(), extraBar.GetColor(), c++);
			}
		}

		void DrawBar(WorldRenderer wr, float value, Color barColor, int barNum = 0, float displayValue = -1f)
		{
			IEnumerable<CPos> cells;
			var color = Color.FromArgb(100, 30, 30, 30);
			var colorB = Color.FromArgb(50, 30, 30, 30);
			var shadowColor = Color.FromArgb(barColor.A, barColor.R / 2, barColor.G / 2, barColor.B / 2);
			var isoOff = new float3(1f, 0.5f, 0);
			var isoOff2 = new float3(0, 0.5f, 0);

			var building = actor.TraitOrDefault<Building>();
			if (building != null)
				cells = FootprintUtils.Tiles(actor);
			else
				cells = new CPos[1] { actor.Location };

			var xMin = cells.Min(c => c.X);
			var tc = cells.Where(c => c.X == xMin).MinBy(c => c.Y);
			var lc = cells.Where(c => c.X == xMin).MaxBy(c => c.Y);

			var tp = wr.Screen3DPxPosition(wr.World.Map.CenterOfCell(tc) + new WVec(0, -724, 0));
			var lp = wr.Screen3DPxPosition(wr.World.Map.CenterOfCell(lc) + new WVec(-724, 0, 0));
			tp += barNum * barDim.X * isoOff;
			lp += barNum * barDim.X * isoOff;

			var iz = 1 / wr.Viewport.Zoom;
			var z = float3.Lerp(tp, lp, value);
			var top = new float3(0, -isometricHeight, 0);

			var wcr = Game.Renderer.WorldRgbaColorRenderer;

			for (int i = 1; i < barDim.X; ++i)
			{
				wcr.DrawLine(tp + top + i * isoOff, z + top + i * isoOff, 1.0f, barColor);
				wcr.DrawLine(z + top + i * isoOff, z + top + i * isoOff + (barDim.Y - 1) * isoOff2, 1.0f, shadowColor);
			}

			for (int i = 1; i < barDim.Y; ++i)
				wcr.DrawLine(tp + top + (barDim.X - 1) * isoOff + i * isoOff2, z + top + (barDim.X - 1) * isoOff + i * isoOff2, 1.0f, shadowColor);

			// Wireframe
			wcr.DrawLine(tp + top, lp + top, iz, color);
			wcr.DrawLine(tp + top, tp + top + (barDim.Y - 1) * isoOff2, 1f, colorB);
			wcr.DrawLine(lp + top, lp + top + (barDim.Y - 1) * isoOff2, 1f, color);
			wcr.DrawLine(lp + top + (barDim.Y - 1) * isoOff2, tp + top + (barDim.Y - 1) * isoOff2, 1f, colorB);

			wcr.DrawLine(tp + top, tp + top + (barDim.X - 1) * isoOff, 1f, color);
			wcr.DrawLine(tp + top + (barDim.Y - 1) * isoOff2, tp + top + (barDim.X - 1) * isoOff + (barDim.Y - 1) * isoOff2, 1f, colorB);
			wcr.DrawLine(tp + top + (barDim.X - 1) * isoOff, tp + top + (barDim.X - 1) * isoOff + (barDim.Y - 1) * isoOff2, 1f, color);
			wcr.DrawLine(tp + top + (barDim.X - 1) * isoOff, lp + top + (barDim.X - 1) * isoOff, 1f, color);
			wcr.DrawLine(tp + top + (barDim.X - 1) * isoOff + (barDim.Y - 1) * isoOff2, lp + top + (barDim.X - 1) * isoOff + (barDim.Y - 1) * isoOff2, 1f, color);
			wcr.DrawLine(lp + top + (barDim.X - 1) * isoOff, lp + top + (barDim.X - 1) * isoOff + (barDim.Y - 1) * isoOff2, 1f, color);
			wcr.DrawLine(lp + top, lp + top + (barDim.X - 1) * isoOff, 1f, color);
			wcr.DrawLine(lp + top + (barDim.Y - 1) * isoOff2, lp + top + (barDim.X - 1) * isoOff + (barDim.Y - 1) * isoOff2, 1f, color);

			if (displayValue != -1 && displayValue != value)
			{
				var deltaColor = Color.OrangeRed;
				var deltaShadow = Color.FromArgb(
					deltaColor.A,
					deltaColor.R / 2,
					deltaColor.G / 2,
					deltaColor.B / 2);
				var zz = float3.Lerp(tp, lp, displayValue);

				for (int i = 1; i < barDim.X; ++i)
				{
					wcr.DrawLine(z + top + i * isoOff, zz + top + i * isoOff, 1.0f, deltaColor);
					wcr.DrawLine(zz + top + i * isoOff, zz + top + i * isoOff + (barDim.Y - 1) * isoOff2, 1.0f, deltaShadow);
				}

				for (int i = 1; i < barDim.Y; ++i)
					wcr.DrawLine(z + top + (barDim.X - 1) * isoOff + i * isoOff2, zz + top + (barDim.X - 1) * isoOff + i * isoOff2, 1.0f, deltaShadow);
			}
		}

		Color GetHealthColor(IHealth health)
		{
			if (Game.Settings.Game.UsePlayerStanceColors)
				return actor.Owner.PlayerStanceColor(actor);
			else
				return health.DamageState == DamageState.Critical ? Color.Red :
					health.DamageState == DamageState.Heavy ? Color.Yellow : Color.LimeGreen;
		}

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			if (!actor.IsInWorld || actor.IsDead || isometricHeight == 0)
				return;

			var health = actor.TraitOrDefault<IHealth>();

			var screenPos = wr.Screen3DPxPosition(pos);
			var bounds = actor.VisualBounds;
			bounds.Offset((int)screenPos.X, (int)screenPos.Y);

			var start = new float3(bounds.Left + 1, bounds.Top, screenPos.Z);
			var end = new float3(bounds.Right - 1, bounds.Top, screenPos.Z);

			if (DisplayHealth)
			{
				if (health == null || health.IsDead)
					return;

				DrawBar(wr, (float)health.HP / health.MaxHP, GetHealthColor(health), 0, (float)health.DisplayHP / health.MaxHP);
			}

			if (DisplayExtra)
				DrawExtraBars(wr, start, end);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
