#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using Eluant;
using Eluant.ObjectBinding;

namespace OpenRA.Scripting
{
	public abstract class ScriptObjectWrapper : IScriptBindable, ILuaTableBinding
	{
		protected abstract string DuplicateKeyError(string memberName);
		protected abstract string MemberNotFoundError(string memberName);

		protected readonly ScriptContext Context;
		readonly Dictionary<string, ScriptMemberWrapper> members = new Dictionary<string, ScriptMemberWrapper>();

#if !NET5_0_OR_GREATER
		readonly List<string> membersToRemove = new List<string>();
#endif

		public ScriptObjectWrapper(ScriptContext context)
		{
			Context = context;
		}

		protected static object[] CreateObjects(Type[] types, object[] constructorArgs)
		{
			var i = 0;
			var argTypes = new Type[constructorArgs.Length];
			foreach (var ca in constructorArgs)
				argTypes[i++] = ca.GetType();

			var objects = new object[types.Length];
			i = 0;
			foreach (var type in types)
				objects[i++] = type.GetConstructor(argTypes).Invoke(constructorArgs);

			return objects;
		}

		protected void Bind(object[] clrObjects)
		{
			members.Clear();

			foreach (var obj in clrObjects)
			{
				var wrappable = ScriptMemberWrapper.WrappableMembers(obj.GetType());
				foreach (var m in wrappable)
				{
					if (members.ContainsKey(m.Name))
						throw new LuaException(DuplicateKeyError(m.Name));

					members.Add(m.Name, new ScriptMemberWrapper(Context, obj, m));
				}
			}
		}

		protected void Unbind(Type targetType)
		{
#if NET5_0_OR_GREATER
			// NOTE: In newer versions of .NET modifying the collection by calling Remove while iterating over it is valid
			foreach (var m in members)
				if (targetType == m.Value.Target.GetType())
					members.Remove(m.Key);
#else
			// PERF: Re-use instead of allocating a new list on each unbind
			membersToRemove.Clear();

			foreach (var m in members)
				if (targetType == m.Value.Target.GetType())
					membersToRemove.Add(m.Key);

			foreach (var m in membersToRemove)
				members.Remove(m);
#endif
		}

		public bool ContainsKey(string key) { return members.ContainsKey(key); }

		public LuaValue this[LuaRuntime runtime, LuaValue keyValue]
		{
			get
			{
				var name = keyValue.ToString();
				if (!members.TryGetValue(name, out var wrapper))
					throw new LuaException(MemberNotFoundError(name));

				return wrapper.Get(runtime);
			}

			set
			{
				var name = keyValue.ToString();
				if (!members.TryGetValue(name, out var wrapper))
					throw new LuaException(MemberNotFoundError(name));

				wrapper.Set(value);
			}
		}
	}
}
