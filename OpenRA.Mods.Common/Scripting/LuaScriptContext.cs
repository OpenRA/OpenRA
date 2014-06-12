#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLua;
using NLua.Event;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Scripting
{
	public sealed class LuaScriptContext : IDisposable
	{
		public Lua Lua { get; private set; }
		readonly Cache<string, LuaFunction> functionCache;

		public LuaScriptContext()
		{
			Log.AddChannel("lua", "lua.log");
			Log.Write("lua", "Creating Lua script context");
			Lua = new Lua();
			Lua.HookException += OnLuaException;
			functionCache = new Cache<string, LuaFunction>(Lua.GetFunction);
		}

		public void RegisterObject(object target, string tableName, bool exposeAllMethods)
		{
			Log.Write("lua", "Registering object {0}", target);

			if (tableName != null && Lua.GetTable(tableName) == null)
				Lua.NewTable(tableName);

			var type = target.GetType();

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
			RegisterMethods(tableName, target, methods, exposeAllMethods);
		}

		public void RegisterType(Type type, string tableName, bool exposeAllMethods)
		{
			Log.Write("lua", "Registering type {0}", type);

			if (tableName != null && Lua.GetTable(tableName) == null)
				Lua.NewTable(tableName);

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
			RegisterMethods(tableName, null, methods, exposeAllMethods);
		}

		void RegisterMethods(string tableName, object target, IEnumerable<MethodInfo> methods, bool allMethods)
		{
			foreach (var method in methods)
			{
				string methodName;

				var attr = method.GetCustomAttributes<LuaGlobalAttribute>(true).FirstOrDefault();
				if (attr == null)
				{
					if (allMethods)
						methodName = method.Name;
					else
						continue;
				}
				else
					methodName = attr.Name ?? method.Name;

				var methodTarget = method.IsStatic ? null : target;

				if (tableName != null)
					Lua.RegisterFunction(tableName + "." + methodName, methodTarget, method);
				else
					Lua.RegisterFunction(methodName, methodTarget, method);
			}
		}

		void OnLuaException(object sender, HookExceptionEventArgs e)
		{
			ShowException(e.Exception);
		}

		void ShowException(Exception e)
		{
			ShowErrorMessage(e.Message, e.ToString());
		}

		public void ShowErrorMessage(string shortMessage, string longMessage)
		{
			Game.Debug("{0}", shortMessage);
			Game.Debug("See lua.log for details");
			Log.Write("lua", "{0}", longMessage ?? shortMessage);
		}

		public void LoadLuaScripts(Func<string, string> getFileContents, params string[] files)
		{
			foreach (var file in files)
			{
				try
				{
					Log.Write("lua", "Loading Lua script {0}", file);
					var content = getFileContents(file);
					Lua.DoString(content, file);
				}
				catch (Exception e)
				{
					ShowException(e);
				}
			}
		}

		public object[] InvokeLuaFunction(string name, params object[] args)
		{
			try
			{
				var function = functionCache[name];
				if (function == null)
					return null;
				return function.Call(args);
			}
			catch (Exception e)
			{
				ShowException(e);
				return null;
			}
		}

		public void Dispose()
		{
			if (Lua != null)
				Lua.Dispose();
		}
	}
}
