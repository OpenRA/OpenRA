#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Reflection;

namespace OpenRA.Mods.Common.Lint
{
	public class LintExts
	{
		public static string[] GetFieldValues(object ruleInfo, FieldInfo fieldInfo, Action<string> emitError)
		{
			var type = fieldInfo.FieldType;
			if (type == typeof(string))
				return new[] { (string)fieldInfo.GetValue(ruleInfo) };
			if (type == typeof(string[]))
				return (string[])fieldInfo.GetValue(ruleInfo);

			emitError("Bad type for reference on {0}.{1}. Supported types: string, string[]"
				.F(ruleInfo.GetType().Name, fieldInfo.Name));

			return new string[] { };
		}
	}
}
