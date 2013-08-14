﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.IO;

namespace OpenRA.FileFormats.Graphics
{
	[AttributeUsage( AttributeTargets.Assembly )]
	public class RendererAttribute : Attribute
	{
		public readonly Type Type;

		public RendererAttribute( Type graphicsDeviceType )
		{
			if( !typeof( IDeviceFactory ).IsAssignableFrom( graphicsDeviceType ) )
				throw new InvalidOperationException( "Incorrect type in RendererAttribute" );
			Type = graphicsDeviceType;
		}
	}

	public interface IDeviceFactory
	{
		IGraphicsDevice Create( Size size, WindowMode windowMode );
	}

	public enum BlendMode { None, Alpha, Additive }

	public interface IGraphicsDevice
	{
		IVertexBuffer<Vertex> CreateVertexBuffer( int length );
		ITexture CreateTexture( Bitmap bitmap );
		ITexture CreateTexture();
		IFrameBuffer CreateFrameBuffer(Size s);
		IShader CreateShader( string name );

		Size WindowSize { get; }

		void Clear();
		void Present();
		void PumpInput(IInputHandler inputHandler);

		void DrawPrimitives( PrimitiveType type, int firstVertex, int numVertices );

		void SetLineWidth( float width );
		void EnableScissor( int left, int top, int width, int height );
		void DisableScissor();

		void EnableDepthBuffer();
		void DisableDepthBuffer();

		void SetBlendMode(BlendMode mode);
	}

	public interface IVertexBuffer<T>
	{
		void Bind();
		void SetData( T[] vertices, int length );
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

	public interface ITexture
	{
		void SetData(Bitmap bitmap);
		void SetData(uint[,] colors);
		void SetData(byte[] colors, int width, int height);
		byte[] GetData();
		Size Size { get; }
	}

	public interface IFrameBuffer
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
		public Range( T start, T end ) { Start = start; End = end; }
	}

	public enum WindowMode
	{
		Windowed,
		Fullscreen,
		PseudoFullscreen,
	}
}
