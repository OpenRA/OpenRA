#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class RendererAttribute : Attribute
	{
		public readonly Type Type;

		public RendererAttribute(Type graphicsDeviceType)
		{
			if (!typeof(IDeviceFactory).IsAssignableFrom(graphicsDeviceType))
				throw new InvalidOperationException("Incorrect type in RendererAttribute");
			Type = graphicsDeviceType;
		}
	}

	public interface IDeviceFactory
	{
		IGraphicsDevice Create(Size size, WindowMode windowMode);
	}

	public interface IHardwareCursor : IDisposable { }

	public enum BlendMode : byte
	{
		None,
		Alpha,
		Additive,
		Subtractive,
		Multiply,
		SoftAdditive,
		Translucency25,
		Translucency50,
		Translucency75,
		Multiplicative,
		DoubleMultiplicative
	}

	public interface IGraphicsDevice : IDisposable
	{
		IVertexBuffer<Vertex> CreateVertexBuffer(int length);
		ITexture CreateTexture(Bitmap bitmap);
		ITexture CreateTexture();
		IFrameBuffer CreateFrameBuffer(Size s);
		IShader CreateShader(string name);

		Size WindowSize { get; }

		void Clear();
		void Present();
		void PumpInput(IInputHandler inputHandler);
		string GetClipboardText();
		void DrawPrimitives(PrimitiveType type, int firstVertex, int numVertices);

		void SetLineWidth(float width);
		void EnableScissor(int left, int top, int width, int height);
		void DisableScissor();

		void EnableDepthBuffer();
		void DisableDepthBuffer();

		void SetBlendMode(BlendMode mode);

		void GrabWindowMouseFocus();
		void ReleaseWindowMouseFocus();

		IHardwareCursor CreateHardwareCursor(string name, Size size, byte[] data, int2 hotspot);
		void SetHardwareCursor(IHardwareCursor cursor);
	}

	public interface IVertexBuffer<T> : IDisposable
	{
		void Bind();
		void SetData(T[] vertices, int length);
	}

	public interface IShader
	{
		void SetVec(string name, float x);
		void SetVec(string name, float x, float y);
		void SetVec(string name, float[] vec, int length);
		void SetTexture(string param, ITexture texture);
		void SetMatrix(string param, float[] mtx);
		void Render(Action a);
	}

	public enum TextureScaleFilter { Nearest, Linear }

	public interface ITexture : IDisposable
	{
		void SetData(Bitmap bitmap);
		void SetData(uint[,] colors);
		void SetData(byte[] colors, int width, int height);
		byte[] GetData();
		Size Size { get; }
		TextureScaleFilter ScaleFilter { get; set; }
	}

	public interface IFrameBuffer : IDisposable
	{
		void Bind();
		void Unbind();
		ITexture Texture { get; }
	}

	public enum PrimitiveType
	{
		PointList,
		LineList,
		TriangleList,
		QuadList,
	}

	public struct Range<T>
	{
		public readonly T Start, End;
		public Range(T start, T end) { Start = start; End = end; }
	}

	public enum WindowMode
	{
		Windowed,
		Fullscreen,
		PseudoFullscreen,
	}
}
