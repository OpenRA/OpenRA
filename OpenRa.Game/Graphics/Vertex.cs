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

using System.Runtime.InteropServices;
using OpenRa.GlRenderer;

namespace OpenRa.Graphics
{
	[StructLayout(LayoutKind.Sequential)]
	struct Vertex
	{
		public float x, y, z, u, v;
		public float p, c;

		public Vertex(float2 xy, float2 uv, float2 pc)
		{
			this.x = xy.X; this.y = xy.Y; this.z = 0;
			this.u = uv.X; this.v = uv.Y;
			this.p = pc.X; this.c = pc.Y;
		}

		public const VertexFormat Format = VertexFormat.Position | VertexFormat.Texture2;
	}
}
