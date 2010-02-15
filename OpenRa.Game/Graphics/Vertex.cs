using System.Runtime.InteropServices;
using OpenRa.Gl;

namespace OpenRa.Graphics
{
	[StructLayout(LayoutKind.Sequential)]
	struct Vertex
	{
		public float x, y, z, u, v;
		public float p, c;

		public Vertex(float2 xy, float2 uv, float2 pc)
		{
			this.x = xy.X; this.y = xy.Y; this.z = 0;
			this.u = uv.X; this.v = uv.Y;
			this.p = pc.X; this.c = pc.Y;
		}

		public const VertexFormat Format = VertexFormat.Position | VertexFormat.Texture2;
	}
}
