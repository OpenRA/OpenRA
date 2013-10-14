#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class SelectableInfo : ITraitInfo
	{
		public readonly bool Selectable = true;
		public readonly int Priority = 10;
		public readonly int[] Bounds = null;
		[VoiceReference] public readonly string Voice = null;

		public object Create(ActorInitializer init) { return new Selectable(init.self, this); }
	}

	public class Selectable : IPostRenderSelection
	{
		public SelectableInfo Info;
		Actor self;

		public Selectable(Actor self, SelectableInfo info)
		{
			this.self = self;
			Info = info;
		}

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (!Info.Selectable)
				return;

			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var bounds = self.Bounds.Value;
			bounds.Offset(pos.X, pos.Y);

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);

			wr.DrawSelectionBox(self, Color.White);
			DrawHealthBar(wr, self, xy, Xy);
			DrawExtraBars(wr, self, xy, Xy);
			DrawUnitPath(wr, self);
		}

		public void DrawRollover(WorldRenderer wr, Actor self)
		{
			if (!Info.Selectable)
				return;

			var pos = wr.ScreenPxPosition(self.CenterPosition);
			var bounds = self.Bounds.Value;
			bounds.Offset(pos.X, pos.Y);

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);

			DrawHealthBar(wr, self, xy, Xy);
			DrawExtraBars(wr, self, xy, Xy);
		}

		void DrawExtraBars(WorldRenderer wr, Actor self, float2 xy, float2 Xy)
		{
			foreach (var extraBar in self.TraitsImplementing<ISelectionBar>())
			{
				var value = extraBar.GetValue();
				if (value != 0)
				{
					xy.Y += 4;
					Xy.Y += 4;
					DrawSelectionBar(wr, self, xy, Xy, extraBar.GetValue(), extraBar.GetColor());
				}
			}
		}

		void DrawSelectionBar(WorldRenderer wr, Actor self, float2 xy, float2 Xy, float value, Color barColor)
		{
			if (!self.IsInWorld) return;

			var health = self.TraitOrDefault<Health>();
			if (health == null || health.IsDead) return;

			var c = Color.FromArgb(128, 30, 30, 30);
			var c2 = Color.FromArgb(128, 10, 10, 10);

			var barColor2 = Color.FromArgb(255, barColor.R / 2, barColor.G / 2, barColor.B / 2);

			var z = float2.Lerp(xy, Xy, value);
			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(xy + new float2(0, -4), Xy + new float2(0, -4), c, c);
			wlr.DrawLine(xy + new float2(0, -3), Xy + new float2(0, -3), c2, c2);
			wlr.DrawLine(xy + new float2(0, -2), Xy + new float2(0, -2), c, c);

			wlr.DrawLine(xy + new float2(0, -3), z + new float2(0, -3), barColor, barColor);
			wlr.DrawLine(xy + new float2(0, -2), z + new float2(0, -2), barColor2, barColor2);
			wlr.DrawLine(xy + new float2(0, -4), z + new float2(0, -4), barColor2, barColor2);
		}

		void DrawHealthBar(WorldRenderer wr, Actor self, float2 xy, float2 Xy)
		{
			if (!self.IsInWorld) return;

			var health = self.TraitOrDefault<Health>();
			if (health == null || health.IsDead) return;

			var c = Color.FromArgb(128, 30, 30, 30);
			var c2 = Color.FromArgb(128, 10, 10, 10);

			var healthColor = (health.DamageState == DamageState.Critical) ? Color.Red :
							  (health.DamageState == DamageState.Heavy) ? Color.Yellow : Color.LimeGreen;

			var healthColor2 = Color.FromArgb(
				255,
				healthColor.R / 2,
				healthColor.G / 2,
				healthColor.B / 2);

			var z = float2.Lerp(xy, Xy, (float)health.HP / health.MaxHP);

			var wlr = Game.Renderer.WorldLineRenderer;
			wlr.DrawLine(xy + new float2(0, -4), Xy + new float2(0, -4), c, c);
			wlr.DrawLine(xy + new float2(0, -3), Xy + new float2(0, -3), c2, c2);
			wlr.DrawLine(xy + new float2(0, -2), Xy + new float2(0, -2), c, c);

			wlr.DrawLine(xy + new float2(0, -3), z + new float2(0, -3), healthColor, healthColor);
			wlr.DrawLine(xy + new float2(0, -2), z + new float2(0, -2), healthColor2, healthColor2);
			wlr.DrawLine(xy + new float2(0, -4), z + new float2(0, -4), healthColor2, healthColor2);

			if (health.DisplayHp != health.HP)
			{
				var deltaColor = Color.OrangeRed;
				var deltaColor2 = Color.FromArgb(
					255,
					deltaColor.R / 2,
					deltaColor.G / 2,
					deltaColor.B / 2);
				var zz = float2.Lerp(xy, Xy, (float)health.DisplayHp / health.MaxHP);

				wlr.DrawLine(z + new float2(0, -3), zz + new float2(0, -3), deltaColor, deltaColor);
				wlr.DrawLine(z + new float2(0, -2), zz + new float2(0, -2), deltaColor2, deltaColor2);
				wlr.DrawLine(z + new float2(0, -4), zz + new float2(0, -4), deltaColor2, deltaColor2);
			}
		}

		void DrawUnitPath(WorldRenderer wr, Actor self)
		{
			if (self.World.LocalPlayer == null || !self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>().PathDebug)
				return;

			var activity = self.GetCurrentActivity();
			if (activity != null)
			{
				var targets = activity.GetTargets(self);
				var start = wr.ScreenPxPosition(self.CenterPosition);

				var c = Color.Green;
				foreach (var stp in targets.Where(t => t.Type != TargetType.Invalid).Select(pos => wr.ScreenPxPosition(pos.CenterPosition)))
				{
					Game.Renderer.WorldLineRenderer.DrawLine(start, stp, c, c);
					wr.DrawTargetMarker(c, stp);
					start = stp;
				}
			}
		}

	}
}
