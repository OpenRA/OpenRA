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

#nullable enable
using System;
using System.Reflection;

namespace OpenRA
{
	/// <summary>
	/// Provides null-unforgiving equivalents of methods,
	/// throwing <see cref="InvalidOperationException"/> or <see cref="ArgumentException"/> instead of returning null.
	/// </summary>
	public static class Unforgiving
	{
		static T InvalidOperationExOnNull<T>(this T? nullable, string message) where T : class?
		{
			if (nullable is null)
				throw new InvalidOperationException(message);
			return nullable;
		}

		static T ArgumentExOnNull<T>(this T? nullable, string message) where T : class?
		{
			if (nullable is null)
				throw new ArgumentException(message);
			return nullable;
		}

		static T ArgumentExOnNull<T>(this T? nullable, string paramName, string message) where T : class?
		{
			if (nullable is null)
				throw new ArgumentException(message, paramName);
			return nullable;
		}

		/// <inheritdoc cref="Type.GetElementType()"/>
		public static Type GetElementTypeUnforgiving(this Type type)
		{
			return type.GetElementType()
				.InvalidOperationExOnNull($"the current {nameof(Type)} is not an array or a pointer, or is not passed by reference, " +
				$"or represents a generic type or a type parameter in the definition of a generic type of generic method");
		}

		/// <inheritdoc cref="Type.GetConstructor(Type[])"/>
		public static ConstructorInfo GetConstructorUnforgiving(this Type type, Type[] types)
		{
			return type.GetConstructor(types)
				.ArgumentExOnNull(nameof(types), "the public instance constructor whose parameters " +
				"match the types in the parameter type array was not found");
		}

		/// <inheritdoc cref="Type.GetField(string)"/>
		public static FieldInfo GetFieldUnforgiving(this Type type, string name)
		{
			return type.GetField(name)
				.ArgumentExOnNull(nameof(name), "the public field with the specified name was not found");
		}

		/// <inheritdoc cref="PropertyInfo.GetGetMethod(bool)"/>
		public static MethodInfo GetGetMethodUnforgiving(this PropertyInfo propertyInfo, bool nonPublic)
		{
			return propertyInfo.GetGetMethod(nonPublic)
				.InvalidOperationExOnNull($"{nameof(nonPublic)} is false and the get accessor is non-public, " +
				$"or {nameof(nonPublic)} is true but no get accessors exist");
		}

		/// <inheritdoc cref="Type.GetMethod(string)"/>
		public static MethodInfo GetMethodUnforgiving(this Type type, string name)
		{
			return type.GetMethod(name)
				.ArgumentExOnNull(nameof(name), "the public method with the specified name was not found");
		}

		/// <inheritdoc cref="Type.GetMethod(string, Type[])"/>
		public static MethodInfo GetMethodUnforgiving(this Type type, string name, Type[] types)
		{
			return type.GetMethod(name, types)
				.ArgumentExOnNull("the public method whose parameters match the specified argument types was not found");
		}

		/// <inheritdoc cref="Type.GetProperty(string)"/>
		public static PropertyInfo GetPropertyUnforgiving(this Type type, string name)
		{
			return type.GetProperty(name)
				.ArgumentExOnNull(nameof(name), "the public property with the specified name was not found");
		}

		/// <inheritdoc cref="MemberInfo.DeclaringType"/>
		public static Type DeclaringTypeUnforgiving(this MemberInfo memberInfo)
		{
			return memberInfo.DeclaringType
				.InvalidOperationExOnNull($"the {nameof(MemberInfo)} object is a global member " +
				$"(that is, if it was obtained from the {nameof(Module)}.GetMethods method, which returns global methods on a module)");
		}

		/// <inheritdoc cref="System.Activator"/>
		public static class Activator
		{
			/// <inheritdoc cref="System.Activator.CreateInstance(Type)"/>
			public static object CreateInstance(Type type)
			{
				return System.Activator.CreateInstance(type)
					.ArgumentExOnNull(nameof(type), "the method returns null for the Nullable<T> instances with no value");
			}
		}

		/// <inheritdoc cref="System.IO.Path"/>
		public static class Path
		{
			/// <inheritdoc cref="System.IO.Path.GetDirectoryName(string?)"/>
			public static string GetDirectoryName(string? path)
			{
				return System.IO.Path.GetDirectoryName(path)
					.ArgumentExOnNull(nameof(path), $"{nameof(path)} denotes a root directory or is null");
			}
		}
	}
}
