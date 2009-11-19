using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ijw.DirectX;
using System.Runtime.InteropServices;
using System.Drawing;

namespace OpenRa.Game.Graphics
{
	class LineRenderer
	{
		Renderer renderer;
		FvfVertexBuffer<Vertex> vertexBuffer;
		IndexBuffer indexBuffer;            /* kindof a waste of space, but the GPU likes indexing, oh well */

		const int linesPerBatch = 1024;

		Vertex[] vertices = new Vertex[ 2 * linesPerBatch ];
		ushort[] indices = new ushort[ 2 * linesPerBatch ];
		int lines = 0;
		int nv = 0, ni = 0;

		public LineRenderer( Renderer renderer )
		{
			this.renderer = renderer;
			vertexBuffer = new FvfVertexBuffer<Vertex>( renderer.Device, vertices.Length, Vertex.Format );
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
	}
}
