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
using Tao.Cg;
using Tao.OpenGl;
using Tao.Sdl;

[assembly: Renderer(typeof(OpenRA.Renderer.Cg.DeviceFactory))]

namespace OpenRA.Renderer.Cg
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
		internal IntPtr cgContext;
		internal int vertexProfile, fragmentProfile;

		IntPtr surf;
		SdlInput input;

		public Size WindowSize { get { return windowSize; } }

		static Tao.Cg.Cg.CGerrorCallbackFuncDelegate CgErrorCallback = () =>
		{
			var err = Tao.Cg.Cg.cgGetError();
			var msg = "CG Error: {0}: {1}".F(err, Tao.Cg.Cg.cgGetErrorString(err));
			ErrorHandler.WriteGraphicsLog(msg);
			throw new InvalidOperationException("CG Error. See graphics.log for details");
		};

		public GraphicsDevice(Size size, WindowMode window)
		{
			Console.WriteLine("Using Cg renderer");
			windowSize = size;

			var extensions = new []
			{
				"GL_ARB_vertex_program",
				"GL_ARB_fragment_program",
				"GL_ARB_vertex_buffer_object",
			};

			surf = SdlGraphics.InitializeSdlGl(ref windowSize, window, extensions);

			cgContext = Tao.Cg.Cg.cgCreateContext();

			Tao.Cg.Cg.cgSetErrorCallback(CgErrorCallback);

			Tao.Cg.CgGl.cgGLRegisterStates(cgContext);
			Tao.Cg.CgGl.cgGLSetManageTextureParameters(cgContext, true);
			vertexProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_VERTEX);
			fragmentProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_FRAGMENT);

			Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
			ErrorHandler.CheckGlError();
			Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
			ErrorHandler.CheckGlError();

			Sdl.SDL_SetModState(0);	// i have had enough.

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

		public void SetLineWidth(float width)
		{
			Gl.glLineWidth(width);
			ErrorHandler.CheckGlError();
		}

		public IVertexBuffer<Vertex> CreateVertexBuffer(int size) { return new VertexBuffer<Vertex>(size); }
		public ITexture CreateTexture() { return new Texture(); }
		public ITexture CreateTexture(Bitmap bitmap) { return new Texture(bitmap); }
		public IShader CreateShader(string name) { return new Shader(this, name); }

		public int GpuMemoryUsed { get { return 0; } }
	}
}
