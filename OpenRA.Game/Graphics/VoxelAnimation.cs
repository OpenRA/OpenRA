#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Graphics
{
	public struct VoxelAnimation
	{
		public readonly Voxel Voxel;
		public readonly Func<WVec> OffsetFunc;
		public readonly Func<IEnumerable<WRot>> RotationFunc;
		public readonly Func<bool> DisableFunc;
		public readonly Func<uint> FrameFunc;

		public VoxelAnimation(Voxel voxel, Func<WVec> offset, Func<IEnumerable<WRot>> rotation, Func<bool> disable, Func<uint> frame)
		{
			Voxel = voxel;
			OffsetFunc = offset;
			RotationFunc = rotation;
			DisableFunc = disable;
			FrameFunc = frame;
		}
	}
}