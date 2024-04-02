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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Lint
{
	sealed class CheckTranslationReference : ILintPass, ILintMapPass
	{
		static readonly Regex TranslationFilenameRegex = new(@"(?<language>[^\/\\]+)\.ftl$");

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.TranslationDefinitions == null)
				return;

			var usedKeys = GetUsedTranslationKeysInRuleset(map.Rules);

			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in map translation files required by {context}");

			var mapTranslations = FieldLoader.GetValue<string[]>("value", map.TranslationDefinitions.Value);

			foreach (var language in GetTranslationLanguages(modData))
			{
				var modTranslation = new Translation(language, modData.Manifest.Translations, modData.DefaultFileSystem, _ => { });
				var mapTranslation = new Translation(language, mapTranslations, map, error => emitError(error.Message));

				foreach (var group in usedKeys.KeysWithContext)
				{
					if (modTranslation.HasMessage(group.Key))
					{
						if (mapTranslation.HasMessage(group.Key))
							emitWarning($"Key `{group.Key}` in `{language}` language in map translation files already exists in mod translations and will not be used.");
					}
					else if (!mapTranslation.HasMessage(group.Key))
					{
						foreach (var context in group)
							emitWarning($"Missing key `{group.Key}` in `{language}` language in map translation files required by {context}");
					}
				}
			}
		}

		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			var (usedKeys, testedFields) = GetUsedTranslationKeysInMod(modData);

			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in mod translation files required by {context}");

			foreach (var language in GetTranslationLanguages(modData))
			{
				Console.WriteLine($"Testing translation: {language}");
				var translation = new Translation(language, modData.Manifest.Translations, modData.DefaultFileSystem, error => emitError(error.Message));
				CheckModWidgets(modData, usedKeys, testedFields);
			}

			// With the fully populated keys, check keys and variables are not missing and not unused across all language files.
			CheckModTranslationFiles(modData, usedKeys, emitError, emitWarning);

			// Check if we couldn't test any fields.
			const BindingFlags Binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			var allTranslatableFields = modData.ObjectCreator.GetTypes().SelectMany(t =>
				t.GetFields(Binding).Where(m => Utility.HasAttribute<TranslationReferenceAttribute>(m))).ToArray();
			var untestedFields = allTranslatableFields.Except(testedFields);
			foreach (var field in untestedFields)
				emitError(
					$"Lint pass ({nameof(CheckTranslationReference)}) lacks the know-how to test translatable field " +
					$"`{field.ReflectedType.Name}.{field.Name}` - previous warnings may be incorrect");
		}

		static IEnumerable<string> GetTranslationLanguages(ModData modData)
		{
			return modData.Manifest.Translations
				.Select(filename => TranslationFilenameRegex.Match(filename).Groups["language"].Value)
				.Distinct()
				.OrderBy(l => l);
		}

		static TranslationKeys GetUsedTranslationKeysInRuleset(Ruleset rules)
		{
			var usedKeys = new TranslationKeys();
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var traitType = traitInfo.GetType();
					foreach (var field in Utility.GetFields(traitType))
					{
						var translationReference = Utility.GetCustomAttributes<TranslationReferenceAttribute>(field, true).SingleOrDefault();
						if (translationReference == null)
							continue;

						var keys = LintExts.GetFieldValues(traitInfo, field, translationReference.DictionaryReference);
						foreach (var key in keys)
							usedKeys.Add(key, translationReference, $"Actor `{actorInfo.Key}` trait `{traitType.Name[..^4]}.{field.Name}`");
					}
				}
			}

			return usedKeys;
		}

		static (TranslationKeys UsedKeys, List<FieldInfo> TestedFields) GetUsedTranslationKeysInMod(ModData modData)
		{
			var usedKeys = GetUsedTranslationKeysInRuleset(modData.DefaultRules);
			var testedFields = new List<FieldInfo>();
			testedFields.AddRange(
				modData.ObjectCreator.GetTypes()
				.Where(t => t.IsSubclassOf(typeof(TraitInfo)))
				.SelectMany(t => t.GetFields().Where(f => f.HasAttribute<TranslationReferenceAttribute>())));

			// HACK: Need to hardcode the custom loader for GameSpeeds.
			var gameSpeeds = modData.Manifest.Get<GameSpeeds>();
			var gameSpeedNameField = typeof(GameSpeed).GetField(nameof(GameSpeed.Name));
			var gameSpeedTranslationReference = Utility.GetCustomAttributes<TranslationReferenceAttribute>(gameSpeedNameField, true)[0];
			testedFields.Add(gameSpeedNameField);
			foreach (var speed in gameSpeeds.Speeds.Values)
				usedKeys.Add(speed.Name, gameSpeedTranslationReference, $"`{nameof(GameSpeed)}.{nameof(GameSpeed.Name)}`");

			// TODO: linter does not work with LoadUsing
			foreach (var actorInfo in modData.DefaultRules.Actors)
			{
				foreach (var info in actorInfo.Value.TraitInfos<ResourceRendererInfo>())
				{
					var resourceTypeNameField = typeof(ResourceRendererInfo.ResourceTypeInfo).GetField(nameof(ResourceRendererInfo.ResourceTypeInfo.Name));
					var resourceTypeTranslationReference = Utility.GetCustomAttributes<TranslationReferenceAttribute>(resourceTypeNameField, true)[0];
					testedFields.Add(resourceTypeNameField);
					foreach (var resourceTypes in info.ResourceTypes)
						usedKeys.Add(resourceTypes.Value.Name, resourceTypeTranslationReference, $"`{nameof(ResourceRendererInfo.ResourceTypeInfo)}.{nameof(ResourceRendererInfo.ResourceTypeInfo.Name)}`");
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

					var translationReference = Utility.GetCustomAttributes<TranslationReferenceAttribute>(field, true).SingleOrDefault();
					if (translationReference == null)
						continue;

					testedFields.Add(field);
					var keys = LintExts.GetFieldValues(null, field, translationReference.DictionaryReference);
					foreach (var key in keys)
						usedKeys.Add(key, translationReference, $"`{field.ReflectedType.Name}.{field.Name}`");
				}
			}

			return (usedKeys, testedFields);
		}

		static void CheckModWidgets(ModData modData, TranslationKeys usedKeys, List<FieldInfo> testedFields)
		{
			var chromeLayoutNodes = BuildChromeTree(modData);

			var widgetTypes = modData.ObjectCreator.GetTypes()
				.Where(t => t.Name.EndsWith("Widget", StringComparison.InvariantCulture) && t.IsSubclassOf(typeof(Widget)))
				.ToList();
			var translationReferencesByWidgetField = widgetTypes.SelectMany(t =>
				{
					var widgetName = t.Name[..^6];
					return Utility.GetFields(t)
						.Select(f =>
						{
							var attribute = Utility.GetCustomAttributes<TranslationReferenceAttribute>(f, true).SingleOrDefault();
							return (WidgetName: widgetName, FieldName: f.Name, TranslationReference: attribute);
						})
						.Where(x => x.TranslationReference != null);
				})
				.ToDictionary(
					x => (x.WidgetName, x.FieldName),
					x => x.TranslationReference);

			testedFields.AddRange(widgetTypes.SelectMany(
				t => Utility.GetFields(t).Where(Utility.HasAttribute<TranslationReferenceAttribute>)));

			foreach (var node in chromeLayoutNodes)
				CheckChrome(node, translationReferencesByWidgetField, usedKeys);
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
			Dictionary<(string WidgetName, string FieldName), TranslationReferenceAttribute> translationReferencesByWidgetField,
			TranslationKeys usedKeys)
		{
			var nodeType = rootNode.Key.Split('@')[0];
			foreach (var childNode in rootNode.Value.Nodes)
			{
				var childType = childNode.Key.Split('@')[0];
				if (!translationReferencesByWidgetField.TryGetValue((nodeType, childType), out var translationReference))
					continue;

				var key = childNode.Value.Value;
				usedKeys.Add(key, translationReference, $"Widget `{rootNode.Key}` field `{childType}` in {rootNode.Location}");
			}

			foreach (var childNode in rootNode.Value.Nodes)
				if (childNode.Key == "Children")
					foreach (var n in childNode.Value.Nodes)
						CheckChrome(n, translationReferencesByWidgetField, usedKeys);
		}

		static void CheckModTranslationFiles(ModData modData, TranslationKeys usedKeys, Action<string> emitError, Action<string> emitWarning)
		{
			foreach (var language in GetTranslationLanguages(modData))
			{
				var keyWithAttrs = new HashSet<string>();
				foreach (var file in modData.Manifest.Translations)
				{
					if (!file.EndsWith($"{language}.ftl", StringComparison.Ordinal))
						continue;

					var stream = modData.DefaultFileSystem.Open(file);
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
								CheckUnusedKey(key, attributeName, file, usedKeys, emitWarning);
								CheckVariables(node, key, attributeName, file, usedKeys, emitError, emitWarning);
							}
						}
					}
				}

				foreach (var group in usedKeys.KeysWithContext)
				{
					if (keyWithAttrs.Contains(group.Key))
						continue;

					foreach (var context in group)
						emitWarning($"Missing key `{group.Key}` in `{language}` language in mod translation files required by {context}");
				}
			}

			static void CheckUnusedKey(string key, string attribute, string file, TranslationKeys usedKeys, Action<string> emitWarning)
			{
				var isAttribute = !string.IsNullOrEmpty(attribute);
				var keyWithAtrr = isAttribute ? $"{key}.{attribute}" : key;

				if (!usedKeys.Contains(keyWithAtrr))
					emitWarning(isAttribute ?
						$"Unused attribute `{attribute}` of key `{key}` in {file}" :
						$"Unused key `{key}` in {file}");
			}

			static void CheckVariables(
				Pattern node, string key, string attribute, string file, TranslationKeys usedKeys,
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

		class TranslationKeys
		{
			readonly HashSet<string> keys = new();
			readonly List<(string Key, string Context)> keysWithContext = new();
			readonly Dictionary<string, HashSet<string>> requiredVariablesByKey = new();
			readonly List<string> contextForEmptyKeys = new();

			public void Add(string key, TranslationReferenceAttribute translationReference, string context)
			{
				if (key == null)
				{
					if (!translationReference.Optional)
						contextForEmptyKeys.Add(context);
					return;
				}

				if (translationReference.RequiredVariableNames != null)
				{
					var rv = requiredVariablesByKey.GetOrAdd(key, _ => new HashSet<string>());
					rv.UnionWith(translationReference.RequiredVariableNames);
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
