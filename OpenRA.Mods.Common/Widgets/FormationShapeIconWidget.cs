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

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class FormationShapeIconWidget : Widget
	{
		const int DotDiameter = 3;
		const int Padding = 2;
		const float LineWidth = 1f;
		static readonly Color IconColor = Color.FromArgb(255, 220, 220, 220);

		public Func<FormationType> GetFormationType;

		public FormationShapeIconWidget()
		{
			GetFormationType = () => FormationType.Default;
		}

		protected FormationShapeIconWidget(FormationShapeIconWidget widget)
			: base(widget)
		{
			GetFormationType = widget.GetFormationType;
		}

		public override FormationShapeIconWidget Clone() { return new FormationShapeIconWidget(this); }

		public override void Draw()
		{
			var rb = RenderBounds;
			var center = new float2(rb.X + rb.Width / 2f, rb.Y + rb.Height / 2f);
			var radius = Math.Min(rb.Width, rb.Height) / 2f - Padding;

			switch (GetFormationType())
			{
				case FormationType.Default:
					DrawBlobDots(center, radius);
					break;
				case FormationType.Square:
					DrawGridDots(center, radius, 3, 3);
					break;
				case FormationType.Circle:
					DrawRingDots(center, radius, 8);
					break;
				case FormationType.LineHorizontal:
					DrawLine(center + new float2(-radius, 0), center + new float2(radius, 0));
					break;
				case FormationType.LineVertical:
					DrawLine(center + new float2(0, -radius), center + new float2(0, radius));
					break;
				case FormationType.Pyramid:
					DrawTriangle(
						center + new float2(0, -radius),
						center + new float2(-radius, radius),
						center + new float2(radius, radius));
					break;
				case FormationType.PyramidInverted:
					DrawTriangle(
						center + new float2(0, radius),
						center + new float2(-radius, -radius),
						center + new float2(radius, -radius));
					break;
				case FormationType.PyramidLeft:
					DrawTriangle(
						center + new float2(-radius, 0),
						center + new float2(radius, -radius),
						center + new float2(radius, radius));
					break;
				case FormationType.PyramidRight:
					DrawTriangle(
						center + new float2(radius, 0),
						center + new float2(-radius, -radius),
						center + new float2(-radius, radius));
					break;
				case FormationType.VFormation:
					DrawChevron(center, radius, 0);
					break;
				case FormationType.VInverted:
					DrawChevron(center, radius, 180);
					break;
				case FormationType.VLeft:
					DrawChevron(center, radius, -90);
					break;
				case FormationType.VRight:
					DrawChevron(center, radius, 90);
					break;
			}
		}

		static void DrawDot(float2 center)
		{
			var half = DotDiameter / 2;
			var x = (int)center.X - half;
			var y = (int)center.Y - half;
			WidgetUtils.FillEllipseWithColor(new Rectangle(x, y, DotDiameter, DotDiameter), IconColor);
		}

		static void DrawLine(float2 a, float2 b)
		{
			Game.Renderer.RgbaColorRenderer.DrawLine(
				new float3(a.X, a.Y, 0),
				new float3(b.X, b.Y, 0),
				LineWidth,
				IconColor);
		}

		static void DrawTriangle(float2 a, float2 b, float2 c)
		{
			DrawLine(a, b);
			DrawLine(b, c);
			DrawLine(c, a);
		}

		static void DrawChevron(float2 center, float radius, float degrees)
		{
			var rad = (float)(degrees * Math.PI / 180);
			var cos = (float)Math.Cos(rad);
			var sin = (float)Math.Sin(rad);

			float2 Rotate(float2 p)
			{
				return new float2(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
			}

			var tip = center + Rotate(new float2(0, -radius));
			var left = center + Rotate(new float2(-radius * 0.75f, radius * 0.55f));
			var right = center + Rotate(new float2(radius * 0.75f, radius * 0.55f));

			DrawLine(left, tip);
			DrawLine(tip, right);
		}

		static void DrawGridDots(float2 center, float radius, int cols, int rows)
		{
			var stepX = cols > 1 ? 2f * radius / (cols - 1) : 0;
			var stepY = rows > 1 ? 2f * radius / (rows - 1) : 0;
			var startX = center.X - radius;
			var startY = center.Y - radius;

			for (var row = 0; row < rows; row++)
			{
				for (var col = 0; col < cols; col++)
					DrawDot(new float2(startX + col * stepX, startY + row * stepY));
			}
		}

		static void DrawRingDots(float2 center, float radius, int count)
		{
			for (var i = 0; i < count; i++)
			{
				var angle = (float)(2 * Math.PI * i / count - Math.PI / 2);
				DrawDot(center + new float2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius));
			}
		}

		static void DrawBlobDots(float2 center, float radius)
		{
			// Loose irregular cluster — distinct from the square grid and circle ring.
			DrawDot(center);
			var offsets = new[]
			{
				new float2(-0.55f, -0.35f),
				new float2(0.50f, -0.45f),
				new float2(-0.45f, 0.40f),
				new float2(0.55f, 0.35f),
				new float2(0.05f, 0.65f),
				new float2(-0.65f, 0.05f),
			};

			foreach (var offset in offsets)
				DrawDot(center + offset * radius);
		}
	}
}
