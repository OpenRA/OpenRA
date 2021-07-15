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

namespace OpenRA.Graphics
{
	public class ShaderVertexAttribute
	{
		public readonly string Name;
		public readonly int Index;
		public readonly int Components;
		public readonly int Offset;

		public ShaderVertexAttribute(string name, int index, int components, int offset)
		{
			Name = name;
			Index = index;
			Components = components;
			Offset = offset;
		}
	}
}
