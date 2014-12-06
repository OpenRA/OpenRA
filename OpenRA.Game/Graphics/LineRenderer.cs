#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA.Graphics
{
	public class LineRenderer : Renderer.IBatchRenderer
	{
		static float2 offset = new float2(0.5f, 0.5f);
		float lineWidth = 1f;
		Renderer renderer;
		IShader shader;

		Vertex[] vertices = new Vertex[Renderer.TempBufferSize];
		int nv = 0;

		public LineRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
		}


		public float LineWidth
		{
			get { return lineWidth; }
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
				shader.Render(() =>
				{
					var vb = renderer.GetTempVertexBuffer();
					vb.SetData(vertices, nv);
					renderer.SetLineWidth(LineWidth);
					renderer.DrawBatch(vb, 0, nv, PrimitiveList.LineList);
				});
				renderer.Device.SetBlendMode(BlendMode.None);
				nv = 0;
			}
		}

		public void DrawRect(float2 tl, float2 br, Color c)
		{
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);
			DrawLine(tl, tr, c, c);
			DrawLine(tl, bl, c, c);
			DrawLine(tr, br, c, c);
			DrawLine(bl, br, c, c);
		}

		public void DrawLine(float2 start, float2 end, Color startColor, Color endColor)
		{
			Renderer.CurrentBatchRenderer = this;

			if (nv + 2 > Renderer.TempBufferSize)
				Flush();

			vertices[nv++] = new Vertex(start + offset,
				startColor.R / 255.0f, startColor.G / 255.0f,
				startColor.B / 255.0f, startColor.A / 255.0f);

			vertices[nv++] = new Vertex(end + offset,
				endColor.R / 255.0f, endColor.G / 255.0f,
				endColor.B / 255.0f, endColor.A / 255.0f);
		}

		public void FillRect(RectangleF r, Color color)
		{
			for (var y = r.Top; y < r.Bottom; y++)
				DrawLine(new float2(r.Left, y), new float2(r.Right, y), color, color);
		}

		public void FillEllipse(RectangleF r, Color color)
		{
			var a = (r.Right - r.Left) / 2;
			var b = (r.Bottom - r.Top) / 2;
			var xc = (r.Right + r.Left) / 2;
			var yc = (r.Bottom + r.Top) / 2;
			for (var y = r.Top; y <= r.Bottom; y++)
			{
				var dx = a * (float)(Math.Sqrt(1 - (y - yc) * (y - yc) / b / b));
				DrawLine(new float2(xc - dx, y), new float2(xc + dx, y), color, color);
			}
		}

		public void SetViewportParams(Size screen, float zoom, int2 scroll)
		{
			shader.SetVec("Scroll", scroll.X, scroll.Y);
			shader.SetVec("r1", zoom*2f/screen.Width, -zoom*2f/screen.Height);
			shader.SetVec("r2", -1, 1);
		}
	}
}
