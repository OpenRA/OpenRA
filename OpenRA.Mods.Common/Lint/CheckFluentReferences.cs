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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Linguini.Syntax.Ast;
using Linguini.Syntax.Parser;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Scripting.Global;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Scripting;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Lint
{
	sealed class CheckFluentReferences : ILintPass, ILintMapPass
	{
		static readonly Regex FilenameRegex = new(@"(?<language>[^\/\\]+)\.ftl$");

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.TranslationDefinitions == null)
				return;

			var usedKeys = GetUsedFluentKeysInMap(map, emitWarning);

			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in map ftl files required by {context}");

			var mapTranslations = FieldLoader.GetValue<string[]>("value", map.TranslationDefinitions.Value);

			foreach (var language in GetModLanguages(modData))
			{
				// Check keys and variables are not missing across all language files.
				// But for maps we don't warn on unused keys. They might be unused on *this* map,
				// but the mod or another map may use them and we don't have sight of that.
				CheckKeys(
					modData.Manifest.Translations.Concat(mapTranslations), map.Open, usedKeys,
					language, _ => false, emitError, emitWarning);

				var modFluentBundle = new FluentBundle(language, modData.Manifest.Translations, modData.DefaultFileSystem, _ => { });
				var mapFluentBundle = new FluentBundle(language, mapTranslations, map, error => emitError(error.Message));

				foreach (var group in usedKeys.KeysWithContext)
				{
					if (modFluentBundle.HasMessage(group.Key))
					{
						if (mapFluentBundle.HasMessage(group.Key))
							emitWarning($"Key `{group.Key}` in `{language}` language in map ftl files already exists in mod translations and will not be used.");
					}
					else if (!mapFluentBundle.HasMessage(group.Key))
					{
						foreach (var context in group)
							emitWarning($"Missing key `{group.Key}` in `{language}` language in map ftl files required by {context}");
					}
				}
			}
		}

		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			var (usedKeys, testedFields) = GetUsedFluentKeysInMod(modData);

			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in mod translation files required by {context}");

			foreach (var language in GetModLanguages(modData))
			{
				Console.WriteLine($"Testing language: {language}");
				CheckModWidgets(modData, usedKeys, testedFields);

				// With the fully populated keys, check keys and variables are not missing and not unused across all language files.
				var keyWithAttrs = CheckKeys(
					modData.Manifest.Translations, modData.DefaultFileSystem.Open, usedKeys,
					language,
					file =>
						!modData.Manifest.AllowUnusedTranslationsInExternalPackages ||
						!modData.DefaultFileSystem.IsExternalFile(file),
					emitError, emitWarning);

				foreach (var group in usedKeys.KeysWithContext)
				{
					if (keyWithAttrs.Contains(group.Key))
						continue;

					foreach (var context in group)
						emitWarning($"Missing key `{group.Key}` in `{language}` language in mod ftl files required by {context}");
				}
			}

			// Check if we couldn't test any fields.
			const BindingFlags Binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			var allFluentFields = modData.ObjectCreator.GetTypes().SelectMany(t =>
				t.GetFields(Binding).Where(m => Utility.HasAttribute<FluentReferenceAttribute>(m))).ToArray();
			var untestedFields = allFluentFields.Except(testedFields);
			foreach (var field in untestedFields)
				emitError(
					$"Lint pass ({nameof(CheckFluentReferences)}) lacks the know-how to test translatable field " +
					$"`{field.ReflectedType.Name}.{field.Name}` - previous warnings may be incorrect");
		}

		static IEnumerable<string> GetModLanguages(ModData modData)
		{
			return modData.Manifest.Translations
				.Select(filename => FilenameRegex.Match(filename).Groups["language"].Value)
				.Distinct()
				.OrderBy(l => l);
		}

		static Keys GetUsedFluentKeysInRuleset(Ruleset rules)
		{
			var usedKeys = new Keys();
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var traitType = traitInfo.GetType();
					foreach (var field in Utility.GetFields(traitType))
					{
						var fluentReference = Utility.GetCustomAttributes<FluentReferenceAttribute>(field, true).SingleOrDefault();
						if (fluentReference == null)
							continue;

						foreach (var key in LintExts.GetFieldValues(traitInfo, field, fluentReference.DictionaryReference))
							usedKeys.Add(key, fluentReference, $"Actor `{actorInfo.Key}` trait `{traitType.Name[..^4]}.{field.Name}`");
					}
				}
			}

			foreach (var weapon in rules.Weapons)
			{
				foreach (var warhead in weapon.Value.Warheads)
				{
					var warheadType = warhead.GetType();
					foreach (var field in Utility.GetFields(warheadType))
					{
						var fluentReference = Utility.GetCustomAttributes<FluentReferenceAttribute>(field, true).SingleOrDefault();
						if (fluentReference == null)
							continue;

						foreach (var key in LintExts.GetFieldValues(warhead, field, fluentReference.DictionaryReference))
							usedKeys.Add(key, fluentReference, $"Weapon `{weapon.Key}` warhead `{warheadType.Name[..^7]}.{field.Name}`");
					}
				}
			}

			return usedKeys;
		}

		static Keys GetUsedFluentKeysInMap(Map map, Action<string> emitWarning)
		{
			var usedKeys = GetUsedFluentKeysInRuleset(map.Rules);

			var luaScriptInfo = map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<LuaScriptInfo>();
			if (luaScriptInfo != null)
			{
				// Matches expressions such as:
				// UserInterface.Translate("fluent-key")
				// UserInterface.Translate("fluent-key\"with-escape")
				// UserInterface.Translate("fluent-key", { ["attribute"] = foo })
				// UserInterface.Translate("fluent-key", { ["attribute\"-with-escape"] = foo })
				// UserInterface.Translate("fluent-key", { ["attribute1"] = foo, ["attribute2"] = bar })
				// UserInterface.Translate("fluent-key", tableVariable)
				// Extracts groups for the 'key' and each 'attr'.
				// If the table isn't inline like in the last example, extracts it as 'variable'.
				const string UserInterfaceTranslatePattern =
					@"UserInterface\s*\.\s*Translate\s*\(" + // UserInterface.Translate(
					@"\s*""(?<key>(?:[^""\\]|\\.)+?)""\s*" + // "fluent-key"
					@"(,\s*({\s*\[\s*""(?<attr>(?:[^""\\]|\\.)*?)""\s*\]\s*=\s*.*?" + // { ["attribute1"] = foo
					@"(\s*,\s*\[\s*""(?<attr>(?:[^""\\]|\\.)*?)""\s*\]\s*=\s*.*?)*\s*}\s*)" + // , ["attribute2"] = bar }
					"|\\s*,\\s*(?<variable>.*?))?" + // tableVariable
					@"\)"; // )
				var translateRegex = new Regex(UserInterfaceTranslatePattern);

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
						IEnumerable<Match> matches = translateRegex.Matches(scriptText);
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
							usedKeys.Add(key, new FluentReferenceAttribute(attrs), context);

							if (variable != "")
							{
								var userInterface = typeof(UserInterfaceGlobal).GetCustomAttribute<ScriptGlobalAttribute>().Name;
								const string Translate = nameof(UserInterfaceGlobal.Translate);
								emitWarning(
									$"{context} calls {userInterface}.{Translate} with key `{key}` and translate args passed as `{variable}`." +
									"Inline the args at the callsite for lint analysis.");
							}
						}
					}
				}
			}

			return usedKeys;
		}

		static (Keys UsedKeys, List<FieldInfo> TestedFields) GetUsedFluentKeysInMod(ModData modData)
		{
			var usedKeys = GetUsedFluentKeysInRuleset(modData.DefaultRules);
			var testedFields = new List<FieldInfo>();
			testedFields.AddRange(
				modData.ObjectCreator.GetTypes()
				.Where(t => t.IsSubclassOf(typeof(TraitInfo)) || t.IsSubclassOf(typeof(Warhead)))
				.SelectMany(t => t.GetFields().Where(f => f.HasAttribute<FluentReferenceAttribute>())));

			// HACK: Need to hardcode the custom loader for GameSpeeds.
			var gameSpeeds = modData.Manifest.Get<GameSpeeds>();
			var gameSpeedNameField = typeof(GameSpeed).GetField(nameof(GameSpeed.Name));
			var gameSpeedFluentReference = Utility.GetCustomAttributes<FluentReferenceAttribute>(gameSpeedNameField, true)[0];
			testedFields.Add(gameSpeedNameField);
			foreach (var speed in gameSpeeds.Speeds.Values)
				usedKeys.Add(speed.Name, gameSpeedFluentReference, $"`{nameof(GameSpeed)}.{nameof(GameSpeed.Name)}`");

			// TODO: linter does not work with LoadUsing
			foreach (var actorInfo in modData.DefaultRules.Actors)
			{
				foreach (var info in actorInfo.Value.TraitInfos<ResourceRendererInfo>())
				{
					var resourceTypeNameField = typeof(ResourceRendererInfo.ResourceTypeInfo).GetField(nameof(ResourceRendererInfo.ResourceTypeInfo.Name));
					var resourceTypeFluentReference = Utility.GetCustomAttributes<FluentReferenceAttribute>(resourceTypeNameField, true)[0];
					testedFields.Add(resourceTypeNameField);
					foreach (var resourceTypes in info.ResourceTypes)
						usedKeys.Add(
							resourceTypes.Value.Name,
							resourceTypeFluentReference,
							$"`{nameof(ResourceRendererInfo.ResourceTypeInfo)}.{nameof(ResourceRendererInfo.ResourceTypeInfo.Name)}`");
				}
			}

			foreach (var modType in modData.ObjectCreator.GetTypes())
			{
				const BindingFlags Binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
				foreach (var field in modType.GetFields(Binding))
				{
					// Checking for constant string fields.
					if (!field.IsLiteral)
						continue;

					var fluentReference = Utility.GetCustomAttributes<FluentReferenceAttribute>(field, true).SingleOrDefault();
					if (fluentReference == null)
						continue;

					testedFields.Add(field);
					var keys = LintExts.GetFieldValues(null, field, fluentReference.DictionaryReference);
					foreach (var key in keys)
						usedKeys.Add(key, fluentReference, $"`{field.ReflectedType.Name}.{field.Name}`");
				}
			}

			return (usedKeys, testedFields);
		}

		static void CheckModWidgets(ModData modData, Keys usedKeys, List<FieldInfo> testedFields)
		{
			var chromeLayoutNodes = BuildChromeTree(modData);

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

			testedFields.AddRange(widgetTypes.SelectMany(
				t => Utility.GetFields(t).Where(Utility.HasAttribute<FluentReferenceAttribute>)));

			foreach (var node in chromeLayoutNodes)
				CheckChrome(node, fluentReferencesByWidgetField, usedKeys);
		}

		static MiniYamlNode[] BuildChromeTree(ModData modData)
		{
			// Gather all the nodes together for evaluation.
			var chromeLayoutNodes = modData.Manifest.ChromeLayout
				.SelectMany(filename => MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename))
				.ToArray();

			return chromeLayoutNodes;
		}

		static void CheckChrome(
			MiniYamlNode rootNode,
			Dictionary<(string WidgetName, string FieldName), FluentReferenceAttribute> fluentReferencesByWidgetField,
			Keys usedKeys)
		{
			var nodeType = rootNode.Key.Split('@')[0];
			foreach (var childNode in rootNode.Value.Nodes)
			{
				var childType = childNode.Key.Split('@')[0];
				if (!fluentReferencesByWidgetField.TryGetValue((nodeType, childType), out var reference))
					continue;

				var key = childNode.Value.Value;
				usedKeys.Add(key, reference, $"Widget `{rootNode.Key}` field `{childType}` in {rootNode.Location}");
			}

			foreach (var childNode in rootNode.Value.Nodes)
				if (childNode.Key == "Children")
					foreach (var n in childNode.Value.Nodes)
						CheckChrome(n, fluentReferencesByWidgetField, usedKeys);
		}

		static HashSet<string> CheckKeys(
			IEnumerable<string> paths, Func<string, Stream> openFile, Keys usedKeys,
			string language, Func<string, bool> checkUnusedKeysForFile,
			Action<string> emitError, Action<string> emitWarning)
		{
			var keyWithAttrs = new HashSet<string>();
			foreach (var path in paths)
			{
				if (!path.EndsWith($"{language}.ftl", StringComparison.Ordinal))
					continue;

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
							nodeAndAttributeNames = new[] { (message.Value, (string)null) };
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

		class Keys
		{
			readonly HashSet<string> keys = new();
			readonly List<(string Key, string Context)> keysWithContext = new();
			readonly Dictionary<string, HashSet<string>> requiredVariablesByKey = new();
			readonly List<string> contextForEmptyKeys = new();

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
					var rv = requiredVariablesByKey.GetOrAdd(key, _ => new HashSet<string>());
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
