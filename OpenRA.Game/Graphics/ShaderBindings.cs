#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Graphics
{
	public abstract class ShaderBindings : IShaderBindings
	{
		public string VertexShaderName { get; }
		public string FragmentShaderName { get; }
		public int Stride => 52;

		public IEnumerable<ShaderVertexAttribute> Attributes { get; } = new[]
		{
			new ShaderVertexAttribute("aVertexPosition", 0, 3, 0),
			new ShaderVertexAttribute("aVertexTexCoord", 1, 4, 12),
			new ShaderVertexAttribute("aVertexTexMetadata", 2, 2, 28),
			new ShaderVertexAttribute("aVertexTint", 3, 4, 36)
		};

		protected ShaderBindings(string name)
		{
			VertexShaderName = name;
			FragmentShaderName = name;
		}

		public void SetRenderData(IShader shader, ModelRenderData renderData)
		{
			foreach (var (name, texture) in renderData.Textures)
				shader.SetTexture(name, texture);
		}
	}

	public class CombinedShaderBindings : ShaderBindings
	{
		public CombinedShaderBindings()
			: base("combined") { }
	}

	public class ModelShaderBindings : ShaderBindings
	{
		public ModelShaderBindings()
			: base("model") { }
	}
}
