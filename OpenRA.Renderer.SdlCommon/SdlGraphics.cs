#region Copyright & License Information
/*
* Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
* This file is part of OpenRA, which is free software. It is made
* available to you under the terms of the GNU General Public License
* as published by the Free Software Foundation. For more information,
* see COPYING.
*/
#endregion

using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.FileFormats.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Tao.Sdl;

namespace OpenRA.Renderer.SdlCommon
{
	public abstract class SdlGraphics : IGraphicsDevice
	{
		Size windowSize;
		SdlInput input;

		public Size WindowSize { get { return windowSize; } }

		public SdlGraphics(Size size, WindowMode window, string[] extensions)
		{
			windowSize = size;
			InitializeSdlGl(ref windowSize, window, extensions);

			GL.EnableClientState(ArrayCap.VertexArray);
			ErrorHandler.CheckGlError();
			GL.EnableClientState(ArrayCap.TextureCoordArray);
			ErrorHandler.CheckGlError();

			Sdl.SDL_SetModState(0);

			input = new SdlInput();
		}

		IntPtr InitializeSdlGl(ref Size size, WindowMode window, string[] requiredExtensions)
		{
			Sdl.SDL_Init(Sdl.SDL_INIT_NOPARACHUTE | Sdl.SDL_INIT_VIDEO);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_DOUBLEBUFFER, 1);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_RED_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_GREEN_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_BLUE_SIZE, 8);
			Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_ALPHA_SIZE, 0);

			int windowFlags = 0;
			switch (window)
			{
			case WindowMode.Fullscreen:
				windowFlags |= Sdl.SDL_FULLSCREEN;
				break;
			case WindowMode.PseudoFullscreen:
				windowFlags |= Sdl.SDL_NOFRAME;
				Environment.SetEnvironmentVariable("SDL_VIDEO_WINDOW_POS", "0,0");
				break;
			case WindowMode.Windowed:
				Environment.SetEnvironmentVariable("SDL_VIDEO_CENTERED", "1");
				break;
			default:
				break;
			}

			var info = (Sdl.SDL_VideoInfo)Marshal.PtrToStructure(
				Sdl.SDL_GetVideoInfo(), typeof(Sdl.SDL_VideoInfo));
			Console.WriteLine("Desktop resolution: {0}x{1}",
				info.current_w, info.current_h);

			if (size.Width == 0 && size.Height == 0)
			{
				Console.WriteLine("No custom resolution provided, using desktop resolution");
				size = new Size(info.current_w, info.current_h);
			}

			Console.WriteLine("Using resolution: {0}x{1}", size.Width, size.Height);

			var surf = Sdl.SDL_SetVideoMode(size.Width, size.Height, 0, Sdl.SDL_OPENGL | windowFlags);
			if (surf == IntPtr.Zero)
				Console.WriteLine("Failed to set video mode.");

			Sdl.SDL_WM_SetCaption("OpenRA", "OpenRA");
			Sdl.SDL_ShowCursor(0);
			Sdl.SDL_EnableUNICODE(1);
			Sdl.SDL_EnableKeyRepeat(Sdl.SDL_DEFAULT_REPEAT_DELAY, Sdl.SDL_DEFAULT_REPEAT_INTERVAL);

			ErrorHandler.CheckGlError();

			var extensions = GL.GetString(StringName.Extensions);
			if (extensions == null)
				Console.WriteLine("Failed to fetch GL_EXTENSIONS, this is bad.");

			var missingExtensions = requiredExtensions.Where(r => !extensions.Contains(r)).ToArray();

			if (missingExtensions.Any())
			{
				ErrorHandler.WriteGraphicsLog("Unsupported GPU: Missing extensions: {0}"
					.F(missingExtensions.JoinWith(",")));
				throw new InvalidProgramException("Unsupported GPU. See graphics.log for details.");
			}

			return surf;
		}

		public virtual void Quit()
		{
			Sdl.SDL_Quit();
		}

		OpenTK.Graphics.OpenGL.PrimitiveType ConvertPrimitiveType(OpenRA.FileFormats.Graphics.PrimitiveType pt)
		{
			switch (pt)
			{
				case OpenRA.FileFormats.Graphics.PrimitiveType.PointList: return OpenTK.Graphics.OpenGL.PrimitiveType.Points;
				case OpenRA.FileFormats.Graphics.PrimitiveType.LineList: return OpenTK.Graphics.OpenGL.PrimitiveType.Lines;
				case OpenRA.FileFormats.Graphics.PrimitiveType.TriangleList: return OpenTK.Graphics.OpenGL.PrimitiveType.Triangles;
				case OpenRA.FileFormats.Graphics.PrimitiveType.QuadList: return OpenTK.Graphics.OpenGL.PrimitiveType.Quads;
			}

			throw new NotImplementedException();
		}

		public void DrawPrimitives(OpenRA.FileFormats.Graphics.PrimitiveType pt, int firstVertex, int numVertices)
		{
			GL.DrawArrays(ConvertPrimitiveType(pt), firstVertex, numVertices);
			ErrorHandler.CheckGlError();
		}

		public void Clear()
		{
			GL.ClearColor(0, 0, 0, 0);
			ErrorHandler.CheckGlError();
			GL.Clear(ClearBufferMask.ColorBufferBit);
			ErrorHandler.CheckGlError();
		}

		public void EnableDepthBuffer()
		{
			GL.Clear(ClearBufferMask.DepthBufferBit);
			ErrorHandler.CheckGlError();
			GL.Enable(EnableCap.DepthTest);
			ErrorHandler.CheckGlError();
		}

		public void DisableDepthBuffer()
		{
			GL.Disable(EnableCap.DepthTest);
			ErrorHandler.CheckGlError();
		}

		public void SetBlendMode(BlendMode mode)
		{
			GL.BlendEquation(BlendEquationMode.FuncAdd);
			ErrorHandler.CheckGlError();

			switch (mode)
			{
				case BlendMode.None:
					GL.Disable(EnableCap.Blend);
					break;
				case BlendMode.Alpha:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
					break;
				case BlendMode.Additive:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
					break;
				case BlendMode.Subtractive:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
					ErrorHandler.CheckGlError();
					GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
					break;
				case BlendMode.Multiply:
					GL.Enable(EnableCap.Blend);
					ErrorHandler.CheckGlError();
					GL.BlendFuncSeparate(BlendingFactorSrc.DstColor, BlendingFactorDest.Zero, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcColor);
					ErrorHandler.CheckGlError();
					break;
			}
			ErrorHandler.CheckGlError();
		}

		public void EnableScissor(int left, int top, int width, int height)
		{
			if (width < 0) width = 0;
			if (height < 0) height = 0;

			GL.Scissor(left, windowSize.Height - (top + height), width, height);
			ErrorHandler.CheckGlError();
			GL.Enable(EnableCap.ScissorTest);
			ErrorHandler.CheckGlError();
		}

		public void DisableScissor()
		{
			GL.Disable(EnableCap.ScissorTest);
			ErrorHandler.CheckGlError();
		}

		public void SetLineWidth(float width)
		{
			GL.LineWidth(width);
			ErrorHandler.CheckGlError();
		}

		public void Present() { Sdl.SDL_GL_SwapBuffers(); }
		public void PumpInput(IInputHandler inputHandler) { input.PumpInput(inputHandler); }
		public IVertexBuffer<Vertex> CreateVertexBuffer(int size) { return new VertexBuffer<Vertex>(size); }
		public ITexture CreateTexture() { return new Texture(); }
		public ITexture CreateTexture(Bitmap bitmap) { return new Texture(bitmap); }
		public IFrameBuffer CreateFrameBuffer(Size s) { return new FrameBuffer(s); }
		public abstract IShader CreateShader(string name);
	}
}
