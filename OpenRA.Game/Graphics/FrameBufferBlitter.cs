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
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly record struct FrameBufferBlitVertex(float X, float Y, float S, float T);

	public sealed class FrameBufferBlitShaderBindings : ShaderBindings
	{
		public FrameBufferBlitShaderBindings()
			: base("framebuffer_blit") { }

		public override ShaderVertexAttribute[] Attributes { get; } =
		[
			new ShaderVertexAttribute("aVertexPosition", ShaderVertexAttributeType.Float, 2, 0),
			new ShaderVertexAttribute("aVertexTexCoord", ShaderVertexAttributeType.Float, 2, 8),
		];
	}

	/// <summary>
	/// Copies framebuffer textures without paying for the palette, depth, tint, and
	/// multi-sampler features of the general-purpose sprite shader.
	/// </summary>
	public sealed class FrameBufferBlitter : System.IDisposable
	{
		readonly Renderer renderer;
		readonly IShader shader;
		readonly IVertexBuffer<FrameBufferBlitVertex> buffer;
		readonly FrameBufferBlitVertex[] vertices = new FrameBufferBlitVertex[6];

		public FrameBufferBlitter(Renderer renderer)
		{
			this.renderer = renderer;
			shader = renderer.CreateShader(new FrameBufferBlitShaderBindings());
			buffer = renderer.CreateVertexBuffer(vertices, true);
		}

		public void Draw(Sprite sprite, in float2 location, in float2 size, Size targetSize, bool flipY)
		{
			var x0 = 2f * location.X / targetSize.Width - 1f;
			var x1 = 2f * (location.X + size.X) / targetSize.Width - 1f;
			var y0 = 2f * location.Y / targetSize.Height - 1f;
			var y1 = 2f * (location.Y + size.Y) / targetSize.Height - 1f;
			if (flipY)
			{
				y0 = -y0;
				y1 = -y1;
			}

			vertices[0] = new(x0, y0, sprite.Left, sprite.Top);
			vertices[1] = new(x1, y0, sprite.Right, sprite.Top);
			vertices[2] = new(x1, y1, sprite.Right, sprite.Bottom);
			vertices[3] = vertices[2];
			vertices[4] = new(x0, y1, sprite.Left, sprite.Bottom);
			vertices[5] = vertices[0];

			renderer.Flush();
			buffer.SetData(vertices, vertices.Length);
			shader.SetTexture("SourceTexture", sprite.Sheet.GetTexture());
			shader.PrepareRender();
			renderer.Context.SetBlendMode(BlendMode.None);
			renderer.DrawBatch(buffer, shader, 0, vertices.Length, PrimitiveType.TriangleList);
		}

		public void Dispose()
		{
			buffer.Dispose();
		}
	}
}
