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
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	public class LineRenderer : Renderer.IBatchRenderer
	{
		public float LineWidth = 1f;
		static float2 offset = new float2(0.5f,0.5f);

		Renderer renderer;
		IShader shader;

		Vertex[] vertices = new Vertex[ Renderer.TempBufferSize ];
		int nv = 0;

		public LineRenderer( Renderer renderer, IShader shader )
		{
			this.renderer = renderer;
			this.shader = shader;
		}

		public void Flush()
		{
			if( nv > 0 )
			{
				shader.Render( () =>
				{
					var vb = renderer.GetTempVertexBuffer();
					vb.SetData( vertices, nv );
					renderer.SetLineWidth(LineWidth * Game.viewport.Zoom);
					renderer.DrawBatch( vb,	0, nv, PrimitiveType.LineList );
				} );

				nv = 0;
			}
		}

		public void DrawRect( float2 tl, float2 br, Color c )
		{
			var tr = new float2( br.X, tl.Y );
			var bl = new float2( tl.X, br.Y );
			DrawLine( tl, tr, c, c );
			DrawLine( tl, bl, c, c );
			DrawLine( tr, br, c, c );
			DrawLine( bl, br, c, c );
		}

		public void DrawLine( float2 start, float2 end, Color startColor, Color endColor )
		{
			Renderer.CurrentBatchRenderer = this;

			if( nv + 2 > Renderer.TempBufferSize )
				Flush();

			vertices[ nv++ ] = new Vertex( start + offset,
				new float2( startColor.R / 255.0f, startColor.G / 255.0f ),
				new float2( startColor.B / 255.0f, startColor.A / 255.0f ) );

			vertices[ nv++ ] = new Vertex( end + offset,
				new float2( endColor.R / 255.0f, endColor.G / 255.0f ),
				new float2( endColor.B / 255.0f, endColor.A / 255.0f ) );
		}

		public void FillRect( RectangleF r, Color color )
		{
			for (float y = r.Top; y < r.Bottom; y++)
				DrawLine(new float2(r.Left, y), new float2(r.Right, y), color, color);
		}

		public void SetViewportParams(Size screen, float zoom, float2 scroll)
		{
			shader.SetVec("Scroll", (int)scroll.X, (int)scroll.Y);
			shader.SetVec("r1", zoom*2f/screen.Width, -zoom*2f/screen.Height);
			shader.SetVec("r2", -1, 1);
		}
	}
}
