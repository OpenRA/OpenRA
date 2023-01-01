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
using Eluant;

namespace OpenRA.Scripting
{
	public static class LuaValueExts
	{
		public static Type WrappedClrType(this LuaValue value)
		{
			if (value.TryGetClrObject(out var inner))
				return inner.GetType();

			return value.GetType();
		}

		public static bool TryGetClrValue<T>(this LuaValue value, out T clrObject)
		{
			var ret = value.TryGetClrValue(typeof(T), out var temp);
			clrObject = ret ? (T)temp : default;
			return ret;
		}

		public static bool TryGetClrValue(this LuaValue value, Type t, out object clrObject)
		{
			// Is t a nullable?
			// If yes, get the underlying type
			var nullable = Nullable.GetUnderlyingType(t);
			if (nullable != null)
				t = nullable;

			// Value wraps a CLR object
			if (value.TryGetClrObject(out var temp))
			{
				if (temp.GetType() == t)
				{
					clrObject = temp;
					return true;
				}
			}

			if (value is LuaNil && !t.IsValueType)
			{
				clrObject = null;
				return true;
			}

			if (value is LuaBoolean && t.IsAssignableFrom(typeof(bool)))
			{
				clrObject = value.ToBoolean();
				return true;
			}

			if (value is LuaNumber)
			{
				if (t.IsAssignableFrom(typeof(double)))
				{
					clrObject = value.ToNumber().Value;
					return true;
				}

				// Need an explicit test for double -> int
				// TODO: Lua 5.3 will introduce an integer type, so this will be able to go away
				if (t.IsAssignableFrom(typeof(int)))
				{
					clrObject = (int)value.ToNumber().Value;
					return true;
				}

				if (t.IsAssignableFrom(typeof(short)))
				{
					clrObject = (short)value.ToNumber().Value;
					return true;
				}

				if (t.IsAssignableFrom(typeof(byte)))
				{
					clrObject = (byte)value.ToNumber().Value;
					return true;
				}
			}

			if (value is LuaString && t.IsAssignableFrom(typeof(string)))
			{
				clrObject = value.ToString();
				return true;
			}

			if (value is LuaFunction && t.IsAssignableFrom(typeof(LuaFunction)))
			{
				clrObject = value;
				return true;
			}

			if (value is LuaTable && t.IsAssignableFrom(typeof(LuaTable)))
			{
				clrObject = value;
				return true;
			}

			// Translate LuaTable<int, object> -> object[]
			if (value is LuaTable && t.IsArray)
			{
				var innerType = t.GetElementType();
				var table = (LuaTable)value;
				var array = Array.CreateInstance(innerType, table.Count);
				var i = 0;

				foreach (var kv in table)
				{
					using (kv.Key)
					{
						object element;
						if (innerType == typeof(LuaValue))
							element = kv.Value;
						else
						{
							var elementHasClrValue = kv.Value.TryGetClrValue(innerType, out element);
							if (!elementHasClrValue || !(element is LuaValue))
								kv.Value.Dispose();
							if (!elementHasClrValue)
								throw new LuaException($"Unable to convert table value of type {kv.Value.WrappedClrType()} to type {innerType}");
						}

						array.SetValue(element, i++);
					}
				}

				clrObject = array;
				return true;
			}

			// Value isn't of the requested type.
			// Set a default output value and return false
			// Value types are assumed to specify a default constructor
			clrObject = t.IsValueType ? Activator.CreateInstance(t) : null;
			return false;
		}

		public static LuaValue ToLuaValue(this object obj, ScriptContext context)
		{
			if (obj is LuaValue)
				return (LuaValue)obj;

			if (obj == null)
				return LuaNil.Instance;

			if (obj is double)
				return (double)obj;

			if (obj is int)
				return (int)obj;

			if (obj is bool)
				return (bool)obj;

			if (obj is string)
				return (string)obj;

			if (obj is IScriptBindable)
			{
				// Object needs additional notification / context
				var notify = obj as IScriptNotifyBind;
				notify?.OnScriptBind(context);

				return new LuaCustomClrObject(obj);
			}

			if (obj is Array)
			{
				var array = (Array)obj;
				var i = 1;
				var table = context.CreateTable();

				foreach (var x in array)
					using (LuaValue key = i++, value = x.ToLuaValue(context))
						table.Add(key, value);

				return table;
			}

			throw new InvalidOperationException($"Cannot convert type '{obj.GetType()}' to Lua. Class must implement IScriptBindable.");
		}
	}
}
