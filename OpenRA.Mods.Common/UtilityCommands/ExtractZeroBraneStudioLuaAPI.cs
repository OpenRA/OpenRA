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
using System.Linq;
using System.Reflection;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.UtilityCommands
{
	// See https://studio.zerobrane.com/doc-api-auto-complete for reference
	class ExtractZeroBraneStudioLuaAPI : IUtilityCommand
	{
		string IUtilityCommand.Name => "--zbstudio-lua-api";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Generate ZeroBrane Studio Lua API and auto-complete descriptions.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Console.WriteLine("local interpreter = {");
			Console.WriteLine("  name = \"OpenRA\",");
			Console.WriteLine("  description = \"OpenRA map scripting Lua API\",");
			Console.WriteLine("  api = {\"baselib\", \"openra\"},");
			Console.WriteLine("  hasdebugger = false,");
			Console.WriteLine("  skipcompile = true,");
			Console.WriteLine("}");
			Console.WriteLine();

			Console.WriteLine("-- This is an automatically generated Lua API definition generated for {0} of OpenRA.", utility.ModData.Manifest.Metadata.Version);
			Console.WriteLine("-- https://github.com/OpenRA/OpenRA/wiki/Utility was used with the --zbstudio-lua-api parameter.");
			Console.WriteLine("-- See https://github.com/OpenRA/OpenRA/wiki/Lua-API for human readable documentation.");
			Console.WriteLine();
			Console.WriteLine("local api = {");

			var tables = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptGlobal>().OrderBy(t => t.Name);
			foreach (var t in tables)
			{
				var name = t.GetCustomAttributes<ScriptGlobalAttribute>(true).First().Name;
				Console.WriteLine("  " + name + " = {");
				Console.WriteLine("    type = \"class\",");
				Console.WriteLine("    childs = {");

				var members = ScriptMemberWrapper.WrappableMembers(t);
				foreach (var member in members.OrderBy(m => m.Name))
				{
					Console.WriteLine("      " + member.Name + " = {");
					var methodInfo = member as MethodInfo;
					if (methodInfo != null)
						Console.WriteLine("        type = \"function\",");

					var propertyInfo = member as PropertyInfo;
					if (propertyInfo != null)
						Console.WriteLine("        type = \"value\",");

					if (member.HasAttribute<DescAttribute>())
					{
						var desc = member.GetCustomAttributes<DescAttribute>(true).First().Lines.JoinWith("\n");
						Console.WriteLine("        description = [[{0}]],", desc);
					}

					if (methodInfo != null)
					{
						var parameters = methodInfo.GetParameters().Select(pi => pi.LuaDocString());
						Console.WriteLine("        args = \"({0})\",", parameters.JoinWith(", "));

						var returnType = methodInfo.ReturnType.LuaDocString();
						Console.WriteLine("        returns = \"({0})\",", returnType);
					}

					Console.WriteLine("      },");
				}

				Console.WriteLine("    }");
				Console.WriteLine("  },");
			}

			var actorProperties = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptActorProperties>().SelectMany(cg =>
			{
				return ScriptMemberWrapper.WrappableMembers(cg);
			});

			var scriptProperties = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptPlayerProperties>().SelectMany(cg =>
			{
				return ScriptMemberWrapper.WrappableMembers(cg);
			});

			var properties = actorProperties.Concat(scriptProperties);
			foreach (var property in properties.OrderBy(m => m.Name))
			{
				Console.WriteLine("  " + property.Name + " = {");

				var methodInfo = property as MethodInfo;
				if (methodInfo != null)
					Console.WriteLine("    type = \"function\",");

				var propertyInfo = property as PropertyInfo;
				if (propertyInfo != null)
					Console.WriteLine("    type = \"value\",");

				if (property.HasAttribute<DescAttribute>())
				{
					var desc = property.GetCustomAttributes<DescAttribute>(true).First().Lines.JoinWith("\n");
					Console.WriteLine("    description = [[{0}]],", desc);
				}

				if (methodInfo != null)
				{
					var parameters = methodInfo.GetParameters().Select(pi => pi.LuaDocString());
					Console.WriteLine("    args = \"({0})\",", parameters.JoinWith(", "));

					var returnType = methodInfo.ReturnType.LuaDocString();
					Console.WriteLine("    returns = \"({0})\",", returnType);
				}

				Console.WriteLine("  },");
			}

			Console.WriteLine("}");
			Console.WriteLine();
			Console.WriteLine("return {");
			Console.WriteLine("  name = \"OpenRA\",");
			Console.WriteLine("  description = \"Adds API description for auto-complete and tooltip support for OpenRA.\",");
			Console.WriteLine("  author = \"Matthias Mailänder\",");
			Console.WriteLine($"  version = \"{utility.ModData.Manifest.Metadata.Version.Split('-').LastOrDefault()}\",");
			Console.WriteLine();
			Console.WriteLine("  onRegister = function(self)");
			Console.WriteLine("    ide:AddAPI(\"lua\", \"openra\", api)");
			Console.WriteLine("    ide:AddInterpreter(\"openra\", interpreter)");
			Console.WriteLine("  end,");
			Console.WriteLine();
			Console.WriteLine("  onUnRegister = function(self)");
			Console.WriteLine("    ide:RemoveAPI(\"lua\", \"openra\")");
			Console.WriteLine("    ide:RemoveInterpreter(\"openra\")");
			Console.WriteLine("  end,");
			Console.WriteLine("}");
		}
	}
}
