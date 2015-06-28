#region License
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2010 the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenTK
{
	#region BlittableValueType<T>

	/// <summary>
	/// Checks whether the specified type parameter is a blittable value type.
	/// </summary>
	/// <remarks>
	/// A blittable value type is a struct that only references other value types recursively,
	/// which allows it to be passed to unmanaged code directly.
	/// </remarks>
	public static class BlittableValueType<T>
	{
		#region Fields

		static readonly Type Type;
		static readonly int Stride;

		#endregion

		#region Constructors

		static BlittableValueType()
		{
			Type = typeof(T);
			if (Type.IsValueType && !Type.IsGenericType)
			{
				// Does this support generic types? On Mono 2.4.3 it does
				// On .NET it doesn't.
				// http://msdn.microsoft.com/en-us/library/5s4920fa.aspx
				Stride = Marshal.SizeOf(typeof(T));
			}
		}

		#endregion

		#region Public Members

		/// <summary>
		/// Gets the size of the type in bytes or 0 for non-blittable types.
		/// </summary>
		/// <remarks>
		/// This property returns 0 for non-blittable types.
		/// </remarks>
		public static int GetStride { get { return Stride; } }

		#region Check

		/// <summary>
		/// Checks whether the current typename T is blittable.
		/// </summary>
		/// <returns>True if T is blittable; false otherwise.</returns>
		public static bool Check()
		{
			return Check(Type);
		}

		/// <summary>
		/// Checks whether type is a blittable value type.
		/// </summary>
		/// <param name="type">A System.Type to check.</param>
		/// <returns>True if T is blittable; false otherwise.</returns>
		public static bool Check(Type type)
		{
			if (!CheckStructLayoutAttribute(type))
				Debug.Print("Warning: type {0} does not specify a StructLayoutAttribute with Pack=1. The memory layout of the struct may change between platforms.", type.Name);

			return CheckType(type);
		}

		#endregion

		#endregion

		#region Private Members

		// Checks whether the parameter is a primitive type or consists of primitive types recursively.
		// Throws a NotSupportedException if it is not.
		static bool CheckType(Type type)
		{
			if (type.IsPrimitive)
				return true;

			if (!type.IsValueType)
				return false;

			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			Debug.Indent();
			foreach (FieldInfo field in fields)
			{
				if (!CheckType(field.FieldType))
					return false;
			}

			Debug.Unindent();

			return GetStride != 0;
		}

		// Checks whether the specified struct defines [StructLayout(LayoutKind.Sequential, Pack=1)]
		// or [StructLayout(LayoutKind.Explicit)]
		static bool CheckStructLayoutAttribute(Type type)
		{
			StructLayoutAttribute[] attr = (StructLayoutAttribute[])
				type.GetCustomAttributes(typeof(StructLayoutAttribute), true);

			if ((attr == null) ||
				(attr != null && attr.Length > 0 && attr[0].Value != LayoutKind.Explicit && attr[0].Pack != 1))
				return false;

			return true;
		}

		#endregion
	}

	#endregion

	#region BlittableValueType

	/// <summary>
	/// Checks whether the specified type parameter is a blittable value type.
	/// </summary>
	/// <remarks>
	/// A blittable value type is a struct that only references other value types recursively,
	/// which allows it to be passed to unmanaged code directly.
	/// </remarks>
	public static class BlittableValueType
	{
		#region Check

		/// <summary>
		/// Checks whether type is a blittable value type.
		/// </summary>
		/// <param name="type">An instance of the type to check.</param>
		/// <returns>True if T is blittable; false otherwise.</returns>
		public static bool Check<T>(T type)
		{
			return BlittableValueType<T>.Check();
		}

		/// <summary>
		/// Checks whether type is a blittable value type.
		/// </summary>
		/// <param name="type">An instance of the type to check.</param>
		/// <returns>True if T is blittable; false otherwise.</returns>
		public static bool Check<T>(T[] type)
		{
			return BlittableValueType<T>.Check();
		}

		/// <summary>
		/// Checks whether type is a blittable value type.
		/// </summary>
		/// <param name="type">An instance of the type to check.</param>
		/// <returns>True if T is blittable; false otherwise.</returns>
		public static bool Check<T>(T[,] type)
		{
			return BlittableValueType<T>.Check();
		}

		/// <summary>
		/// Checks whether type is a blittable value type.
		/// </summary>
		/// <param name="type">An instance of the type to check.</param>
		/// <returns>True if T is blittable; false otherwise.</returns>
		public static bool Check<T>(T[,,] type)
		{
			return BlittableValueType<T>.Check();
		}

		/// <summary>
		/// Checks whether type is a blittable value type.
		/// </summary>
		/// <param name="type">An instance of the type to check.</param>
		/// <returns>True if T is blittable; false otherwise.</returns>
		public static bool Check<T>(T[][] type)
		{
			return BlittableValueType<T>.Check();
		}

		#endregion

		#region StrideOf

		/// <summary>
		/// Returns the size of the specified value type in bytes or 0 if the type is not blittable.
		/// </summary>
		/// <typeparam name="T">The value type. Must be blittable.</typeparam>
		/// <param name="type">An instance of the value type.</param>
		/// <returns>An integer, specifying the size of the type in bytes.</returns>
		/// <exception cref="System.ArgumentException">Occurs when type is not blittable.</exception>
		public static int StrideOf<T>(T type)
		{
			if (!Check(type))
				throw new ArgumentException("type");

			return BlittableValueType<T>.GetStride;
		}

		/// <summary>
		/// Returns the size of a single array element in bytes  or 0 if the element is not blittable.
		/// </summary>
		/// <typeparam name="T">The value type.</typeparam>
		/// <param name="type">An instance of the value type.</param>
		/// <returns>An integer, specifying the size of the type in bytes.</returns>
		/// <exception cref="System.ArgumentException">Occurs when type is not blittable.</exception>
		public static int StrideOf<T>(T[] type)
		{
			if (!Check(type))
				throw new ArgumentException("type");

			return BlittableValueType<T>.GetStride;
		}

		/// <summary>
		/// Returns the size of a single array element in bytes or 0 if the element is not blittable.
		/// </summary>
		/// <typeparam name="T">The value type.</typeparam>
		/// <param name="type">An instance of the value type.</param>
		/// <returns>An integer, specifying the size of the type in bytes.</returns>
		/// <exception cref="System.ArgumentException">Occurs when type is not blittable.</exception>
		public static int StrideOf<T>(T[,] type)
		{
			if (!Check(type))
				throw new ArgumentException("type");

			return BlittableValueType<T>.GetStride;
		}

		/// <summary>
		/// Returns the size of a single array element in bytes or 0 if the element is not blittable.
		/// </summary>
		/// <typeparam name="T">The value type.</typeparam>
		/// <param name="type">An instance of the value type.</param>
		/// <returns>An integer, specifying the size of the type in bytes.</returns>
		/// <exception cref="System.ArgumentException">Occurs when type is not blittable.</exception>
		public static int StrideOf<T>(T[,,] type)
		{
			if (!Check(type))
				throw new ArgumentException("type");

			return BlittableValueType<T>.GetStride;
		}

		#endregion
	}

	#endregion
}