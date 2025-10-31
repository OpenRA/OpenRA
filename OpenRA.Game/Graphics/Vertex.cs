#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public readonly struct Vertex
	{
		// 3d position
		public readonly float X, Y, Z;

		// Primary and secondary texture coordinates or RGBA color
		public readonly float S, T, U, V;

		// Palette and channel flags
		public readonly uint C;

		// Color tint
		public readonly float R, G, B, A;

		public Vertex(in float3 xyz, float s, float t, float u, float v, uint c)
			: this(xyz.X, xyz.Y, xyz.Z, s, t, u, v, c, float3.Ones, 1f) { }

		public Vertex(in float3 xyz, float s, float t, float u, float v, uint c, in float3 tint, float a)
			: this(xyz.X, xyz.Y, xyz.Z, s, t, u, v, c, tint.X, tint.Y, tint.Z, a) { }

		public Vertex(float x, float y, float z, float s, float t, float u, float v, uint c, in float3 tint, float a)
			: this(x, y, z, s, t, u, v, c, tint.X, tint.Y, tint.Z, a) { }

		public Vertex(float x, float y, float z, float s, float t, float u, float v, uint c, float r, float g, float b, float a)
		{
			X = x; Y = y; Z = z;
			S = s; T = t;
			U = u; V = v;
			C = c;
			R = r; G = g; B = b; A = a;
		}
	}

	public sealed class CombinedShaderBindings : ShaderBindings
	{
		public CombinedShaderBindings()
			: base("combined")
		{ }

		public override ShaderVertexAttribute[] Attributes { get; } =
		[
			new ShaderVertexAttribute("aVertexPosition", ShaderVertexAttributeType.Float, 3, 0),
			new ShaderVertexAttribute("aVertexTexCoord", ShaderVertexAttributeType.Float, 4, 12),
			new ShaderVertexAttribute("aVertexAttributes", ShaderVertexAttributeType.UInt, 1, 28),
			new ShaderVertexAttribute("aVertexTint", ShaderVertexAttributeType.Float, 4, 32)
		];
	}
}
