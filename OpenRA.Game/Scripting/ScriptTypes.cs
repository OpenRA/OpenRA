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
	
			throw new InvalidOperationException("Cannot convert type '{0}' to Lua. Class must implement IScriptBindable.".F(obj.GetType()));
		}

		public static LuaTable ToLuaTable(this IEnumerable collection, ScriptContext context)
		{
			var i = 1;
			var table = context.CreateTable();
			foreach (var x in collection)
				table.Add(i++, x.ToLuaValue(context));
			return table;
		}
	}
}
