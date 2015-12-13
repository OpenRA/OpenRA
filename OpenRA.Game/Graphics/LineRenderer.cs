#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenRA.Graphics
{
	public class LineRenderer : Renderer.IBatchRenderer
	{
		static readonly float2 Offset = new float2(0.5f, 0.5f);

		readonly Renderer renderer;
		readonly IShader shader;
		readonly Action renderAction;

		readonly Vertex[] vertices;
		int nv = 0;

		float lineWidth = 1f;

		public LineRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
			vertices = new Vertex[renderer.TempBufferSize];
			renderAction = () =>
			{
				renderer.SetLineWidth(LineWidth);
				renderer.DrawBatch(vertices, nv, PrimitiveType.LineList);
			};
		}

		public float LineWidth
		{
			get
			{
				return lineWidth;
			}

			set
			{
				if (LineWidth != value)
					Flush();

				lineWidth = value;
			}
		}

		public void Flush()
		{
			if (nv > 0)
			{
				renderer.Device.SetBlendMode(BlendMode.Alpha);
				shader.Render(renderAction);
				renderer.Device.SetBlendMode(BlendMode.None);

				nv = 0;
			}
		}

		public void DrawRect(float2 tl, float2 br, Color c)
		{
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);
			DrawLine(tl, tr, c);
			DrawLine(tl, bl, c);
			DrawLine(tr, br, c);
			DrawLine(bl, br, c);
		}

		public void DrawLine(float2 start, float2 end, Color color)
		{
			renderer.CurrentBatchRenderer = this;

			if (nv + 2 > renderer.TempBufferSize)
				Flush();

			color = Util.PremultiplyAlpha(color);
			var r = color.R / 255.0f;
			var g = color.G / 255.0f;
			var b = color.B / 255.0f;
			var a = color.A / 255.0f;
			vertices[nv++] = new Vertex(start + Offset, r, g, b, a);
			vertices[nv++] = new Vertex(end + Offset, r, g, b, a);
		}

		public void DrawLine(float2 start, float2 end, Color startColor, Color endColor)
		{
			renderer.CurrentBatchRenderer = this;

			if (nv + 2 > renderer.TempBufferSize)
				Flush();

			startColor = Util.PremultiplyAlpha(startColor);
			var r = startColor.R / 255.0f;
			var g = startColor.G / 255.0f;
			var b = startColor.B / 255.0f;
			var a = startColor.A / 255.0f;
			vertices[nv++] = new Vertex(start + Offset, r, g, b, a);

			endColor = Util.PremultiplyAlpha(endColor);
			r = endColor.R / 255.0f;
			g = endColor.G / 255.0f;
			b = endColor.B / 255.0f;
			a = endColor.A / 255.0f;
			vertices[nv++] = new Vertex(end + Offset, r, g, b, a);
		}

		public void DrawLineStrip(IEnumerable<float2> points, Color color)
		{
			renderer.CurrentBatchRenderer = this;

			color = Util.PremultiplyAlpha(color);
			var r = color.R / 255.0f;
			var g = color.G / 255.0f;
			var b = color.B / 255.0f;
			var a = color.A / 255.0f;

			var first = true;
			var prev = new Vertex();
			foreach (var point in points)
			{
				if (first)
				{
					first = false;
					prev = new Vertex(point + Offset, r, g, b, a);
					continue;
				}

				if (nv + 2 > renderer.TempBufferSize)
					Flush();

				vertices[nv++] = prev;
				prev = new Vertex(point + Offset, r, g, b, a);
				vertices[nv++] = prev;
			}
		}

		public void SetViewportParams(Size screen, float zoom, int2 scroll)
		{
			shader.SetVec("Scroll", scroll.X, scroll.Y);
			shader.SetVec("r1", zoom * 2f / screen.Width, -zoom * 2f / screen.Height);
			shader.SetVec("r2", -1, 1);
		}
	}
}
