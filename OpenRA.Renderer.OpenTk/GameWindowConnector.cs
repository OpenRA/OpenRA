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
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Input;
using OpenRA.Rendering;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

[assembly: Renderer(typeof(OpenRA.Renderer.OpenTk.DeviceFactory))]

namespace OpenRA.Renderer.OpenTk
{
	public class DeviceFactory : IDeviceFactory
	{
		public IGraphicsDevice Create(Size size, WindowMode windowMode)
		{
			Console.WriteLine("Using OpenTK.GameWindow with OpenGL rendering.");
			return new GameWindowConnector(size, windowMode);
		}
	}

	public sealed class GameWindowConnector : IGraphicsDevice
	{
		Size size;
		GameWindow window;
		bool disposed;
		DefaultInput input;

		public Size WindowSize { get { return size; } }

		public GameWindowConnector(Size windowSize, WindowMode windowMode)
		{
			size = windowSize;

			var device = DisplayDevice.GetDisplay(DisplayIndex.Primary);
			Console.WriteLine("Detected display device {0}".F(device));
			foreach (var resolution in device.AvailableResolutions)
				Console.WriteLine("{0}, {1}".F(resolution.Width, resolution.Height));

			Console.WriteLine("Desktop resolution: {0}x{1}", device.Width, device.Height);
			if (size.Width == 0 && size.Height == 0)
			{
				Console.WriteLine("No custom resolution provided, using desktop resolution");
				size = new Size(device.Width, device.Height);
			}

			Console.WriteLine("Using resolution: {0}x{1}", size.Width, size.Height);

			window = new GameWindow(size.Width, size.Height, GraphicsMode.Default, "OpenRA", GameWindowFlags.FixedWindow, device);
			window.Location = new Point(0,0);

			if (windowMode == WindowMode.Fullscreen)
				window.WindowState = WindowState.Fullscreen;
			else if (windowMode == WindowMode.PseudoFullscreen)
				window.WindowBorder = WindowBorder.Hidden;
			else
				window.WindowState = WindowState.Normal;

			window.CursorVisible = false;
			window.MakeCurrent();
			window.Visible = true;

			window.FocusedChanged += (sender, e) =>
			{
				Game.HasInputFocus = window.Focused;
			};

			ErrorHandler.CheckGlVersion();
			ErrorHandler.CheckGlError();

			var framebufferStatus = GL.CheckFramebufferStatus(FramebufferTarget.FramebufferExt);
			if (framebufferStatus == FramebufferErrorCode.FramebufferUnsupportedExt)
			{
				ErrorHandler.WriteGraphicsLog("OpenRA requires the OpenGL extension GL_EXT_framebuffer_object.\n"
					+ "Please try updating your GPU driver to the latest version provided by the manufacturer.");
				throw new InvalidProgramException("Missing OpenGL extension GL_EXT_framebuffer_object. See graphics.log for details.");
			}

			GL.EnableClientState(ArrayCap.VertexArray);
			ErrorHandler.CheckGlError();
			GL.EnableClientState(ArrayCap.TextureCoordArray);
			ErrorHandler.CheckGlError();

			input = new DefaultInput(window);
		}

		public void Dispose()
		{
			if (!disposed)
			{
				window.Exit();
				window.Dispose();
				disposed = true;
			}
		}

		public void DrawPrimitives(PrimitiveList pt, int firstVertex, int numVertices)
		{
			GL.DrawArrays(Util.ModeFromPrimitiveType(pt), firstVertex, numVertices);
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
					GL.BlendFuncSeparate(BlendingFactorSrc.DstColor, BlendingFactorDest.Zero, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
					ErrorHandler.CheckGlError();
					break;
			}

			ErrorHandler.CheckGlError();
		}

		public void GrabWindowMouseFocus() { } // TODO
		public void ReleaseWindowMouseFocus() { } // TODO

		public void EnableScissor(int left, int top, int width, int height)
		{
			if (width < 0)
				width = 0;

			if (height < 0)
				height = 0;

			GL.Scissor(left, size.Height - (top + height), width, height);
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

		public void Present() { window.SwapBuffers(); }

		public string GetClipboardText() { return ""; } // TODO
		public void PumpInput()
		{
			window.ProcessEvents();
			input.PumpInput(window);
		}

		public IVertexBuffer<Vertex> CreateVertexBuffer(int size) { return new VertexBuffer<Vertex>(size); }
		public ITexture CreateTexture() { return new Texture(); }
		public ITexture CreateTexture(Bitmap bitmap) { return new Texture(bitmap); }
		public IFrameBuffer CreateFrameBuffer(Size s) { return new FrameBuffer(s); }
		public IShader CreateShader(string name) { return new Shader(name); }
	}
}
