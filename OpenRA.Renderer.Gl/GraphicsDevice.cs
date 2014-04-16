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
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Renderer.SdlCommon;

[assembly: Renderer(typeof(OpenRA.Renderer.Glsl.DeviceFactory))]

namespace OpenRA.Renderer.Glsl
{
	public class DeviceFactory : IDeviceFactory
	{
		public IGraphicsDevice Create(Size size, WindowMode windowMode)
		{
			Console.WriteLine("Using SDL 1.2 with OpenGL renderer");
			return new GraphicsDevice(size, windowMode);
		}
	}

	public class GraphicsDevice : SdlGraphics
	{
		static string[] requiredExtensions =
		{
			"GL_ARB_vertex_shader",
			"GL_ARB_fragment_shader",
			"GL_ARB_vertex_buffer_object",
			"GL_EXT_framebuffer_object"
		};

		public GraphicsDevice(Size size, WindowMode window)
			: base(size, window, requiredExtensions) { }

		public override IShader CreateShader(string name) { return new Shader(name); }
	}
}
