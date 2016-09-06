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

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public struct SelectionBarsRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly Actor actor;
		readonly bool displayHealth;
		readonly bool displayExtra;

		public SelectionBarsRenderable(Actor actor, bool displayHealth, bool displayExtra)
			: this(actor.CenterPosition, actor)
		{
			this.displayHealth = displayHealth;
			this.displayExtra = displayExtra;
		}

		public SelectionBarsRenderable(WPos pos, Actor actor)
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
		public IRenderable OffsetBy(WVec vec) { return new SelectionBarsRenderable(pos + vec, actor); }
		public IRenderable AsDecoration() { return this; }

		void DrawExtraBars(WorldRenderer wr, float3 start, float3 end)
		{
			foreach (var extraBar in actor.TraitsImplementing<ISelectionBar>())
			{
				var value = extraBar.GetValue();
				if (value != 0 || extraBar.DisplayWhenEmpty)
				{
					var offset = new float3(0, (int)(4 / wr.Viewport.Zoom), 0);
					start += offset;
					end += offset;
					DrawSelectionBar(wr, start, end, extraBar.GetValue(), extraBar.GetColor());
				}
			}
		}

		void DrawSelectionBar(WorldRenderer wr, float3 start, float3 end, float value, Color barColor)
		{
			var iz = 1 / wr.Viewport.Zoom;
			var c = Color.FromArgb(128, 30, 30, 30);
			var c2 = Color.FromArgb(128, 10, 10, 10);
			var p = new float2(0, -4 * iz);
			var q = new float2(0, -3 * iz);
			var r = new float2(0, -2 * iz);

			var barColor2 = Color.FromArgb(255, barColor.R / 2, barColor.G / 2, barColor.B / 2);

			var z = float3.Lerp(start, end, value);
			var wcr = Game.Renderer.WorldRgbaColorRenderer;
			wcr.DrawLine(start + p, end + p, iz, c);
			wcr.DrawLine(start + q, end + q, iz, c2);
			wcr.DrawLine(start + r, end + r, iz, c);

			wcr.DrawLine(start + p, z + p, iz, barColor2);
			wcr.DrawLine(start + q, z + q, iz, barColor);
			wcr.DrawLine(start + r, z + r, iz, barColor2);
		}

		Color GetHealthColor(IHealth health)
		{
			if (Game.Settings.Game.UsePlayerStanceColors)
				return actor.Owner.PlayerStanceColor(actor);
			else
				return health.DamageState == DamageState.Critical ? Color.Red :
					health.DamageState == DamageState.Heavy ? Color.Yellow : Color.LimeGreen;
		}

		void DrawHealthBar(WorldRenderer wr, IHealth health, float3 start, float3 end)
		{
			if (health == null || health.IsDead)
				return;

			var c = Color.FromArgb(128, 30, 30, 30);
			var c2 = Color.FromArgb(128, 10, 10, 10);
			var iz = 1 / wr.Viewport.Zoom;
			var p = new float2(0, -4 * iz);
			var q = new float2(0, -3 * iz);
			var r = new float2(0, -2 * iz);

			var healthColor = GetHealthColor(health);
			var healthColor2 = Color.FromArgb(
				255,
				healthColor.R / 2,
				healthColor.G / 2,
				healthColor.B / 2);

			var z = float3.Lerp(start, end, (float)health.HP / health.MaxHP);

			var wcr = Game.Renderer.WorldRgbaColorRenderer;
			wcr.DrawLine(start + p, end + p, iz, c);
			wcr.DrawLine(start + q, end + q, iz, c2);
			wcr.DrawLine(start + r, end + r, iz, c);

			wcr.DrawLine(start + p, z + p, iz, healthColor2);
			wcr.DrawLine(start + q, z + q, iz, healthColor);
			wcr.DrawLine(start + r, z + r, iz, healthColor2);

			if (health.DisplayHP != health.HP)
			{
				var deltaColor = Color.OrangeRed;
				var deltaColor2 = Color.FromArgb(
					255,
					deltaColor.R / 2,
					deltaColor.G / 2,
					deltaColor.B / 2);
				var zz = float3.Lerp(start, end, (float)health.DisplayHP / health.MaxHP);

				wcr.DrawLine(z + p, zz + p, iz, deltaColor2);
				wcr.DrawLine(z + q, zz + q, iz, deltaColor);
				wcr.DrawLine(z + r, zz + r, iz, deltaColor2);
			}
		}

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			if (!actor.IsInWorld || actor.IsDead)
				return;

			var health = actor.TraitOrDefault<IHealth>();

			var screenPos = wr.Screen3DPxPosition(pos);
			var bounds = actor.VisualBounds;
			bounds.Offset((int)screenPos.X, (int)screenPos.Y);

			var start = new float3(bounds.Left + 1, bounds.Top, screenPos.Z);
			var end = new float3(bounds.Right - 1, bounds.Top, screenPos.Z);

			if (DisplayHealth)
				DrawHealthBar(wr, health, start, end);

			if (DisplayExtra)
				DrawExtraBars(wr, start, end);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
