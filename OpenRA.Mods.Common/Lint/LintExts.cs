#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenRA.Support;

namespace OpenRA.Mods.Common.Lint
{
	public class LintExts
	{
		static IEnumerable<string> GetValues(object value, Type type, object parent, string name, Action<string> emitError, bool preferVariableExpressions = false)
		{
			if (value == null)
				return Enumerable.Empty<string>();

			if (type == typeof(string))
				return new[] { (string)value };

			var expression = value as VariableExpression;
			if (expression != null)
				return expression.Variables;

			if (value is IEnumerable)
			{
				var strings = value as IEnumerable<string>;
				if (strings != null)
					return strings;

				var expressions = value as IEnumerable<VariableExpression>;
				if (expressions != null)
					return expressions.SelectMany(expr => expr.Variables).Distinct();

				if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				{
					var useKeys = preferVariableExpressions
						? typeof(VariableExpression).IsAssignableFrom(type.GetGenericArguments()[0])
						: typeof(VariableExpression).IsAssignableFrom(type.GetGenericArguments()[1]);
					value = useKeys ? type.GetProperty("Keys").GetValue(value) : type.GetProperty("Values").GetValue(value);
					return GetValues(value, value.GetType(), parent, name, emitError, preferVariableExpressions);
				}

				var values = value as IEnumerable<object>;
				if (values != null)
				{
					var first = values.FirstOrDefault();
					type = first == null ? null : first.GetType();
					if (type == null)
						return Enumerable.Empty<string>();

					return values.SelectMany(item => GetValues(item, type, parent, name, emitError, preferVariableExpressions)).Distinct();
				}
			}

			throw new InvalidOperationException("Bad type for reference on " + parent.GetType().Name + "." + name
				+ ". Supported types: string, BooleanExpression, IntegerExpression, IEnumerable<(supported)>, Dictionary<?, (supported)>, Dictionary<(supported), ?>");
		}

		public static IEnumerable<string> GetFieldValues(object ruleInfo, FieldInfo fieldInfo, Action<string> emitError, bool preferVariableExpressions = false)
		{
			return GetValues(fieldInfo.GetValue(ruleInfo), fieldInfo.FieldType, ruleInfo, fieldInfo.Name, emitError, preferVariableExpressions);
		}

		public static IEnumerable<string> GetPropertyValues(object ruleInfo, PropertyInfo propertyInfo, Action<string> emitError, bool preferVariableExpressions = false)
		{
			return GetValues(propertyInfo.GetValue(ruleInfo), propertyInfo.PropertyType, ruleInfo, propertyInfo.Name, emitError, preferVariableExpressions);
		}
	}
}
