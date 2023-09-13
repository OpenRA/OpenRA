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
	public readonly struct ModelVertex
	{
		// 3d position
		public readonly float X, Y, Z;

		// Primary and secondary texture coordinates or RGBA color
		public readonly float S, T, U, V;

		// Palette and channel flags
		public readonly float P, C;

		public ModelVertex(in float3 xyz, float s, float t, float u, float v, float p, float c)
			: this(xyz.X, xyz.Y, xyz.Z, s, t, u, v, p, c) { }

		public ModelVertex(float x, float y, float z, float s, float t, float u, float v, float p, float c)
		{
			X = x; Y = y; Z = z;
			S = s; T = t;
			U = u; V = v;
			P = p; C = c;
		}
	}

	public sealed class ModelShaderBindings : ShaderBindings
	{
		public ModelShaderBindings()
			: base("model")
		{ }

		public override ShaderVertexAttribute[] Attributes { get; }	= new[]
		{
			new ShaderVertexAttribute("aVertexPosition", 3, 0),
			new ShaderVertexAttribute("aVertexTexCoord", 4, 12),
			new ShaderVertexAttribute("aVertexTexMetadata", 2, 28),
		};
	}
}
