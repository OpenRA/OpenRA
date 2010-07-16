#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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
