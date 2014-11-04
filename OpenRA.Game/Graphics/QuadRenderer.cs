#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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

		Vertex[] vertices = new Vertex[Renderer.TempBufferSize];
		int nv = 0;

		public QuadRenderer(Renderer renderer, IShader shader)
		{
			this.renderer = renderer;
			this.shader = shader;
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
					renderer.DrawBatch(vb, 0, nv, PrimitiveList.QuadList);
				});
				renderer.Device.SetBlendMode(BlendMode.None);

				nv = 0;
			}
		}

		public void FillRect(RectangleF rect, Color color)
		{
			Renderer.CurrentBatchRenderer = this;

			if (nv + 4 > Renderer.TempBufferSize)
				Flush();

			var r = color.R / 255.0f;
			var g = color.G / 255.0f;
			var b = color.B / 255.0f;
			var a = color.A / 255.0f;
			vertices[nv] = new Vertex(new float2(rect.Left, rect.Top), r, g, b, a);
			vertices[nv + 1] = new Vertex(new float2(rect.Right, rect.Top), r, g, b, a);
			vertices[nv + 2] = new Vertex(new float2(rect.Right, rect.Bottom), r, g, b, a);
			vertices[nv + 3] = new Vertex(new float2(rect.Left, rect.Bottom), r, g, b, a);

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
