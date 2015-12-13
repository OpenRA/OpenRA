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
	public class RgbaColorRenderer : Renderer.IBatchRenderer
	{
		static readonly float2 Offset = new float2(0.5f, 0.5f);

		readonly Renderer renderer;
		readonly IShader shader;
		readonly Action renderAction;

		readonly Vertex[] vertices;
		int nv = 0;

		public RgbaColorRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
			vertices = new Vertex[renderer.TempBufferSize];
			renderAction = () => renderer.DrawBatch(vertices, nv, PrimitiveType.QuadList);
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

		public void DrawLine(float2 start, float2 end, float width, Color color)
		{
			renderer.CurrentBatchRenderer = this;

			if (nv + 4 > renderer.TempBufferSize)
				Flush();

			var delta = (end - start) / (end - start).Length;
			var corner = width / 2 * new float2(-delta.Y, delta.X);

			color = Util.PremultiplyAlpha(color);
			var r = color.R / 255.0f;
			var g = color.G / 255.0f;
			var b = color.B / 255.0f;
			var a = color.A / 255.0f;

			vertices[nv++] = new Vertex(start - corner + Offset, r, g, b, a);
			vertices[nv++] = new Vertex(start + corner + Offset, r, g, b, a);
			vertices[nv++] = new Vertex(end + corner + Offset, r, g, b, a);
			vertices[nv++] = new Vertex(end - corner + Offset, r, g, b, a);
		}

		public void FillRect(float2 tl, float2 br, Color color)
		{
			renderer.CurrentBatchRenderer = this;

			if (nv + 4 > renderer.TempBufferSize)
				Flush();

			color = Util.PremultiplyAlpha(color);
			var r = color.R / 255.0f;
			var g = color.G / 255.0f;
			var b = color.B / 255.0f;
			var a = color.A / 255.0f;

			vertices[nv++] = new Vertex(new float2(tl.X, tl.Y) + Offset, r, g, b, a);
			vertices[nv++] = new Vertex(new float2(br.X, tl.Y) + Offset, r, g, b, a);
			vertices[nv++] = new Vertex(new float2(br.X, br.Y) + Offset, r, g, b, a);
			vertices[nv++] = new Vertex(new float2(tl.X, br.Y) + Offset, r, g, b, a);
		}

		public void FillRect(float2 a, float2 b, float2 c, float2 d, Color color)
		{
			renderer.CurrentBatchRenderer = this;

			if (nv + 4 > renderer.TempBufferSize)
				Flush();

			color = Util.PremultiplyAlpha(color);
			var cr = color.R / 255.0f;
			var cg = color.G / 255.0f;
			var cb = color.B / 255.0f;
			var ca = color.A / 255.0f;

			vertices[nv++] = new Vertex(a + Offset, cr, cg, cb, ca);
			vertices[nv++] = new Vertex(b + Offset, cr, cg, cb, ca);
			vertices[nv++] = new Vertex(c + Offset, cr, cg, cb, ca);
			vertices[nv++] = new Vertex(d + Offset, cr, cg, cb, ca);
		}

		public void FillEllipse(RectangleF r, Color color, int vertices = 32)
		{
			// TODO: Create an ellipse polygon instead
			var a = (r.Right - r.Left) / 2;
			var b = (r.Bottom - r.Top) / 2;
			var xc = (r.Right + r.Left) / 2;
			var yc = (r.Bottom + r.Top) / 2;
			for (var y = r.Top; y <= r.Bottom; y++)
			{
				var dx = a * (float)Math.Sqrt(1 - (y - yc) * (y - yc) / b / b);
				DrawLine(new float2(xc - dx, y), new float2(xc + dx, y), 1, color);
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
