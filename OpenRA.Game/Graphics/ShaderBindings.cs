#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using System.Linq;

namespace OpenRA.Graphics
{
	public readonly struct ShaderVertexAttribute
	{
		public readonly string Name;
		public readonly int Components;
		public readonly int Offset;

		public ShaderVertexAttribute(string name, int components, int offset)
		{
			Name = name;
			Components = components;
			Offset = offset;
		}
	}

	public abstract class ShaderBindings : IShaderBindings
	{
		public string VertexShaderName { get; }
		public string VertexShaderCode { get; }
		public string FragmentShaderName { get; }
		public string FragmentShaderCode { get; }
		public int Stride { get; }

		public abstract ShaderVertexAttribute[] Attributes { get; }

		protected ShaderBindings(string name)
		{
			Stride = Attributes.Sum(a => a.Components * 4);
			VertexShaderName = name;
			VertexShaderCode = GetShaderCode(VertexShaderName + ".vert");
			FragmentShaderName = name;
			FragmentShaderCode = GetShaderCode(FragmentShaderName + ".frag");
		}

		public static string GetShaderCode(string filename)
		{
			var filepath = Path.Combine(Platform.EngineDir, "glsl", filename);
			return File.ReadAllText(filepath);
		}
	}
}
