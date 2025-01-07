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
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands.Documentation
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
			Console.WriteLine("-- See https://docs.openra.net/en/release/lua/ for human readable documentation.");

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

			var actorProperties = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptActorProperties>();
			WriteScriptProperties(typeof(Actor), actorProperties);

			var playerProperties = utility.ModData.ObjectCreator.GetTypesImplementing<ScriptPlayerProperties>();
			WriteScriptProperties(typeof(Player), playerProperties);
		}

		static void WriteDiagnosticsDisabling()
		{
			Console.WriteLine(
				"--- This file only lists function \"signatures\", causing Lua Diagnostics errors: " +
				"\"Annotations specify that a return value is required here.\"");
			Console.WriteLine("--- and Lua Diagnostics warnings \"Unused local\" for the functions' parameters.");
			Console.WriteLine("--- Disable those specific errors for the entire file.");
			Console.WriteLine("---@diagnostic disable: missing-return");
			Console.WriteLine("---@diagnostic disable: unused-local");
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
			Console.WriteLine();
			Console.WriteLine("---@class cpos");
			Console.WriteLine("---@field X integer");
			Console.WriteLine("---@field Y integer");
			Console.WriteLine("---@field Layer integer");
			Console.WriteLine("---@operator add(cvec): cpos");
			Console.WriteLine("---@operator sub(cvec): cpos");
			Console.WriteLine("---@operator sub(cpos): cvec");
			Console.WriteLine();
			Console.WriteLine("---@class wpos");
			Console.WriteLine("---@field X integer");
			Console.WriteLine("---@field Y integer");
			Console.WriteLine("---@field Z integer");
			Console.WriteLine("---@operator add(wvec): wpos");
			Console.WriteLine("---@operator sub(wvec): wpos");
			Console.WriteLine("---@operator sub(wpos): wvec");
			Console.WriteLine();
			Console.WriteLine("---@class wangle");
			Console.WriteLine("---@field Angle integer");
			Console.WriteLine("---@operator add(wangle): wangle");
			Console.WriteLine("---@operator sub(wangle): wangle");
			Console.WriteLine();
			Console.WriteLine("---@class wdist");
			Console.WriteLine("---@field Length integer");
			Console.WriteLine("---@operator add(wdist): wdist");
			Console.WriteLine("---@operator sub(wdist): wdist");
			Console.WriteLine("---@operator unm(wdist): wdist");
			Console.WriteLine("---@operator mul(integer): wdist");
			Console.WriteLine("---@operator div(integer): wdist");
			Console.WriteLine();
			Console.WriteLine("---@class wvec");
			Console.WriteLine("---@field X integer");
			Console.WriteLine("---@field Y integer");
			Console.WriteLine("---@field Z integer");
			Console.WriteLine("---@field Facing wangle");
			Console.WriteLine("---@operator add(wvec): wvec");
			Console.WriteLine("---@operator sub(wvec): wvec");
			Console.WriteLine("---@operator unm(wvec): wvec");
			Console.WriteLine("---@operator mul(integer): wvec");
			Console.WriteLine("---@operator div(integer): wvec");
			Console.WriteLine();
			Console.WriteLine("---@class cvec");
			Console.WriteLine("---@field X integer");
			Console.WriteLine("---@field Y integer");
			Console.WriteLine("---@field Length integer");
			Console.WriteLine("---@operator add(cvec): cvec");
			Console.WriteLine("---@operator sub(cvec): cvec");
			Console.WriteLine("---@operator unm(cvec): cvec");
			Console.WriteLine("---@operator mul(integer): cvec");
			Console.WriteLine("---@operator div(integer): cvec");
			Console.WriteLine();
			Console.WriteLine("---@class color");
			Console.WriteLine("local color = { };");
		}

		static void WriteActorInits(IEnumerable<Type> actorInits, out IEnumerable<Type> usedEnums)
		{
			Console.WriteLine("---A list of ActorInit implementations that can be used by Lua scripts.");
			Console.WriteLine("---@class initTable");

			var localEnums = new HashSet<Type>();
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

									return p.EmmyLuaString($"{init.Name}").TypeDeclaration;
								})))
						.Where(s => !s.Contains(", "))
						.Distinct());

				if (!string.IsNullOrEmpty(parameterString))
				{
					// OwnerInit is special as it is the only "required" init. All others are optional.
					if (init.Name != nameof(OwnerInit))
						parameterString += '?';

					Console.WriteLine($"---@field {name} {parameterString}");
				}
			}

			usedEnums = localEnums;
		}

		static void WriteEnums(IEnumerable<Type> enumTypes)
		{
			foreach (var enumType in enumTypes)
			{
				Console.WriteLine($"---@enum {enumType.Name}");
				Console.WriteLine(enumType.Name + " = {");

				foreach (var value in Enum.GetValues(enumType))
					Console.WriteLine($"    {value} = {Convert.ChangeType(value, typeof(int), NumberFormatInfo.InvariantInfo)},");

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
					else
						throw new NotSupportedException($"Missing {nameof(DescAttribute)} on {t.Name} {member.Name}");

					if (member is PropertyInfo propertyInfo)
					{
						var attributes = propertyInfo.GetCustomAttributes(false);
						foreach (var obsolete in attributes.OfType<ObsoleteAttribute>())
							Console.WriteLine($"    ---@deprecated {obsolete.Message}");

						Console.WriteLine($"    ---@type {propertyInfo.PropertyType.EmmyLuaString($"{t.Name} {member.Name}")}");
						body = propertyInfo.Name + " = nil;";
					}

					if (member is MethodInfo methodInfo)
					{
						var parameters = methodInfo.GetParameters();
						var luaParameters = parameters
							.Select(parameter => parameter.NameAndEmmyLuaString($"{t.Name} {member.Name}"))
							.ToArray();
						foreach (var generic in luaParameters.Select(p => p.Generic).Where(g => !string.IsNullOrEmpty(g)).Distinct())
							Console.WriteLine($"    ---@generic {generic}");
						foreach (var nameAndType in luaParameters.Select(p => p.NameAndType))
							Console.WriteLine($"    ---@param {nameAndType}");

						var parameterString = parameters.Select(p => p.Name).JoinWith(", ");

						var attributes = methodInfo.GetCustomAttributes(false);
						foreach (var obsolete in attributes.OfType<ObsoleteAttribute>())
							Console.WriteLine($"    ---@deprecated {obsolete.Message}");

						if (methodInfo.ReturnType != typeof(void))
							Console.WriteLine($"    ---@return {methodInfo.ReturnTypeEmmyLuaString($"{t.Name} {member.Name}")}");

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
			var tableName = $"__{className}";
			Console.WriteLine($"---@class {className}");

			var members = implementingTypes.SelectMany(t =>
			{
				var requiredTraits = ScriptMemberWrapper.RequiredTraitNames(t);
				return ScriptMemberWrapper.WrappableMembers(t).Select(memberInfo => (memberInfo, requiredTraits));
			});

			var duplicateMembers = members
				.GroupBy(x => x.memberInfo.Name)
				.Where(x => x.Count() > 1)
				.Select(x => x.Key)
				.ToHashSet();

			foreach (var (memberInfo, requiredTraits) in members)
			{
				// Properties are supposed to be defined as @fields on the class.
				// They can be defined as keys inside the tables, but then are treated as readonly by the Lua extension.
				if (memberInfo is PropertyInfo propertyInfo && propertyInfo.CanWrite)
				{
					WriteMemberDescription(memberInfo, requiredTraits, 0);

					if (duplicateMembers.Contains(memberInfo.Name))
						Console.WriteLine("    ---@diagnostic disable-next-line: duplicate-index");

					Console.WriteLine($"---@field {propertyInfo.Name} {propertyInfo.PropertyType.EmmyLuaString($"{memberInfo.DeclaringType.Name} {memberInfo.Name}")}");
				}
			}

			Console.WriteLine("local " + tableName + " = {");

			foreach (var (memberInfo, requiredTraits) in members)
			{
				// Properties are supposed to be defined as @fields on the class,
				// but if they are defined as keys inside the table, they are treated as readonly by the Lua extension.
				if (memberInfo is PropertyInfo propertyInfo && !propertyInfo.CanWrite)
				{
					Console.WriteLine();
					WriteMemberDescription(memberInfo, requiredTraits, 1);

					if (duplicateMembers.Contains(memberInfo.Name))
						Console.WriteLine("    ---@diagnostic disable-next-line: duplicate-index");

					Console.WriteLine($"    ---@type {propertyInfo.PropertyType.EmmyLuaString($"{memberInfo.DeclaringType.Name} {memberInfo.Name}")}");
					Console.WriteLine($"    {propertyInfo.Name} = nil;");
				}

				// Functions are defined as keys inside the table.
				if (memberInfo is MethodInfo methodInfo)
				{
					Console.WriteLine();
					WriteMemberDescription(memberInfo, requiredTraits, 1);

					var attributes = methodInfo.GetCustomAttributes(false);
					foreach (var obsolete in attributes.OfType<ObsoleteAttribute>())
						Console.WriteLine($"    ---@deprecated {obsolete.Message}");

					var parameters = methodInfo.GetParameters();
					var luaParameters = parameters
						.Select(parameter => parameter.NameAndEmmyLuaString($"{memberInfo.DeclaringType.Name} {memberInfo.Name}"))
						.ToArray();
					foreach (var generic in luaParameters.Select(p => p.Generic).Where(g => !string.IsNullOrEmpty(g)).Distinct())
						Console.WriteLine($"    ---@generic {generic}");
					foreach (var nameAndType in luaParameters.Select(p => p.NameAndType))
						Console.WriteLine($"    ---@param {nameAndType}");

					var parameterString = parameters.Select(p => p.Name).JoinWith(", ");

					if (methodInfo.ReturnType != typeof(void))
						Console.WriteLine($"    ---@return {methodInfo.ReturnTypeEmmyLuaString($"{memberInfo.DeclaringType.Name} {memberInfo.Name}")}");

					if (duplicateMembers.Contains(methodInfo.Name))
						Console.WriteLine("    ---@diagnostic disable-next-line: duplicate-index");

					Console.WriteLine($"    {methodInfo.Name} = function({parameterString}) end;");
				}
			}

			Console.WriteLine("}");
			Console.WriteLine();

			static void WriteMemberDescription(MemberInfo memberInfo, string[] requiredTraits, int indentation)
			{
				var isActivity = Utility.HasAttribute<ScriptActorPropertyActivityAttribute>(memberInfo);

				if (Utility.HasAttribute<DescAttribute>(memberInfo))
				{
					var lines = Utility.GetCustomAttributes<DescAttribute>(memberInfo, true).First().Lines;
					foreach (var line in lines)
						Console.WriteLine($"{new string(' ', indentation * 4)}--- {line}");
				}
				else
					throw new NotSupportedException($"Missing {nameof(DescAttribute)} on {memberInfo.DeclaringType.Name} {memberInfo.Name}");

				if (isActivity)
					Console.WriteLine(
						$"{new string(' ', indentation * 4)}--- *Queued Activity*");

				if (requiredTraits.Length != 0)
					Console.WriteLine(
						$"{new string(' ', indentation * 4)}--- **Requires {(requiredTraits.Length == 1 ? "Trait" : "Traits")}:** " +
						$"{requiredTraits.Select(GetDocumentationUrl).JoinWith(", ")}");
			}
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
			// These are weak type mappings, don't add these.
			// Instead, use ScriptEmmyTypeOverrideAttribute to provide a specific type.
			////{ "Object", "any" },
			////{ "LuaValue", "any" },
			////{ "LuaTable", "table" },
			////{ "LuaFunction", "function" },
			{ "Byte", "integer" },
			{ "UInt32", "integer" },
			{ "Int32", "integer" },
			{ "String", "string" },
			{ "Boolean", "boolean" },
			{ "Double", "number" },
			{ "WVec", "wvec" },
			{ "CVec", "cvec" },
			{ "CPos", "cpos" },
			{ "WPos", "wpos" },
			{ "WAngle", "wangle" },
			{ "WDist", "wdist" },
			{ "Color", "color" },
			{ "Actor", "actor" },
			{ "Player", "player" },
		};

		public static string EmmyLuaString(this Type type, string notSupportedExceptionContext)
		{
			if (type.IsArray)
				return EmmaLuaStringInner(type.GetElementType(), notSupportedExceptionContext) + "[]";

			return EmmaLuaStringInner(type, notSupportedExceptionContext);

			static string EmmaLuaStringInner(Type type, string context)
			{
				if (LuaTypeNameReplacements.TryGetValue(type.Name, out var replacement))
					return replacement;

				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					var argument = type.GetGenericArguments()[0].Name;
					if (LuaTypeNameReplacements.TryGetValue(argument, out var genericReplacement))
						return $"{genericReplacement}?";
				}

				if (type.IsEnum)
					return type.Name;

				// This may indicate we are trying to export a type we have not added support for yet.
				// Consider adding support for this type.
				// This may mean updating WriteManual and adding IScriptBindable to the type.
				// Or adding an entry to LuaTypeNameReplacements.
				// Or use ScriptEmmyTypeOverride to provide a custom type for a parameter.
				// Or consider using ISuppressInitExport if the parameter is coming from an init we don't want to expose to Lua.
				// Or, it may need a different approach than the ones listed above.
				throw new NotSupportedException(
					$"Command lacks support for exposing type to Lua: `{type}` required by `{context}`. " +
					$"Consider applying {nameof(ScriptEmmyTypeOverrideAttribute)} or {nameof(ISuppressInitExport)}");
			}
		}

		public static string ReturnTypeEmmyLuaString(this MethodInfo methodInfo, string notSupportedExceptionContext)
		{
			var overrideAttr = methodInfo.ReturnTypeCustomAttributes
				.GetCustomAttributes(typeof(ScriptEmmyTypeOverrideAttribute), false)
				.Cast<ScriptEmmyTypeOverrideAttribute>()
				.SingleOrDefault();
			if (overrideAttr != null)
				return overrideAttr.TypeDeclaration;

			return methodInfo.ReturnType.EmmyLuaString(notSupportedExceptionContext);
		}

		public static (string TypeDeclaration, string GenericTypeDeclaration) EmmyLuaString(this ParameterInfo parameterInfo, string notSupportedExceptionContext)
		{
			var overrideAttr = parameterInfo.GetCustomAttribute<ScriptEmmyTypeOverrideAttribute>();
			if (overrideAttr != null)
				return (overrideAttr.TypeDeclaration, overrideAttr.GenericTypeDeclaration);

			return (parameterInfo.ParameterType.EmmyLuaString(notSupportedExceptionContext), null);
		}

		public static (string NameAndType, string Generic) NameAndEmmyLuaString(this ParameterInfo parameterInfo, string notSupportedExceptionContext)
		{
			var optional = parameterInfo.IsOptional ? "?" : "";
			var (typeDeclaration, genericTypeDeclaration) = parameterInfo.EmmyLuaString(notSupportedExceptionContext);
			return ($"{parameterInfo.Name}{optional} {typeDeclaration}", genericTypeDeclaration);
		}
	}
}
