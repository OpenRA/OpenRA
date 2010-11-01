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
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Renderer.Null
{
	public class NullShader : IShader
	{
		public void SetValue(string name, float x, float y) { }
		public void SetValue(string param, ITexture texture) { }
		public void Commit() { }
		public void Render(Action a) { }
	}
}
