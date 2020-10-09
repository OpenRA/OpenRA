#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Runtime.InteropServices;

namespace OpenRA.Graphics
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vertex
	{
		// 3d position
		public readonly float X, Y, Z;

		// Primary and secondary texture coordinates or RGBA color
		public readonly float S, T, U, V;

		// Palette and channel flags
		public readonly float P, C;

		// Color tint
		public readonly float R, G, B, A;

		public Vertex(in float3 xyz, float s, float t, float u, float v, float p, float c)
			: this(xyz.X, xyz.Y, xyz.Z, s, t, u, v, p, c, float3.Ones, 1f) { }

		public Vertex(in float3 xyz, float s, float t, float u, float v, float p, float c, in float3 tint, float a)
			: this(xyz.X, xyz.Y, xyz.Z, s, t, u, v, p, c, tint.X, tint.Y, tint.Z, a) { }

		public Vertex(float x, float y, float z, float s, float t, float u, float v, float p, float c, in float3 tint, float a)
			: this(x, y, z, s, t, u, v, p, c, tint.X, tint.Y, tint.Z, a) { }

		public Vertex(float x, float y, float z, float s, float t, float u, float v, float p, float c, float r, float g, float b, float a)
		{
			X = x; Y = y; Z = z;
			S = s; T = t;
			U = u; V = v;
			P = p; C = c;
			R = r; G = g; B = b; A = a;
		}
	}
}
