#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class PlatformAttribute : Attribute
	{
		public readonly Type Type;

		public PlatformAttribute(Type graphicsDeviceType)
		{
			if (!typeof(IDeviceFactory).IsAssignableFrom(graphicsDeviceType))
				throw new InvalidOperationException("Incorrect type in RendererAttribute");
			Type = graphicsDeviceType;
		}
	}

	public interface IDeviceFactory
	{
		IGraphicsDevice CreateGraphics(Size size, WindowMode windowMode);
		ISoundEngine CreateSound();
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
		Bitmap TakeScreenshot();
		void PumpInput(IInputHandler inputHandler);
		string GetClipboardText();
		bool SetClipboardText(string text);
		void DrawPrimitives(PrimitiveType type, int firstVertex, int numVertices);

		void EnableScissor(int left, int top, int width, int height);
		void DisableScissor();

		void EnableDepthBuffer();
		void DisableDepthBuffer();
		void ClearDepthBuffer();

		void SetBlendMode(BlendMode mode);

		void GrabWindowMouseFocus();
		void ReleaseWindowMouseFocus();

		IHardwareCursor CreateHardwareCursor(string name, Size size, byte[] data, int2 hotspot);
		void SetHardwareCursor(IHardwareCursor cursor);

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
