#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Drawing;
using OpenRa.GlRenderer;

namespace OpenRa.Graphics
{
	class LineRenderer
	{
		Renderer renderer;
		VertexBuffer<Vertex> vertexBuffer;
		IndexBuffer indexBuffer;            /* kindof a waste of space, but the GPU likes indexing, oh well */

		const int linesPerBatch = 1024;

		Vertex[] vertices = new Vertex[ 2 * linesPerBatch ];
		ushort[] indices = new ushort[ 2 * linesPerBatch ];
		int lines = 0;
		int nv = 0, ni = 0;

		public LineRenderer( Renderer renderer )
		{
			this.renderer = renderer;
			vertexBuffer = new VertexBuffer<Vertex>( renderer.Device, vertices.Length, Vertex.Format );
			indexBuffer = new IndexBuffer( renderer.Device, indices.Length );
		}

		public void Flush()
		{
			if( lines > 0 )
			{
				renderer.LineShader.Render( () =>
				{
					vertexBuffer.SetData( vertices );
					indexBuffer.SetData( indices );
					renderer.DrawBatch( vertexBuffer, indexBuffer,
					nv, ni / 2, null, PrimitiveType.LineList );
				} );

				nv = 0; ni = 0;
				lines = 0;
			}
		}

		public void DrawLine( float2 start, float2 end, Color startColor, Color endColor )
		{
			indices[ ni++ ] = (ushort)nv;

			vertices[ nv++ ] = new Vertex( start,
			new float2( startColor.R / 255.0f, startColor.G / 255.0f ),
			new float2( startColor.B / 255.0f, startColor.A / 255.0f ) );

			indices[ ni++ ] = (ushort)nv;

			vertices[ nv++ ] = new Vertex( end,
			new float2( endColor.R / 255.0f, endColor.G / 255.0f ),
			new float2( endColor.B / 255.0f, endColor.A / 255.0f ) );

			if( ++lines >= linesPerBatch )
				Flush();
		}
		
		public void FillRect( RectangleF r, Color color )
		{
			for (float y = r.Top; y < r.Bottom; y++)
			{
				DrawLine(new float2(r.Left, y), new float2(r.Right, y), color, color);
			}
		}
	}
}
