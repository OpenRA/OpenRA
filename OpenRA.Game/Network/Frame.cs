#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Network
{
	public struct Frame
	{
		public readonly int Index;

		// A frame contains one or multiple serialized orders
		// TODO: replace with Span<byte> once available
		public readonly ArraySegment<byte> Data;

		public Frame(int index, byte[] data)
		{
			Index = index;
			Data = new ArraySegment<byte>(data);
		}

		public Frame(int index, ArraySegment<byte> data)
		{
			Index = index;
			Data = data;
		}
	}
}
