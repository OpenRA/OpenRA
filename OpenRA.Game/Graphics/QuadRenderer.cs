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

namespace OpenRA.Graphics
{
	public class QuadRenderer : Renderer.IBatchRenderer
	{
		Renderer renderer;
		IShader shader;

		readonly Vertex[] vertices;
		int nv = 0;

		public QuadRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
			vertices = new Vertex[renderer.TempBufferSize];
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
					renderer.DrawBatch(vb, 0, nv, PrimitiveType.QuadList);
				});
				renderer.Device.SetBlendMode(BlendMode.None);

				nv = 0;
			}
		}

		public void FillRect(RectangleF r, Color color)
		{
			renderer.CurrentBatchRenderer = this;

			if (nv + 4 > renderer.TempBufferSize)
				Flush();

			vertices[nv] = new Vertex(new float2(r.Left, r.Top), new float2(color.R / 255.0f, color.G / 255.0f), new float2(color.B / 255.0f, color.A / 255.0f));
			vertices[nv + 1] = new Vertex(new float2(r.Right, r.Top), new float2(color.R / 255.0f, color.G / 255.0f), new float2(color.B / 255.0f, color.A / 255.0f));
			vertices[nv + 2] = new Vertex(new float2(r.Right, r.Bottom), new float2(color.R / 255.0f, color.G / 255.0f), new float2(color.B / 255.0f, color.A / 255.0f));
			vertices[nv + 3] = new Vertex(new float2(r.Left, r.Bottom), new float2(color.R / 255.0f, color.G / 255.0f), new float2(color.B / 255.0f, color.A / 255.0f));

			nv += 4;
		}

		public void SetViewportParams(Size screen, float zoom, int2 scroll)
		{
			shader.SetVec("Scroll", scroll.X, scroll.Y);
			shader.SetVec("r1", zoom*2f/screen.Width, -zoom*2f/screen.Height);
			shader.SetVec("r2", -1, 1);
		}
	}
}
