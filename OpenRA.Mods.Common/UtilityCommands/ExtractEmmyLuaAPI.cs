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
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	// See https://emmylua.github.io/annotation.html for reference
	sealed class ExtractEmmyLuaAPI : IUtilityCommand
	{
		string IUtilityCommand.Name => "--emmy-lua-api";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Generate EmmyLua API annotations for use in IDEs.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var version = utility.ModData.Manifest.Metadata.Version;
			Console.WriteLine($"-- This is an automatically generated Lua API definition generated for {version} of OpenRA.");
			Console.WriteLine("-- https://wiki.openra.net/Utility was used with the --emmy-lua-api parameter.");
			Console.WriteLine("-- See https://docs.openra.net/en/latest/release/lua/ for human readable documentation.");

			Console.WriteLine();
			WriteDiagnosticsDisabling();
			Console.WriteLine();

			Console.WriteLine();
			WriteManual();
			Console.WriteLine();

			Console.WriteLine();
			var actorInits = utility.ModData.ObjectCreator.GetTypesImplementing<ActorInit>()
				.Where(x => !x.IsAbstract && !x.GetInterfaces().Contains(typeof(ISuppressInitExport)));
			WriteActorInits(actorInits, out var usedEnums);
			Console.WriteLine();

			Console.WriteLine();
			WriteEnums(usedEnums);
			Console.WriteLine();

			Console.WriteLine();
			var globalTables = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptGlobal>().OrderBy(t => t.Name);
			WriteGlobals(globalTables);
			Console.WriteLine();

			var actorProperties = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptActorProperties>();
			WriteScriptProperties(typeof(Actor), actorProperties);

			var playerProperties = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptPlayerProperties>();
			WriteScriptProperties(typeof(Player), playerProperties);
		}

		static void WriteDiagnosticsDisabling()
		{
			Console.WriteLine("--- This file only lists function \"signatures\", causing Lua Diagnostics errors: \"Annotations specify that a return value is required here.\"");
			Console.WriteLine("--- Disable that specific error for the entire file.");
			Console.WriteLine("---@diagnostic disable: missing-return");
		}

		static void WriteManual()
		{
			Console.WriteLine("--- This function is triggered once, after the map is loaded.");
			Console.WriteLine("function WorldLoaded() end");
			Console.WriteLine();
			Console.WriteLine("--- This function will hit every game tick which by default is every 40 ms.");
			Console.WriteLine("function Tick() end");
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("--- Base engine types.");
			Console.WriteLine("---@class cpos");
			Console.WriteLine("---@field X integer");
			Console.WriteLine("---@field Y integer");
			Console.WriteLine("---@operator add(cvec): cpos");
			Console.WriteLine("---@operator sub(cvec): cpos");
			Console.WriteLine();
			Console.WriteLine("---@class wpos");
			Console.WriteLine("---@field X integer");
			Console.WriteLine("---@field Y integer");
			Console.WriteLine("---@field Z integer");
			Console.WriteLine("---@operator add(wvec): wpos");
			Console.WriteLine("---@operator sub(wvec): wpos");
			Console.WriteLine();
			Console.WriteLine("---@class wangle");
			Console.WriteLine("---@field Angle integer");
			Console.WriteLine("---@operator add(wangle): wangle");
			Console.WriteLine("---@operator sub(wangle): wangle");
			Console.WriteLine();
			Console.WriteLine("---@class wdist");
			Console.WriteLine("---@field Length integer");
			Console.WriteLine();
			Console.WriteLine("---@class wvec");
			Console.WriteLine("---@field X integer");
			Console.WriteLine("---@field Y integer");
			Console.WriteLine("---@field Z integer");
			Console.WriteLine("---@operator add(wvec): wvec");
			Console.WriteLine("---@operator sub(wvec): wvec");
			Console.WriteLine();
			Console.WriteLine("---@class cvec");
			Console.WriteLine("---@field X integer");
			Console.WriteLine("---@field Y integer");
			Console.WriteLine("---@operator add(cvec): cvec");
			Console.WriteLine("---@operator sub(cvec): cvec");
			Console.WriteLine();
			Console.WriteLine("---@class color");
			Console.WriteLine("local color = { };");
		}

		static void WriteActorInits(IEnumerable<Type> actorInits, out IEnumerable<Type> usedEnums)
		{
			Console.WriteLine("---A list of ActorInit implementations that can be used by Lua scripts.");
			Console.WriteLine("---@class initTable");

			var localEnums = new List<Type>();
			foreach (var init in actorInits)
			{
				var name = init.Name[..^4];
				var parameters = init.GetConstructors().Select(ci => ci.GetParameters());
				var parameterString = string.Join(" | ",
					parameters
						.Select(cp => string.Join(", ",
							cp
								.Where(p => !p.HasDefaultValue && p.ParameterType != typeof(TraitInfo)
									&& p.ParameterType.Name != typeof(Func<int>).Name)
								.Select(p =>
								{
									if (p.ParameterType.IsEnum)
										localEnums.Add(p.ParameterType);

									return p.ParameterType.EmmyLuaString();
								})))
						.Where(s => !s.Contains(", "))
						.Distinct());

				if (!string.IsNullOrEmpty(parameterString))
					Console.WriteLine($"---@field {name} {parameterString}");
			}

			usedEnums = localEnums.Distinct();
		}

		static void WriteEnums(IEnumerable<Type> enumTypes)
		{
			foreach (var enumType in enumTypes)
			{
				Console.WriteLine($"---@enum {enumType.Name}");
				Console.WriteLine(enumType.Name + " = {");

				foreach (var value in Enum.GetValues(enumType))
					Console.WriteLine($"    {value} = {Convert.ChangeType(value, typeof(int))},");

				Console.WriteLine("}");
				Console.WriteLine();
			}
		}

		static void WriteGlobals(IEnumerable<Type> globalTables)
		{
			foreach (var t in globalTables)
			{
				var name = Utility.GetCustomAttributes<ScriptGlobalAttribute>(t, true).First().Name;
				Console.WriteLine("---Global variable provided by the game scripting engine.");

				foreach (var obsolete in t.GetCustomAttributes(false).OfType<ObsoleteAttribute>())
				{
					Console.WriteLine("---@deprecated");
					Console.WriteLine($"--- {obsolete.Message}");
				}

				Console.WriteLine(name + " = {");

				var members = ScriptMemberWrapper.WrappableMembers(t);
				foreach (var member in members.OrderBy(m => m.Name))
				{
					Console.WriteLine();

					var body = "";

					if (Utility.HasAttribute<DescAttribute>(member))
					{
						var lines = Utility.GetCustomAttributes<DescAttribute>(member, true).First().Lines;
						foreach (var line in lines)
							Console.WriteLine($"    --- {line}");
					}

					if (member is PropertyInfo propertyInfo)
					{
						var attributes = propertyInfo.GetCustomAttributes(false);
						foreach (var obsolete in attributes.OfType<ObsoleteAttribute>())
							Console.WriteLine($"    ---@deprecated {obsolete.Message}");

						Console.WriteLine($"    ---@type {propertyInfo.PropertyType.EmmyLuaString()}");
						body = propertyInfo.Name + " = nil;";
					}

					if (member is MethodInfo methodInfo)
					{
						var parameters = methodInfo.GetParameters();
						foreach (var parameter in parameters)
							Console.WriteLine($"    ---@param {parameter.EmmyLuaString()}");

						var parameterString = parameters.Select(p => p.Name).JoinWith(", ");

						var attributes = methodInfo.GetCustomAttributes(false);
						foreach (var obsolete in attributes.OfType<ObsoleteAttribute>())
							Console.WriteLine($"    ---@deprecated {obsolete.Message}");

						var returnType = methodInfo.ReturnType.EmmyLuaString();
						if (returnType != "Void")
							Console.WriteLine($"    ---@return {returnType}");

						body = member.Name + $" = function({parameterString}) end;";
					}

					Console.WriteLine($"    {body}");
				}

				Console.WriteLine("}");
				Console.WriteLine();
			}
		}

		static void WriteScriptProperties(Type type, IEnumerable<Type> implementingTypes)
		{
			var className = type.Name.ToLowerInvariant();
			var tableName = $"__{type.Name.ToLowerInvariant()}";
			Console.WriteLine($"---@class {className}");
			Console.WriteLine("local " + tableName + " = {");

			var properties = implementingTypes.SelectMany(t =>
			{
				var requiredTraits = ScriptMemberWrapper.RequiredTraitNames(t);
				return ScriptMemberWrapper.WrappableMembers(t).Select(memberInfo => (memberInfo, requiredTraits));
			});

			var duplicateProperties = properties
				.GroupBy(x => x.memberInfo.Name)
				.Where(x => x.Count() > 1)
				.Select(x => x.Key)
				.ToHashSet();

			foreach (var (memberInfo, requiredTraits) in properties)
			{
				Console.WriteLine();

				var isActivity = Utility.HasAttribute<ScriptActorPropertyActivityAttribute>(memberInfo);

				if (Utility.HasAttribute<DescAttribute>(memberInfo))
				{
					var lines = Utility.GetCustomAttributes<DescAttribute>(memberInfo, true).First().Lines;
					foreach (var line in lines)
						Console.WriteLine($"    --- {line}");
				}

				if (isActivity)
					Console.WriteLine("    --- *Queued Activity*");

				if (requiredTraits.Any())
					Console.WriteLine($"    --- **Requires {(requiredTraits.Length == 1 ? "Trait" : "Traits")}:** {requiredTraits.Select(GetDocumentationUrl).JoinWith(", ")}");

				if (memberInfo is MethodInfo methodInfo)
				{
					var attributes = methodInfo.GetCustomAttributes(false);
					foreach (var obsolete in attributes.OfType<ObsoleteAttribute>())
						Console.WriteLine($"    ---@deprecated {obsolete.Message}");

					var parameters = methodInfo.GetParameters();
					foreach (var parameter in parameters)
						Console.WriteLine($"    ---@param {parameter.EmmyLuaString()}");

					var parameterString = parameters.Select(p => p.Name).JoinWith(", ");

					var returnType = methodInfo.ReturnType.EmmyLuaString();
					if (returnType != "Void")
						Console.WriteLine($"    ---@return {returnType}");

					if (duplicateProperties.Contains(methodInfo.Name))
						Console.WriteLine("    ---@diagnostic disable-next-line: duplicate-index");

					Console.WriteLine($"    {methodInfo.Name} = function({parameterString}) end;");
				}

				if (memberInfo is PropertyInfo propertyInfo)
				{
					Console.WriteLine($"    ---@type {propertyInfo.PropertyType.EmmyLuaString()}");

					if (duplicateProperties.Contains(propertyInfo.Name))
						Console.WriteLine("    ---@diagnostic disable-next-line: duplicate-index");

					Console.WriteLine("    " + propertyInfo.Name + " = nil;");
				}
			}

			Console.WriteLine("}");
			Console.WriteLine();
		}

		static string GetDocumentationUrl(string trait)
		{
			return $"[{trait}](https://docs.openra.net/en/release/traits/#{trait.ToLowerInvariant()})";
		}
	}

	public static class EmmyLuaExts
	{
		static readonly Dictionary<string, string> LuaTypeNameReplacements = new()
		{
			{ "UInt32", "integer" },
			{ "Int32", "integer" },
			{ "String", "string" },
			{ "String[]", "string[]" },
			{ "Boolean", "boolean" },
			{ "Double", "number" },
			{ "Object", "any" },
			{ "LuaTable", "table" },
			{ "LuaValue", "any" },
			{ "LuaValue[]", "table" },
			{ "LuaFunction", "function" },
			{ "WVec", "wvec" },
			{ "CVec", "cvec" },
			{ "CPos", "cpos" },
			{ "CPos[]", "cpos[]" },
			{ "WPos", "wpos" },
			{ "WAngle", "wangle" },
			{ "WAngle[]", "wangle[]" },
			{ "WDist", "wdist" },
			{ "Color", "color" },
			{ "Actor", "actor" },
			{ "Actor[]", "actor[]" },
			{ "Player", "player" },
			{ "Player[]", "player[]" },
		};

		public static string EmmyLuaString(this Type type)
		{
			if (!LuaTypeNameReplacements.TryGetValue(type.Name, out var replacement))
				replacement = type.Name;

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				var argument = type.GetGenericArguments().Select(p => p.Name).First();
				if (LuaTypeNameReplacements.TryGetValue(argument, out var genericReplacement))
					replacement = $"{genericReplacement}?";
				else
					replacement = $"{type.GetGenericArguments().Select(p => p.Name).First()}?";
			}

			return replacement;
		}

		public static string EmmyLuaString(this ParameterInfo parameterInfo)
		{
			var optional = parameterInfo.IsOptional ? "?" : "";

			var parameterType = parameterInfo.ParameterType.EmmyLuaString();

			// A hack for ActorGlobal.Create().
			if (parameterInfo.Name == "initTable")
				parameterType = "initTable";

			return $"{parameterInfo.Name}{optional} {parameterType}";
		}

		public static string EmmyLuaString(this PropertyInfo propertyInfo)
		{
			return $"{propertyInfo.Name} {propertyInfo.PropertyType.EmmyLuaString()}";
		}
	}
}
