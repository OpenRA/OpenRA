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
using OpenTK;
using Tao.Cg;

[assembly: Renderer(typeof(OpenRA.Renderer.Cg.DeviceFactory))]

namespace OpenRA.Renderer.Cg
{
	public class DeviceFactory : IDeviceFactory
	{
		public IGraphicsDevice Create(Size size, WindowMode windowMode)
		{
			Console.WriteLine("Using SDL 1.2 with Cg renderer");
			return new GraphicsDevice(size, windowMode);
		}
	}

	public class GraphicsDevice : SdlGraphics
	{
		static string[] requiredExtensions =
		{
			"GL_ARB_vertex_program",
			"GL_ARB_fragment_program",
			"GL_ARB_vertex_buffer_object",
			"GL_EXT_framebuffer_object"
		};

		internal IntPtr Context;
		internal int VertexProfile, FragmentProfile;

		static Tao.Cg.Cg.CGerrorCallbackFuncDelegate errorCallback = () =>
		{
			var err = Tao.Cg.Cg.cgGetError();
			var msg = "Cg Error: {0}: {1}".F(err, Tao.Cg.Cg.cgGetErrorString(err));
			ErrorHandler.WriteGraphicsLog(msg);
			throw new InvalidOperationException("Cg Error. See graphics.log for details");
		};

		public GraphicsDevice(Size size, WindowMode window)
			: base(size, window, requiredExtensions)
		{
			Context = Tao.Cg.Cg.cgCreateContext();

			Tao.Cg.Cg.cgSetErrorCallback(errorCallback);

			CgGl.cgGLRegisterStates(Context);
			CgGl.cgGLSetManageTextureParameters(Context, true);
			VertexProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_VERTEX);
			FragmentProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_FRAGMENT);
		}

		public override void Quit()
		{
			Tao.Cg.Cg.cgDestroyContext(Context);
			base.Quit();
		}

		public override IShader CreateShader(string name) { return new Shader(this, name); }
	}
}
