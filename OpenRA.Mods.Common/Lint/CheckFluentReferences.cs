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
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Scripting;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Lint
{
	sealed class CheckFluentReferences : ILintPass, ILintMapPass
	{
		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.FluentMessageDefinitions == null)
				return;

			var usedKeys = GetUsedFluentKeysInMap(map, emitWarning);

			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in map ftl files required by {context}");

			var mapMessages = FieldLoader.GetValue<string[]>("value", map.FluentMessageDefinitions.Value);
			var modMessages = modData.Manifest.FluentMessages;

			// For maps we don't warn on unused keys. They might be unused on *this* map,
			// but the mod or another map may use them and we don't have sight of that.
			CheckKeys(modMessages.Concat(mapMessages), map.Open, usedKeys,
				_ => false, emitError, emitWarning);

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
			var (usedKeys, testedFields) = GetUsedFluentKeysInMod(modData);

			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in mod translation files required by {context}");

			var modMessages = modData.Manifest.FluentMessages.ToArray();
			CheckModWidgets(modData, usedKeys, testedFields);

			// With the fully populated keys, check keys and variables are not missing and not unused across all language files.
			var keyWithAttrs = CheckKeys(
				modMessages, modData.DefaultFileSystem.Open, usedKeys,
				file =>
					!modData.Manifest.AllowUnusedFluentMessagesInExternalPackages ||
					!modData.DefaultFileSystem.IsExternalFile(file),
				emitError, emitWarning);

			foreach (var group in usedKeys.KeysWithContext)
			{
				if (keyWithAttrs.Contains(group.Key))
					continue;

				foreach (var context in group)
					emitWarning($"Missing key `{group.Key}` in mod ftl files required by {context}");
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
							usedKeys.Add(key, new FluentReferenceAttribute(attrs), context);

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

			return usedKeys;
		}

		static (Keys UsedKeys, List<FieldInfo> TestedFields) GetUsedFluentKeysInMod(ModData modData)
		{
			var usedKeys = GetUsedFluentKeysInRuleset(modData.DefaultRules);
			var testedFields = new List<FieldInfo>();
			testedFields.AddRange(
				modData.ObjectCreator.GetTypes()
				.Where(t => t.IsSubclassOf(typeof(TraitInfo)) || t.IsSubclassOf(typeof(Warhead)))
				.SelectMany(t => Utility.GetFields(t).Where(Utility.HasAttribute<FluentReferenceAttribute>)));

			// TODO: linter does not work with LoadUsing
			foreach (var speed in modData.Manifest.Get<GameSpeeds>().Speeds)
				GetUsedFluentKeys(
					usedKeys, testedFields,
					Utility.GetFields(typeof(GameSpeed)),
					[speed.Value],
					(obj, field) => $"`GameSpeeds.Speeds.{speed.Key}.{field.Name}` in mod.yaml");

			// TODO: linter does not work with LoadUsing
			foreach (var resource in modData.DefaultRules.Actors
				.SelectMany(actorInfo => actorInfo.Value.TraitInfos<ResourceRendererInfo>())
				.SelectMany(info => info.ResourceTypes))
				GetUsedFluentKeys(
					usedKeys, testedFields,
					Utility.GetFields(typeof(ResourceRendererInfo.ResourceTypeInfo)),
					[resource.Value],
					(obj, field) => $"`ResourceRenderer.ResourceTypes.{resource.Key}.{field.Name}` in rules yaml");

			const BindingFlags Binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			var constFields = modData.ObjectCreator.GetTypes().SelectMany(modType => modType.GetFields(Binding)).Where(f => f.IsLiteral);
			GetUsedFluentKeys(
				usedKeys, testedFields,
				constFields,
				[(object)null],
				(obj, field) => $"`{field.ReflectedType.Name}.{field.Name}`");

			var modMetadataFields = typeof(ModMetadata).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			GetUsedFluentKeys(
				usedKeys, testedFields,
				modMetadataFields,
				[modData.Manifest.Metadata],
				(obj, field) => $"`Metadata.{field.Name}` in mod.yaml");

			var modContent = modData.Manifest.Get<ModContent>();
			GetUsedFluentKeys(
				usedKeys, testedFields,
				Utility.GetFields(typeof(ModContent)),
				[modContent],
				(obj, field) => $"`ModContent.{field.Name}` in mod.yaml");
			GetUsedFluentKeys(
				usedKeys, testedFields,
				Utility.GetFields(typeof(ModContent.ModPackage)),
				modContent.Packages.Values.ToArray(),
				(obj, field) => $"`ModContent.Packages.ContentPackage.{field.Name}` in mod.yaml");

			GetUsedFluentKeys(
				usedKeys, testedFields,
				Utility.GetFields(typeof(HotkeyDefinition)),
				modData.Hotkeys.Definitions,
				(obj, field) => $"`{obj.Name}.{field.Name}` in hotkeys yaml");

			// All keycodes and modifiers should be marked as used, as they can all be configured for use at hotkeys at runtime.
			GetUsedFluentKeys(
				usedKeys, testedFields,
				Utility.GetFields(typeof(KeycodeExts)).Concat(Utility.GetFields(typeof(ModifiersExts))),
				[(object)null],
				(obj, field) => $"`{field.ReflectedType.Name}.{field.Name}`");

			foreach (var filename in modData.Manifest.ChromeLayout)
				CheckHotkeysSettingsLogic(usedKeys, MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename));

			static void CheckHotkeysSettingsLogic(Keys usedKeys, IEnumerable<MiniYamlNode> nodes)
			{
				foreach (var node in nodes)
				{
					if (node.Value.Nodes != null)
						CheckHotkeysSettingsLogic(usedKeys, node.Value.Nodes);

					if (node.Key != "Logic" || node?.Value.Value != "HotkeysSettingsLogic")
						continue;

					var hotkeyGroupsNode = node.Value.NodeWithKeyOrDefault("HotkeyGroups");
					if (hotkeyGroupsNode == null)
						continue;

					var hotkeyGroupsKeys = hotkeyGroupsNode?.Value.Nodes.Select(n => n.Key);
					foreach (var key in hotkeyGroupsKeys)
						usedKeys.Add(key, new FluentReferenceAttribute(), $"`{nameof(HotkeysSettingsLogic)}.HotkeyGroups`");
				}
			}

			return (usedKeys, testedFields);
		}

		static void GetUsedFluentKeys<T>(
			Keys usedKeys, List<FieldInfo> testedFields,
			IEnumerable<FieldInfo> newFields, IEnumerable<T> objects,
			Func<T, FieldInfo, string> getContext)
		{
			var fieldsWithAttribute =
				newFields
					.Select(f => (Field: f, FluentReference: Utility.GetCustomAttributes<FluentReferenceAttribute>(f, true).SingleOrDefault()))
					.Where(x => x.FluentReference != null)
					.ToArray();
			testedFields.AddRange(fieldsWithAttribute.Select(x => x.Field));
			foreach (var obj in objects)
			{
				foreach (var (field, fluentReference) in fieldsWithAttribute)
				{
					var keys = LintExts.GetFieldValues(obj, field, fluentReference.DictionaryReference);
					foreach (var key in keys)
						usedKeys.Add(key, fluentReference, getContext(obj, field));
				}
			}
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
