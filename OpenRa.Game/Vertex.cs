using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	[StructLayout(LayoutKind.Sequential)]
	struct Vertex
	{
		public float x, y, z, u, v;

		public Vertex(float x, float y, float z, float u, float v)
		{
			this.x = x; this.y = y; this.z = z;
			this.u = u;
			this.v = v;
		}

		public const VertexFormat Format = VertexFormat.Position | VertexFormat.Texture;
	}
}
