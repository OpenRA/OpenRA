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
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Renderer.SdlCommon;

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
			using (var file = new StreamReader(GlobalFileSystem.Open("cg{0}{1}.fx".F(Path.DirectorySeparatorChar, name))))
				code = file.ReadToEnd();
			effect = Tao.Cg.Cg.cgCreateEffect(dev.Context, code, null);

			if (effect == IntPtr.Zero)
			{
				var err = Tao.Cg.Cg.cgGetErrorString(Tao.Cg.Cg.cgGetError());
				var results = Tao.Cg.Cg.cgGetLastListing(dev.Context);
				throw new InvalidOperationException(
					"Cg compile failed ({0}):\n{1}".F(err, results));
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
			Tao.Cg.CgGl.cgGLEnableProfile(dev.VertexProfile);
			Tao.Cg.CgGl.cgGLEnableProfile(dev.FragmentProfile);

			var pass = Tao.Cg.Cg.cgGetFirstPass(technique);
			while (pass != IntPtr.Zero)
			{
				Tao.Cg.Cg.cgSetPassState(pass);
				a();
				Tao.Cg.Cg.cgResetPassState(pass);
				pass = Tao.Cg.Cg.cgGetNextPass(pass);
			}

			Tao.Cg.CgGl.cgGLDisableProfile(dev.FragmentProfile);
			Tao.Cg.CgGl.cgGLDisableProfile(dev.VertexProfile);
		}

		public void SetTexture(string name, ITexture t)
		{
			var texture = (Texture)t;
			var param = Tao.Cg.Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero && texture != null)
				Tao.Cg.CgGl.cgGLSetupSampler(param, texture.ID);
		}

		public void SetVec(string name, float x)
		{
			var param = Tao.Cg.Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero)
				Tao.Cg.CgGl.cgGLSetParameter1f(param, x);
		}

		public void SetVec(string name, float x, float y)
		{
			var param = Tao.Cg.Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero)
				Tao.Cg.CgGl.cgGLSetParameter2f(param, x, y);
		}

		public void SetVec(string name, float[] vec, int length)
		{
			var param = Tao.Cg.Cg.cgGetNamedEffectParameter(effect, name);
			if (param == IntPtr.Zero)
				return;

			switch (length)
			{
				case 1: Tao.Cg.CgGl.cgGLSetParameter1fv(param, vec); break;
				case 2: Tao.Cg.CgGl.cgGLSetParameter2fv(param, vec); break;
				case 3: Tao.Cg.CgGl.cgGLSetParameter3fv(param, vec); break;
				case 4: Tao.Cg.CgGl.cgGLSetParameter4fv(param, vec); break;
				default: throw new InvalidDataException("Invalid vector length");
			}
		}

		public void SetMatrix(string name, float[] mtx)
		{
			if (mtx.Length != 16)
				throw new InvalidDataException("Invalid 4x4 matrix");

			var param = Tao.Cg.Cg.cgGetNamedEffectParameter(effect, name);
			if (param != IntPtr.Zero)
				Tao.Cg.CgGl.cgGLSetMatrixParameterfr(param, mtx);
		}
	}
}
