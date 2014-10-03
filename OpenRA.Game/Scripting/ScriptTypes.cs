#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using Eluant;

namespace OpenRA.Scripting
{
	public static class LuaValueExts
	{
		public static Type WrappedClrType(this LuaValue value)
		{
			object inner;
			if (value.TryGetClrObject(out inner))
				return inner.GetType();

			return value.GetType();
		}

		public static bool TryGetClrValue<T>(this LuaValue value, out T clrObject)
		{
			object temp;
			var ret = value.TryGetClrValue(typeof(T), out temp);
			clrObject = ret ? (T)temp : default(T);
			return ret;
		}

		public static bool TryGetClrValue(this LuaValue value, Type t, out object clrObject)
		{
			object temp;

			// Value wraps a CLR object
			if (value.TryGetClrObject(out temp))
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

			if (value is LuaNumber && t.IsAssignableFrom(typeof(double)))
			{
				clrObject = value.ToNumber().Value;
				return true;
			}

			// Need an explicit test for double -> int
			// TODO: Lua 5.3 will introduce an integer type, so this will be able to go away
			if (value is LuaNumber && t.IsAssignableFrom(typeof(int)))
			{
				clrObject = (int)(value.ToNumber().Value);
				return true;
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
					object element;
					if (innerType == typeof(LuaValue))
						element = kv.Value;
					else if (!kv.Value.TryGetClrValue(innerType, out element))
						throw new LuaException("Unable to convert table value of type {0} to type {1}".F(kv.Value.WrappedClrType(), innerType));

					array.SetValue(element, i++);
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
				return (LuaValue)(double)obj;

			if (obj is int)
				return (LuaValue)(int)obj;

			if (obj is bool)
				return (LuaValue)(bool)obj;

			if (obj is string)
				return (LuaValue)(string)obj;

			if (obj is IScriptBindable)
			{
				// Object needs additional notification / context
				var notify = obj as IScriptNotifyBind;
				if (notify != null)
					notify.OnScriptBind(context);

				return new LuaCustomClrObject(obj);
			}

			if (obj is Array)
			{
				var array = obj as Array;
				var i = 1;
				var table = context.CreateTable();
				foreach (var x in array)
					table.Add(i++, x.ToLuaValue(context));

				return table;
			}

			throw new InvalidOperationException("Cannot convert type '{0}' to Lua. Class must implement IScriptBindable.".F(obj.GetType()));
		}
	}
}
