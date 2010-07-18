#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Runtime.InteropServices;

namespace OpenRA.FileFormats.Graphics
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
	}
}
