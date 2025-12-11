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

using System.Numerics;

namespace OpenRA.Primitives
{
	/// <summary>
	/// Axis-aligned bounding box.
	/// </summary>
	public readonly record struct AABB(float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ)
	{
		/// <summary>
		/// Provides one of eight corners of the <see cref="AABB"/> at the given <paramref name="index"/>.
		/// </summary>
		/// <param name="index">Index into one of the eight possible corners (0 thru 7).</param>
		/// <returns>A vector for this corner with a <see cref="Vector4.W"/> component of 1.</returns>
		public readonly Vector4 Corner(int index) => new(
			(uint)index % 8 < 4 ? MinX : MaxX,
			(uint)index % 4 < 2 ? MinY : MaxY,
			(uint)index % 2 < 1 ? MinZ : MaxZ,
			1);
	}
}
