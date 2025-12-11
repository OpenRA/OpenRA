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

using System;

namespace OpenRA
{
	public readonly record struct Int32Matrix4x4(
			int M11, int M12, int M13, int M14,
			int M21, int M22, int M23, int M24,
			int M31, int M32, int M33, int M34,
			int M41, int M42, int M43, int M44) : IEquatable<Int32Matrix4x4>
	{
		public bool Equals(Int32Matrix4x4 other) { return other == this; }

		public override int GetHashCode() { return M11 ^ M22 ^ M33 ^ M44; }

		/// <summary>Returns a string that represents this matrix.</summary>
		public override string ToString()
		{
			return
				"{{ " +
				$"{{M11:{M11} M12:{M12} M13:{M13} M14:{M14}}} " +
				$"{{M21:{M21} M22:{M22} M23:{M23} M24:{M24}}} " +
				$"{{M31:{M31} M32:{M32} M33:{M33} M34:{M34}}} " +
				$"{{M41:{M41} M42:{M42} M43:{M43} M44:{M44}}} " +
				"}}";
		}
	}
}
