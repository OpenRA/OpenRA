#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
		public readonly float X, Y, Z, U, V, P, C;

		public Vertex(float2 xy, float u, float v, float p, float c)
			: this(xy.X, xy.Y, 0, u, v, p, c) { }

		public Vertex(float[] xyz, float u, float v, float p, float c)
			: this(xyz[0], xyz[1], xyz[2], u, v, p, c) { }

		public Vertex(float x, float y, float z, float u, float v, float p, float c)
		{
			X = x; Y = y; Z = z;
			U = u; V = v;
			P = p; C = c;
		}
	}
}
