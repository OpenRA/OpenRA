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
	public readonly record struct RenderPostProcessPassVertex(float X, float Y);

	[StructLayout(LayoutKind.Sequential)]
	public readonly record struct RenderPostProcessPassTexturedVertex(float X, float Y, float S, float T);

	public sealed class RenderPostProcessPassShaderBindings : ShaderBindings
	{
		public RenderPostProcessPassShaderBindings(string name)
			: base("postprocess", "postprocess_" + name) { }

		public override ShaderVertexAttribute[] Attributes { get; } =
		[
			new ShaderVertexAttribute("aVertexPosition", ShaderVertexAttributeType.Float, 2, 0)
		];
	}

	public sealed class RenderPostProcessPassTexturedShaderBindings : ShaderBindings
	{
		public RenderPostProcessPassTexturedShaderBindings(string name)
			: base("postprocess_textured", "postprocess_textured_" + name)
		{ }

		public override ShaderVertexAttribute[] Attributes { get; } =
		[
			new ShaderVertexAttribute("aVertexPosition", ShaderVertexAttributeType.Float, 2, 0),
			new ShaderVertexAttribute("aVertexTexCoord", ShaderVertexAttributeType.Float, 2, 8),
		];
	}
}
