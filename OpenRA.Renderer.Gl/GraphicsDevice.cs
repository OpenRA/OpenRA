#region Copyright & License Information
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
using OpenRA.FileFormats.Graphics;
using OpenRA.Renderer.SdlCommon;
using Tao.OpenGl;
using Tao.Sdl;

[assembly: Renderer(typeof(OpenRA.Renderer.Glsl.DeviceFactory))]

namespace OpenRA.Renderer.Glsl
{
	public class DeviceFactory : IDeviceFactory
	{
		public IGraphicsDevice Create(Size size, WindowMode windowMode)
		{
			return new GraphicsDevice(size, windowMode);
		}
	}

	public class GraphicsDevice : IGraphicsDevice
	{
		Size windowSize;
		IntPtr surf;
		SdlInput input;

		public Size WindowSize { get { return windowSize; } }

		public GraphicsDevice(Size size, WindowMode window)
		{
			Console.WriteLine("Using Gl renderer");
			windowSize = size;

			var extensions = new []
			{
				"GL_ARB_vertex_shader",
				"GL_ARB_fragment_shader",
				"GL_ARB_vertex_buffer_object",
			};

			surf = SdlGraphics.InitializeSdlGl(ref windowSize, window, extensions);

			Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
			ErrorHandler.CheckGlError();
			Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
			ErrorHandler.CheckGlError();

			Sdl.SDL_SetModState(0);

			input = new SdlInput(surf);
		}

		public void EnableScissor(int left, int top, int width, int height)
		{
			if (width < 0) width = 0;
			if (height < 0) height = 0;

			Gl.glScissor(left, windowSize.Height - ( top + height ), width, height);
			ErrorHandler.CheckGlError();
			Gl.glEnable(Gl.GL_SCISSOR_TEST);
			ErrorHandler.CheckGlError();
		}

		public void DisableScissor()
		{
			Gl.glDisable(Gl.GL_SCISSOR_TEST);
			ErrorHandler.CheckGlError();
		}

		public void Clear() { SdlGraphics.Clear(); }
		public void Present() { Sdl.SDL_GL_SwapBuffers(); }
		public void PumpInput(IInputHandler inputHandler) { input.PumpInput(inputHandler); }

		public void DrawPrimitives(PrimitiveType pt, int firstVertex, int numVertices)
		{
			SdlGraphics.DrawPrimitives(pt, firstVertex, numVertices);
		}

		public void SetLineWidth( float width )
		{
			Gl.glLineWidth(width);
			ErrorHandler.CheckGlError();
		}

		public IVertexBuffer<Vertex> CreateVertexBuffer( int size ) { return new VertexBuffer<Vertex>( size ); }
		public ITexture CreateTexture() { return new Texture(); }
		public ITexture CreateTexture( Bitmap bitmap ) { return new Texture( bitmap ); }
		public IShader CreateShader( string name ) { return new Shader( this, name ); }

		public int GpuMemoryUsed { get; internal set; }
	}
}
