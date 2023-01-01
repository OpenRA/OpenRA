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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenRA.Scripting
{
	public static class ScriptMemberExts
	{
		static readonly Dictionary<string, string> LuaTypeNameReplacements = new Dictionary<string, string>()
		{
			{ "Void", "void" },
			{ "Int32", "int" },
			{ "String", "string" },
			{ "Boolean", "bool" }
		};

		public static string LuaDocString(this Type t)
		{
			if (!LuaTypeNameReplacements.TryGetValue(t.Name, out var ret))
				ret = t.Name;

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
				ret = $"{t.GetGenericArguments()[0].LuaDocString()}?";

			return ret;
		}

		public static string LuaDocString(this ParameterInfo pi)
		{
			var ret = $"{pi.ParameterType.LuaDocString()} {pi.Name}";
			if (pi.IsOptional)
				ret += $" = {pi.DefaultValue ?? "nil"}";

			return ret;
		}

		public static string LuaDocString(this MemberInfo mi)
		{
			var methodInfo = mi as MethodInfo;
			if (methodInfo != null)
			{
				var parameters = methodInfo.GetParameters().Select(pi => pi.LuaDocString());
				return $"{methodInfo.ReturnType.LuaDocString()} {mi.Name}({parameters.JoinWith(", ")})";
			}

			var propertyInfo = mi as PropertyInfo;
			if (propertyInfo != null)
			{
				var types = new List<string>();
				if (propertyInfo.GetGetMethod() != null)
					types.Add("get;");
				if (propertyInfo.GetSetMethod() != null)
					types.Add("set;");

				return $"{propertyInfo.PropertyType.LuaDocString()} {mi.Name} {{ {types.JoinWith(" ")} }}";
			}

			return $"Unknown field: {mi.Name}";
		}
	}
}
