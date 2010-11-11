#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using Tao.Cg;

namespace OpenRA.Renderer.Cg
{
	public class Shader : IShader
	{
		IntPtr effect;
		IntPtr technique;
		GraphicsDevice dev;

		public Shader(GraphicsDevice dev, string name)
		{
			this.dev = dev;
			string code;
			using (var file = new StreamReader(FileSystem.Open("cg{0}{1}.fx".F(Path.DirectorySeparatorChar, name))))
				code = file.ReadToEnd();
			effect = Tao.Cg.Cg.cgCreateEffect(dev.cgContext, code, null);

			if (effect == IntPtr.Zero)
			{
				var err = Tao.Cg.Cg.cgGetErrorString(Tao.Cg.Cg.cgGetError());
				var results = Tao.Cg.Cg.cgGetLastListing(dev.cgContext);
				throw new InvalidOperationException(
					string.Format("Cg compile failed ({0}):\n{1}", err, results));
			}

			technique = Tao.Cg.Cg.cgGetFirstTechnique(effect);
			if (technique == IntPtr.Zero)
				throw new InvalidOperationException("No techniques");
			while (Tao.Cg.Cg.cgValidateTechnique(technique) == 0)
			{
				technique = Tao.Cg.Cg.cgGetNextTechnique(technique);
				if (technique == IntPtr.Zero)
					throw new InvalidOperationException("No valid techniques");
			}
		}

		public void Render(Action a)
		{
			Tao.Cg.CgGl.cgGLEnableProfile(dev.vertexProfile);
			Tao.Cg.CgGl.cgGLEnableProfile(dev.fragmentProfile);

			var pass = Tao.Cg.Cg.cgGetFirstPass(technique);
			while (pass != IntPtr.Zero)
			{
				Tao.Cg.Cg.cgSetPassState(pass);
				a();
				Tao.Cg.Cg.cgResetPassState(pass);
				pass = Tao.Cg.Cg.cgGetNextPass(pass);
			}

			Tao.Cg.CgGl.cgGLDisableProfile(dev.fragmentProfile);
			Tao.Cg.CgGl.cgGLDisableProfile(dev.vertexProfile);
		}

		public void SetValue(string name, ITexture t)
		{
			var texture = (Texture)t;
			var param = Tao.Cg.Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero && texture != null)
				Tao.Cg.CgGl.cgGLSetupSampler(param, texture.texture);
		}

		public void SetValue(string name, float x, float y)
		{
			var param = Tao.Cg.Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero)
				Tao.Cg.CgGl.cgGLSetParameter2f(param, x, y);
		}

		public void Commit() { }
	}
}
