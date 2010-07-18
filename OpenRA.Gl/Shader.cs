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
using OpenRA.FileFormats.Graphics;
using Tao.Cg;

namespace OpenRA.GlRenderer
{
	public class Shader : IShader
	{
		IntPtr effect;
		IntPtr technique;
		GraphicsDevice dev;

		public Shader(GraphicsDevice dev, Stream s)
		{
			this.dev = dev;
			string code;
			using (var file = new StreamReader(s))
				code = file.ReadToEnd();
			effect = Cg.cgCreateEffect(dev.cgContext, code, null);

			if (effect == IntPtr.Zero)
			{
				var err = Cg.cgGetErrorString(Cg.cgGetError());
				var results = Cg.cgGetLastListing(dev.cgContext);
				throw new InvalidOperationException(
					string.Format("Cg compile failed ({0}):\n{1}", err, results));
			}

			technique = Cg.cgGetFirstTechnique(effect);
			if (technique == IntPtr.Zero)
				throw new InvalidOperationException("No techniques");
			while (Cg.cgValidateTechnique(technique) == 0)
			{
				technique = Cg.cgGetNextTechnique(technique);
				if (technique == IntPtr.Zero)
					throw new InvalidOperationException("No valid techniques");
			}
		}

		public void Render(Action a)
		{
			CgGl.cgGLEnableProfile(dev.vertexProfile);
			CgGl.cgGLEnableProfile(dev.fragmentProfile);

			var pass = Cg.cgGetFirstPass(technique);
			while (pass != IntPtr.Zero)
			{
				Cg.cgSetPassState(pass);
				a();
				Cg.cgResetPassState(pass);
				pass = Cg.cgGetNextPass(pass);
			}

			CgGl.cgGLDisableProfile(dev.fragmentProfile);
			CgGl.cgGLDisableProfile(dev.vertexProfile);
		}

		public void SetValue(string name, ITexture t)
		{
			var texture = (Texture)t;
			var param = Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero && texture != null)
				CgGl.cgGLSetupSampler(param, texture.texture);
		}

		public void SetValue(string name, float x, float y)
		{
			var param = Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero)
				CgGl.cgGLSetParameter2f(param, x, y);
		}

		public void Commit() { }
	}
}
