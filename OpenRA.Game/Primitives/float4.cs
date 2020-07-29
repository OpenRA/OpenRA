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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenRA
{
	[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Mimic a built-in type alias.")]
	[StructLayout(LayoutKind.Sequential)]
	public struct float4 : IEquatable<float4>
	{
		public readonly float X, Y, Z, W;

		public float4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }

		public static bool operator ==(float4 me, float4 other) { return me.X == other.X && me.Y == other.Y && me.Z == other.Z && me.W == other.W; }
		public static bool operator !=(float4 me, float4 other) { return !(me == other); }
		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode(); }

		public bool Equals(float4 other)
		{
			return other == this;
		}

		public override bool Equals(object obj)
		{
			var o = obj as float4?;
			return o != null && o == this;
		}

		public override string ToString() { return "{0},{1},{2},{3}".F(X, Y, Z, W); }
	}
}
