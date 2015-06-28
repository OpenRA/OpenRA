#region --- License ---
/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTK
{
	/// <summary>Represents a handle to an OpenGL or OpenAL context.</summary>
	public struct ContextHandle : IComparable<ContextHandle>, IEquatable<ContextHandle>
	{
		#region Fields

		IntPtr handle;

		/// <summary>Gets a System.IntPtr that represents the handle of this ContextHandle.</summary>
		public IntPtr Handle { get { return handle; } }

		/// <summary>A read-only field that represents a handle that has been initialized to zero.</summary>
		public static readonly ContextHandle Zero = new ContextHandle(IntPtr.Zero);

		#endregion

		#region Constructors

		public ContextHandle(IntPtr h) { handle = h; }

		#endregion

		#region Public Members

		#region ToString

		public override string ToString()
		{
			return Handle.ToString();
		}

		#endregion

		#region Equals

		public override bool Equals(object obj)
		{
			if (obj is ContextHandle)
				return this.Equals((ContextHandle)obj);
			return false;
		}

		#endregion

		#region GetHashCode

		public override int GetHashCode()
		{
			return Handle.GetHashCode();
		}

		#endregion

		#region public static explicit operator IntPtr(ContextHandle c)

		public static explicit operator IntPtr(ContextHandle c)
		{
			return c != ContextHandle.Zero ? c.handle : IntPtr.Zero;
		}

		#endregion

		#region public static explicit operator ContextHandle(IntPtr p)

		public static explicit operator ContextHandle(IntPtr p)
		{
			return new ContextHandle(p);
		}

		#endregion

		#region public static bool operator ==(ContextHandle left, ContextHandle right)

		public static bool operator ==(ContextHandle left, ContextHandle right)
		{
			return left.Equals(right);
		}

		#endregion

		#region public static bool operator !=(ContextHandle left, ContextHandle right)

		public static bool operator !=(ContextHandle left, ContextHandle right)
		{
			return !left.Equals(right);
		}

		#endregion

		#endregion

		#region IComparable<ContextHandle> Members

		public int CompareTo(ContextHandle other)
		{
			unsafe { return (int)((int*)other.handle.ToPointer() - (int*)this.handle.ToPointer()); }
		}

		#endregion

		#region IEquatable<ContextHandle> Members

		public bool Equals(ContextHandle other)
		{
			return Handle == other.Handle;
		}

		#endregion
	}
}
