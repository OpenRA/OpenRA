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
using System.Collections.Generic;
using System.Reflection;
using Eluant;
using Eluant.ObjectBinding;

namespace OpenRA.Scripting
{
	public abstract class ScriptObjectWrapper : IScriptBindable, ILuaTableBinding
	{
		protected abstract string DuplicateKeyError(string memberName);
		protected abstract string MemberNotFoundError(string memberName);

		protected readonly ScriptContext Context;
		Dictionary<string, ScriptMemberWrapper> members;

		// Binding filters
		protected static readonly Predicate<MemberInfo> BindAny = x => true;
		protected static readonly Predicate<MemberInfo> BindAIOnly = x =>
			!x.HasAttribute<ScriptContextAttribute>() || x.GetCustomAttribute<ScriptContextAttribute>().Type == ScriptContextType.AI;
		protected static readonly Predicate<MemberInfo> BindMissionOnly = x =>
			!x.HasAttribute<ScriptContextAttribute>() || x.GetCustomAttribute<ScriptContextAttribute>().Type == ScriptContextType.Mission;

		public ScriptObjectWrapper(ScriptContext context)
		{
			Context = context;
		}

		protected void Bind(IEnumerable<object> clrObjects, Predicate<MemberInfo> canBind)
		{
			members = new Dictionary<string, ScriptMemberWrapper>();
			foreach (var obj in clrObjects)
			{
				var wrappable = ScriptMemberWrapper.WrappableMembers(obj.GetType());
				foreach (var m in wrappable)
				{
					if (members.ContainsKey(m.Name))
						throw new LuaException(DuplicateKeyError(m.Name));

					if (canBind(m))
						members.Add(m.Name, new ScriptMemberWrapper(Context, obj, m));
				}
			}
		}

		public bool ContainsKey(string key) { return members.ContainsKey(key); }

		public LuaValue this[LuaRuntime runtime, LuaValue keyValue]
		{
			get
			{
				var name = keyValue.ToString();
				ScriptMemberWrapper wrapper;
				if (!members.TryGetValue(name, out wrapper))
					throw new LuaException(MemberNotFoundError(name));

				return wrapper.Get(runtime);
			}

			set
			{
				var name = keyValue.ToString();
				ScriptMemberWrapper wrapper;
				if (!members.TryGetValue(name, out wrapper))
					throw new LuaException(MemberNotFoundError(name));

				wrapper.Set(runtime, value);
			}
		}
	}
}
