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
			Console.WriteLine("Using Cg renderer");
			return new GraphicsDevice(size, windowMode);
		}
	}

	public class GraphicsDevice : SdlGraphics
	{
		static string[] RequiredExtensions =
		{
			"GL_ARB_vertex_program",
			"GL_ARB_fragment_program",
			"GL_ARB_vertex_buffer_object"
		};

		internal IntPtr cgContext;
		internal int vertexProfile, fragmentProfile;

		static Tao.Cg.Cg.CGerrorCallbackFuncDelegate CgErrorCallback = () =>
		{
			var err = Tao.Cg.Cg.cgGetError();
			var msg = "CG Error: {0}: {1}".F(err, Tao.Cg.Cg.cgGetErrorString(err));
			ErrorHandler.WriteGraphicsLog(msg);
			throw new InvalidOperationException("CG Error. See graphics.log for details");
		};

		public GraphicsDevice(Size size, WindowMode window)
			: base(size, window, RequiredExtensions)
		{
			cgContext = Tao.Cg.Cg.cgCreateContext();

			Tao.Cg.Cg.cgSetErrorCallback(CgErrorCallback);

			Tao.Cg.CgGl.cgGLRegisterStates(cgContext);
			Tao.Cg.CgGl.cgGLSetManageTextureParameters(cgContext, true);
			vertexProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_VERTEX);
			fragmentProfile = CgGl.cgGLGetLatestProfile(CgGl.CG_GL_FRAGMENT);
		}

		public override IShader CreateShader(string name) { return new Shader(this, name); }
	}
}
