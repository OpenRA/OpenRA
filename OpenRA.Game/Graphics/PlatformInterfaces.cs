#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
{
	public interface IPlatform
	{
		IPlatformWindow CreateWindow(Size size, WindowMode windowMode, int batchSize);
		ISoundEngine CreateSound(string device);
		IFont CreateFont(byte[] data);
	}

	public interface IHardwareCursor : IDisposable { }

	public enum BlendMode : byte
	{
		None,
		Alpha,
		Additive,
		Subtractive,
		Multiply,
		Multiplicative,
		DoubleMultiplicative
	}

	public interface IPlatformWindow : IDisposable
	{
		IGraphicsContext Context { get; }

		Size WindowSize { get; }
		float WindowScale { get; }
		Size SurfaceSize { get; }

		event Action<float, float> OnWindowScaleChanged;

		void PumpInput(IInputHandler inputHandler);
		string GetClipboardText();
		bool SetClipboardText(string text);

		void GrabWindowMouseFocus();
		void ReleaseWindowMouseFocus();

		IHardwareCursor CreateHardwareCursor(string name, Size size, byte[] data, int2 hotspot);
		void SetHardwareCursor(IHardwareCursor cursor);
		void SetRelativeMouseMode(bool mode);
	}

	public interface IGraphicsContext : IDisposable
	{
		IVertexBuffer<Vertex> CreateVertexBuffer(int size);
		ITexture CreateTexture();
		IFrameBuffer CreateFrameBuffer(Size s);
		IFrameBuffer CreateFrameBuffer(Size s, Color clearColor);
		IShader CreateShader(string name);
		void EnableScissor(int x, int y, int width, int height);
		void DisableScissor();
		void Present();
		void DrawPrimitives(PrimitiveType pt, int firstVertex, int numVertices);
		void Clear();
		void EnableDepthBuffer();
		void DisableDepthBuffer();
		void ClearDepthBuffer();
		void SetBlendMode(BlendMode mode);
		string GLVersion { get; }
	}

	public interface IVertexBuffer<T> : IDisposable
	{
		void Bind();
		void SetData(T[] vertices, int length);
		void SetData(T[] vertices, int start, int length);
		void SetData(IntPtr data, int start, int length);
	}

	public interface IShader
	{
		void SetBool(string name, bool value);
		void SetVec(string name, float x);
		void SetVec(string name, float x, float y);
		void SetVec(string name, float x, float y, float z);
		void SetVec(string name, float[] vec, int length);
		void SetTexture(string param, ITexture texture);
		void SetMatrix(string param, float[] mtx);
		void PrepareRender();
	}

	public enum TextureScaleFilter { Nearest, Linear }

	public interface ITexture : IDisposable
	{
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

	public interface IFont : IDisposable
	{
		FontGlyph CreateGlyph(char c, int size, float deviceScale);
	}

	public struct FontGlyph
	{
		public int2 Offset;
		public Size Size;
		public float Advance;
		public byte[] Data;
	}
}
