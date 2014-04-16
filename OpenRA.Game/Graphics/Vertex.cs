#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Runtime.InteropServices;

namespace OpenRA.Graphics
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vertex
	{
		public float x, y, z, u, v;
		public float p, c;

		public Vertex(float2 xy, float2 uv, float2 pc)
		{
			this.x = xy.X; this.y = xy.Y; this.z = 0;
			this.u = uv.X; this.v = uv.Y;
			this.p = pc.X; this.c = pc.Y;
		}

		public Vertex(float[] xyz, float2 uv, float2 pc)
		{
			this.x = xyz[0]; this.y = xyz[1]; this.z = xyz[2];
			this.u = uv.X; this.v = uv.Y;
			this.p = pc.X; this.c = pc.Y;
		}
	}
}
