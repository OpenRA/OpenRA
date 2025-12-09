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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Linguini.Syntax.Ast;
using Linguini.Syntax.Parser;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Scripting.Global;
using OpenRA.Scripting;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Lint
{
	sealed class CheckFluentReferences : ILintPass, ILintMapPass
	{
		const BindingFlags StaticBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.FluentMessageDefinitions == null)
				return;

			var usedKeys = ExtractMapFluentKeys(modData, map, emitWarning);
			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in map ftl files required by {context}");

			var mapMessages = FieldLoader.GetValue<ImmutableArray<string>>("value", map.FluentMessageDefinitions.Value);
			var modMessages = modData.Manifest.FluentMessages;

			// For maps we don't warn on unused keys. They might be unused on *this* map,
			// but the mod or another map may use them and we don't have sight of that.
			CheckKeys(modMessages.Concat(mapMessages), map.Open, usedKeys, _ => false, emitError, emitWarning);

			var modFluentBundle = new FluentBundle(modData.Manifest.FluentCulture, modMessages, modData.DefaultFileSystem, _ => { });
			var mapFluentBundle = new FluentBundle(modData.Manifest.FluentCulture, mapMessages, map, error => emitError(error.Message));

			foreach (var group in usedKeys.KeysWithContext)
			{
				if (modFluentBundle.HasMessage(group.Key))
				{
					if (mapFluentBundle.HasMessage(group.Key))
						emitWarning($"Key `{group.Key}` in map ftl files already exists in mod translations and will not be used.");
				}
				else if (!mapFluentBundle.HasMessage(group.Key))
				{
					foreach (var context in group)
						emitWarning($"Missing key `{group.Key}` in map ftl files required by {context}");
				}
			}

			if (map.FluentMessageDefinitions.Nodes.Length > 0)
				emitWarning(
					$"Lint pass ({nameof(CheckFluentReferences)}) lacks the know-how to test inline map fluent messages " +
					"- previous warnings may be incorrect");
		}

		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			Console.WriteLine("Testing Fluent references");
			var usedKeys = ExtractModFluentKeys(modData);
			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in mod translation files required by {context}");

			var modMessages = modData.Manifest.FluentMessages.ToArray();

			// With the fully populated keys, check keys and variables are not missing and not unused across all language files.
			var keyWithAttrs = CheckKeys(modMessages, modData.DefaultFileSystem.Open, usedKeys,
				file =>
					!modData.Manifest.AllowUnusedFluentMessagesInExternalPackages ||
					!modData.DefaultFileSystem.IsExternalFile(file),
				emitError, emitWarning);

			foreach (var group in usedKeys.KeysWithContext)
				if (!keyWithAttrs.Contains(group.Key))
					foreach (var context in group)
						emitWarning($"Missing key `{group.Key}` in mod ftl files required by {context}");
		}

		static void ExtractRulesetFluentKeys(ModData modData, Ruleset rules, Keys keys)
		{
			foreach (var actorInfo in rules.Actors)
				foreach (var ti in actorInfo.Value.TraitInfos<TraitInfo>())
					ExtractFluentKeys(modData, ti, $"Actor `{actorInfo.Key}` trait {ti.GetType().Name[..^4]}", keys);

			foreach (var w in rules.Weapons)
				foreach (var wh in w.Value.Warheads)
					ExtractFluentKeys(modData, wh, $"Weapon `{w.Key}` warhead {wh.GetType().Name[..^7]}", keys);
		}

		static Keys ExtractMapFluentKeys(ModData modData, Map map, Action<string> emitWarning)
		{
			var keys = new Keys();
			ExtractRulesetFluentKeys(modData, map.Rules, keys);

			var luaScriptInfo = map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<LuaScriptInfo>();
			if (luaScriptInfo != null)
			{
				// Matches expressions such as:
				// UserInterface.GetFluentMessage("fluent-key")
				// UserInterface.GetFluentMessage("fluent-key\"with-escape")
				// UserInterface.GetFluentMessage("fluent-key", { ["attribute"] = foo })
				// UserInterface.GetFluentMessage("fluent-key", { ["attribute\"-with-escape"] = foo })
				// UserInterface.GetFluentMessage("fluent-key", { ["attribute1"] = foo, ["attribute2"] = bar })
				// UserInterface.GetFluentMessage("fluent-key", tableVariable)
				// Extracts groups for the 'key' and each 'attr'.
				// If the table isn't inline like in the last example, extracts it as 'variable'.
				const string UserInterfaceFluentMessagePattern =
					@"UserInterface\s*\.\s*GetFluentMessage\s*\(" + // UserInterface.GetFluentMessage(
					@"\s*""(?<key>(?:[^""\\]|\\.)+?)""\s*" + // "fluent-key"
					@"(,\s*({\s*\[\s*""(?<attr>(?:[^""\\]|\\.)*?)""\s*\]\s*=\s*.*?" + // { ["attribute1"] = foo
					@"(\s*,\s*\[\s*""(?<attr>(?:[^""\\]|\\.)*?)""\s*\]\s*=\s*.*?)*\s*}\s*)" + // , ["attribute2"] = bar }
					"|\\s*,\\s*(?<variable>.*?))?" + // tableVariable
					@"\)"; // )
				var fluentMessageRegex = new Regex(UserInterfaceFluentMessagePattern);

				// The script in mods/common/scripts/utils.lua defines some helpers which accept a fluent key
				// Matches expressions such as:
				// AddPrimaryObjective(Player, "fluent-key")
				// AddSecondaryObjective(Player, "fluent-key")
				// AddPrimaryObjective(Player, "fluent-key\"with-escape")
				// Extracts groups for the 'key'.
				const string AddObjectivePattern =
					@"(AddPrimaryObjective|AddSecondaryObjective)\s*\(" + // AddPrimaryObjective(
					@".*?\s*,\s*""(?<key>(?:[^""\\]|\\.)+?)""\s*" + // Player, "fluent-key"
					@"\)"; // )
				var objectiveRegex = new Regex(AddObjectivePattern);

				foreach (var script in luaScriptInfo.Scripts)
				{
					if (!map.TryOpen(script, out var scriptStream))
						continue;

					using (scriptStream)
					{
						var scriptText = scriptStream.ReadAllText();
						IEnumerable<Match> matches = fluentMessageRegex.Matches(scriptText);
						if (luaScriptInfo.Scripts.Contains("utils.lua"))
							matches = matches.Concat(objectiveRegex.Matches(scriptText));

						var references = matches.Select(m =>
						{
							var key = m.Groups["key"].Value.Replace(@"\""", @"""");
							var attrs = m.Groups["attr"].Captures.Select(c => c.Value.Replace(@"\""", @"""")).ToArray();
							var variable = m.Groups["variable"].Value;
							var line = scriptText.Take(m.Index).Count(x => x == '\n') + 1;
							return (Key: key, Attrs: attrs, Variable: variable, Line: line);
						}).ToArray();

						foreach (var (key, attrs, variable, line) in references)
						{
							var context = $"Script {script}:{line}";
							keys.Add(key, new FluentReferenceAttribute(attrs), context);

							if (variable != "")
							{
								var userInterface = typeof(UserInterfaceGlobal).GetCustomAttribute<ScriptGlobalAttribute>().Name;
								const string FluentMessage = nameof(UserInterfaceGlobal.GetFluentMessage);
								emitWarning(
									$"{context} calls {userInterface}.{FluentMessage} with key `{key}` and args passed as `{variable}`." +
									"Inline the args at the callsite for lint analysis.");
							}
						}
					}
				}
			}

			return keys;
		}

		static Keys ExtractModFluentKeys(ModData modData)
		{
			var keys = new Keys();

			// Extract hardcoded core engine references
			ExtractConstFluentKeys(modData, typeof(Game), keys);

			// Extract references from mod.yaml (metadata, server traits, IGlobalModData)
			ExtractFluentKeys(modData, modData.Manifest.Metadata, "mod.yaml", keys);
			foreach (var traitName in modData.Manifest.ServerTraits)
			{
				var traitType = modData.ObjectCreator.FindType(traitName);
				if (traitType != null)
					ExtractConstFluentKeys(modData, traitType, keys);
			}

			var getModule = modData.GetType().GetMethod(nameof(ModData.GetOrNull), []);
			var globalModData = modData.ObjectCreator.GetTypesImplementing<IGlobalModData>()
				.Select(t => getModule?.MakeGenericMethod(t).Invoke(modData, []))
				.Where(x => x != null);

			foreach (var module in globalModData)
				ExtractFluentKeys(modData, module, "mod.yaml", keys);

			// Load screen
			var loadScreenType = modData.ObjectCreator.FindType(modData.Manifest.LoadScreen.Value);
			if (loadScreenType != null)
				ExtractConstFluentKeys(modData, loadScreenType, keys);

			// Traits, Weapons
			ExtractRulesetFluentKeys(modData, modData.DefaultRules, keys);
			foreach (var hotkey in modData.Hotkeys.Definitions)
				ExtractFluentKeys(modData, hotkey, $"Hotkey {hotkey.GetType().Name}", keys);

			// TerrainInfo
			foreach (var terrainInfo in modData.DefaultTerrainInfo.Values)
				ExtractFluentKeys(modData, terrainInfo, $"Tileset {terrainInfo.Id}", keys);

			// Chrome
			ExtractChromeFluentKeys(modData, keys);

			return keys;
		}

		static void ExtractFluentKeys(ModData modData, object o, string prefix, Keys keys)
		{
			var type = o.GetType();
			ExtractConstFluentKeys(modData, type, keys);
			foreach (var f in Utility.GetFields(type))
			{
				var reference = Utility.GetCustomAttributes<FluentReferenceAttribute>(f, true).SingleOrDefault();
				if (reference != null)
					foreach (var key in LintExts.GetFieldValues(o, f, reference.DictionaryReference))
						keys.Add(key, reference, $"{prefix}.{f.Name}");

				var lint = Utility.GetCustomAttributes<IncludeFluentReferencesAttribute>(f, true).SingleOrDefault();
				if (lint != null)
					ExtractChildFluentKeys(modData, lint.DictionaryReference, f.GetValue(o), $"{prefix}.{f.Name}", keys);
			}
		}

		static void ExtractConstFluentKeys(ModData modData, Type t, Keys keys)
		{
			var classReferences = t.GetCustomAttributes<IncludeStaticFluentReferencesAttribute>(true);
			foreach (var classReference in classReferences)
				foreach (var referencedType in classReference.Types)
					ExtractConstFluentKeys(modData, referencedType, keys);

			foreach (var f in t.GetFields(StaticBinding))
			{
				var reference = Utility.GetCustomAttributes<FluentReferenceAttribute>(f, true).SingleOrDefault();
				if (reference != null)
					foreach (var key in LintExts.GetFieldValues(null, f, reference.DictionaryReference))
						keys.Add(key, reference, $"{t.Name}.{f.Name}");

				var lint = Utility.GetCustomAttributes<IncludeFluentReferencesAttribute>(f, true).SingleOrDefault();
				if (lint != null)
					ExtractChildFluentKeys(modData, lint.DictionaryReference, f.GetValue(null), $"{t.Name}.{f.Name}", keys);
			}
		}

		static void ExtractChildFluentKeys(ModData modData, LintDictionaryReference dictionaryReference,
			object fieldValue, string prefix, Keys keys)
		{
			var type = fieldValue.GetType();
			if (typeof(IEnumerable<object>).IsAssignableFrom(type))
				foreach (var o in (IEnumerable<object>)fieldValue)
					ExtractFluentKeys(modData, o, prefix, keys);

			Type dictionaryInterface = null;
			if (type.IsGenericType)
			{
				if (type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
					dictionaryInterface = type;
				else
					dictionaryInterface = type.GetInterface(typeof(IReadOnlyDictionary<,>).FullName);
			}

			if (dictionaryInterface != null)
			{
				// Use an intermediate list to cover the unlikely case where both keys and values are lintable.
				if (dictionaryReference.HasFlag(LintDictionaryReference.Keys))
				{
					IEnumerable fieldKeys = ((IDictionary)fieldValue).Keys;
					if (typeof(IEnumerable<object>).IsAssignableFrom(dictionaryInterface.GenericTypeArguments[0]))
						fieldKeys = ((ICollection<IEnumerable<object>>)fieldKeys).SelectMany(v => v);

					foreach (var k in fieldKeys)
						ExtractFluentKeys(modData, k, prefix, keys);
				}

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values))
				{
					IEnumerable fieldValues = ((IDictionary)fieldValue).Values;
					if (typeof(IEnumerable<object>).IsAssignableFrom(dictionaryInterface.GenericTypeArguments[1]))
						fieldValues = ((ICollection<IEnumerable<object>>)fieldValues).SelectMany(v => v);

					foreach (var v in fieldValues)
						ExtractFluentKeys(modData, v, prefix, keys);
				}
			}
		}

		static void ExtractChromeFluentKeys(ModData modData, Keys usedKeys)
		{
			// Gather all the nodes together for evaluation.
			var chromeLayoutNodes = modData.Manifest.ChromeLayout
				.SelectMany(filename => MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename))
				.ToArray();

			var widgetTypes = modData.ObjectCreator.GetTypes()
				.Where(t => t.Name.EndsWith("Widget", StringComparison.InvariantCulture) && t.IsSubclassOf(typeof(Widget)))
				.ToList();

			var fluentReferencesByWidgetField = widgetTypes.SelectMany(t =>
				{
					var widgetName = t.Name[..^6];
					return Utility.GetFields(t)
						.Select(f =>
						{
							var attribute = Utility.GetCustomAttributes<FluentReferenceAttribute>(f, true).SingleOrDefault();
							return (WidgetName: widgetName, FieldName: f.Name, FluentReference: attribute);
						})
						.Where(x => x.FluentReference != null);
				})
				.ToDictionary(
					x => (x.WidgetName, x.FieldName),
					x => x.FluentReference);

			foreach (var node in chromeLayoutNodes)
				ExtractChromeFluentKeys(modData, node, fluentReferencesByWidgetField, usedKeys);
		}

		static void ExtractChromeFluentKeys(
			ModData modData,
			MiniYamlNode rootNode,
			Dictionary<(string WidgetName, string FieldName), FluentReferenceAttribute> fluentReferencesByWidgetField,
			Keys keys)
		{
			var nodeType = rootNode.Key.Split('@')[0];
			foreach (var childNode in rootNode.Value.Nodes)
			{
				var childType = childNode.Key.Split('@')[0];
				if (!fluentReferencesByWidgetField.TryGetValue((nodeType, childType), out var reference))
					continue;

				var key = childNode.Value.Value;
				keys.Add(key, reference, $"Widget `{rootNode.Key}` field `{childType}` in {rootNode.Location}");
			}

			var widgetType = modData.ObjectCreator.FindType(nodeType + "Widget");
			ExtractConstFluentKeys(modData, widgetType, keys);

			Type[] logicArgsTypes = [typeof(Dictionary<string, MiniYaml>)];
			foreach (var childNode in rootNode.Value.Nodes)
			{
				if (childNode.Key == "Logic")
				{
					foreach (var logicName in FieldLoader.GetValue<ImmutableArray<string>>(childNode.Key, childNode.Value.Value))
					{
						var logicType = modData.ObjectCreator.FindType(logicName);
						if (logicType == null)
							continue;

						ExtractConstFluentKeys(modData, logicType, keys);

						var chromeArgsReferences = logicType.GetCustomAttributes<IncludeChromeLogicArgsFluentReferencesAttribute>(true);
						foreach (var methodName in chromeArgsReferences.SelectMany(a => a.MethodNames))
						{
							var dynamicReferencesMethod = logicType.GetMethod(methodName, StaticBinding, logicArgsTypes);
							var dynamicReferences = dynamicReferencesMethod.Invoke(null, [childNode.Value.ToDictionary()]);
							foreach (var (key, reference) in (IEnumerable<(string Key, FluentReferenceAttribute Reference)>)dynamicReferences)
								keys.Add(key, reference, logicType.Name);
						}
					}
				}

				if (childNode.Key == "Children")
					foreach (var n in childNode.Value.Nodes)
						ExtractChromeFluentKeys(modData, n, fluentReferencesByWidgetField, keys);
			}
		}

		static HashSet<string> CheckKeys(
			IEnumerable<string> paths, Func<string, Stream> openFile, Keys usedKeys,
			Func<string, bool> checkUnusedKeysForFile, Action<string> emitError, Action<string> emitWarning)
		{
			var keyWithAttrs = new HashSet<string>();
			foreach (var path in paths)
			{
				var stream = openFile(path);
				using (var reader = new StreamReader(stream))
				{
					var parser = new LinguiniParser(reader);
					var result = parser.Parse();

					foreach (var entry in result.Entries)
					{
						if (entry is not AstMessage message)
							continue;

						IEnumerable<(Pattern Node, string AttributeName)> nodeAndAttributeNames;
						if (message.Attributes.Count == 0)
							nodeAndAttributeNames = [(message.Value, null)];
						else
							nodeAndAttributeNames = message.Attributes.Select(a => (a.Value, a.Id.Name.ToString()));

						var key = message.GetId();
						foreach (var (node, attributeName) in nodeAndAttributeNames)
						{
							keyWithAttrs.Add(attributeName == null ? key : $"{key}.{attributeName}");
							if (checkUnusedKeysForFile(path))
								CheckUnusedKey(key, attributeName, path, usedKeys, emitWarning);
							CheckVariables(node, key, attributeName, path, usedKeys, emitError, emitWarning);
						}
					}
				}
			}

			return keyWithAttrs;

			static void CheckUnusedKey(string key, string attribute, string file, Keys usedKeys, Action<string> emitWarning)
			{
				var isAttribute = !string.IsNullOrEmpty(attribute);
				var keyWithAtrr = isAttribute ? $"{key}.{attribute}" : key;

				if (!usedKeys.Contains(keyWithAtrr))
					emitWarning(isAttribute ?
						$"Unused attribute `{attribute}` of key `{key}` in {file}" :
						$"Unused key `{key}` in {file}");
			}

			static void CheckVariables(
				Pattern node, string key, string attribute, string file, Keys usedKeys,
				Action<string> emitError, Action<string> emitWarning)
			{
				var isAttribute = !string.IsNullOrEmpty(attribute);
				var keyWithAtrr = isAttribute ? $"{key}.{attribute}" : key;

				if (!usedKeys.TryGetRequiredVariables(keyWithAtrr, out var requiredVariables))
					return;

				var variableNames = new HashSet<string>();
				foreach (var element in node.Elements)
				{
					if (element is not Placeable placeable)
						continue;

					AddVariableAndCheckUnusedVariable(placeable);
					if (placeable.Expression is SelectExpression selectExpression)
						foreach (var variant in selectExpression.Variants)
							foreach (var variantElement in variant.Value.Elements)
								if (variantElement is Placeable variantPlaceable)
									AddVariableAndCheckUnusedVariable(variantPlaceable);
				}

				void AddVariableAndCheckUnusedVariable(Placeable placeable)
				{
					if (placeable.Expression is not IInlineExpression inlineExpression ||
						inlineExpression is not VariableReference variableReference)
						return;

					var name = variableReference.Id.Name.ToString();
					variableNames.Add(name);

					if (!requiredVariables.Contains(name))
						emitWarning(isAttribute ?
							$"Unused variable `{name}` for attribute `{attribute}` of key `{key}` in {file}" :
							$"Unused variable `{name}` for key `{key}` in {file}");
				}

				foreach (var name in requiredVariables)
					if (!variableNames.Contains(name))
						emitError(isAttribute ?
							$"Missing variable `{name}` for attribute `{attribute}` of key `{key}` in {file}" :
							$"Missing variable `{name}` for key `{key}` in {file}");
			}
		}

		sealed class Keys
		{
			readonly HashSet<string> keys = [];
			readonly List<(string Key, string Context)> keysWithContext = [];
			readonly Dictionary<string, HashSet<string>> requiredVariablesByKey = [];
			readonly List<string> contextForEmptyKeys = [];

			public void Add(string key, FluentReferenceAttribute fluentReference, string context)
			{
				if (key == null)
				{
					if (!fluentReference.Optional)
						contextForEmptyKeys.Add(context);
					return;
				}

				if (fluentReference.RequiredVariableNames != null && fluentReference.RequiredVariableNames.Length > 0)
				{
					var rv = requiredVariablesByKey.GetOrAdd(key, _ => []);
					rv.UnionWith(fluentReference.RequiredVariableNames);
				}

				keys.Add(key);
				keysWithContext.Add((key, context));
			}

			public bool TryGetRequiredVariables(string key, out ISet<string> requiredVariables)
			{
				if (requiredVariablesByKey.TryGetValue(key, out var rv))
				{
					requiredVariables = rv;
					return true;
				}

				requiredVariables = null;
				return false;
			}

			public bool Contains(string key)
			{
				return keys.Contains(key);
			}

			public ILookup<string, string> KeysWithContext => keysWithContext.OrderBy(x => x.Key).ToLookup(x => x.Key, x => x.Context);

			public IEnumerable<string> EmptyKeyContexts => contextForEmptyKeys;
		}
	}
}
