using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using BluntDirectX.Direct3D;
using System.Drawing;

namespace OpenRa.Game
{
	[StructLayout(LayoutKind.Sequential)]
	struct Vertex
	{
		public float x, y, z, u, v;
		public float p, c;

		public Vertex(PointF xy, PointF uv, PointF pc)
		{
			this.x = xy.X; this.y = xy.Y; this.z = 0;
			this.u = uv.X; this.v = uv.Y;
			this.p = pc.X; this.c = pc.Y;
		}

		public const VertexFormat Format = VertexFormat.Position | VertexFormat.Texture2;
	}
}
