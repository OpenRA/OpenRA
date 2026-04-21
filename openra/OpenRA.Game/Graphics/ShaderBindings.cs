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
	public enum ShaderVertexAttributeType
	{
		// Assign the underlying OpenGL type values
		// to simplify enum use in the shader
		Float = 0x1406, // GL_FLOAT
		Int = 0x1404, // GL_INT
		UInt = 0x1405 // GL_UNSIGNED_INT
	}

	public readonly record struct ShaderVertexAttribute(string Name, ShaderVertexAttributeType Type, int Components, int Offset);

	public abstract class ShaderBindings : IShaderBindings
	{
		public string VertexShaderName { get; }
		public string VertexShaderCode { get; }
		public string FragmentShaderName { get; }
		public string FragmentShaderCode { get; }
		public int Stride { get; }

		public abstract ShaderVertexAttribute[] Attributes { get; }

		protected ShaderBindings(string name)
			: this(name, name) { }

		protected ShaderBindings(string vertexName, string fragmentName)
		{
			Stride = Attributes.Sum(a => a.Components * 4);
			VertexShaderName = vertexName;
			VertexShaderCode = GetShaderCode(VertexShaderName + ".vert");
			FragmentShaderName = fragmentName;
			FragmentShaderCode = GetShaderCode(FragmentShaderName + ".frag");
		}

		public static string GetShaderCode(string filename)
		{
			var filepath = Path.Combine(Platform.EngineDir, "glsl", filename);
			return File.ReadAllText(filepath);
		}
	}
}
