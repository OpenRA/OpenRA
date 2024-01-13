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
using Linguini.Syntax.Ast;
using Linguini.Syntax.Parser;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Lint
{
	sealed class CheckTranslationReference : ILintPass, ILintMapPass
	{
		const BindingFlags Binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

		readonly List<string> referencedKeys = new();
		readonly Dictionary<string, string[]> referencedVariablesPerKey = new();
		readonly List<string> variableReferences = new();

		void TestTraits(Ruleset rules, Action<string> emitError, Action<string> testKey)
		{
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var traitType = traitInfo.GetType();
					foreach (var field in Utility.GetFields(traitType))
					{
						var translationReference = Utility.GetCustomAttributes<TranslationReferenceAttribute>(field, true).FirstOrDefault();
						if (translationReference == null)
							continue;

						var keys = LintExts.GetFieldValues(traitInfo, field, translationReference.DictionaryReference);
						foreach (var key in keys)
						{
							if (key == null)
							{
								if (!translationReference.Optional)
									emitError($"Actor type `{actorInfo.Key}` has an empty translation reference in trait `{traitType.Name[..^4]}.{field.Name}`.");

								continue;
							}

							if (referencedKeys.Contains(key))
								continue;

							testKey(key);
							referencedKeys.Add(key);
						}
					}
				}
			}
		}

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.TranslationDefinitions == null)
				return;

			// TODO: Check all available languages.
			const string Language = "en";
			var modTranslation = new Translation(Language, modData.Manifest.Translations, modData.DefaultFileSystem, _ => { });
			var mapTranslation = new Translation(Language, FieldLoader.GetValue<string[]>("value", map.TranslationDefinitions.Value), map, error => emitError(error.ToString()));

			TestTraits(map.Rules, emitError, key =>
			{
				if (modTranslation.HasMessage(key))
				{
					if (mapTranslation.HasMessage(key))
						emitWarning($"Map translation key `{key}` already exists in `{Language}` mod translations and will not be used.");
				}
				else if (!mapTranslation.HasMessage(key))
					emitWarning($"`{key}` is not present in `{Language}` translation.");
			});
		}

		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			// TODO: Check all available languages.
			const string Language = "en";
			Console.WriteLine($"Testing translation: {Language}");
			var translation = new Translation(Language, modData.Manifest.Translations, modData.DefaultFileSystem, error => emitError(error.ToString()));

			TestTraits(modData.DefaultRules, emitError, key =>
			{
				if (!translation.HasMessage(key))
					emitWarning($"`{key}` is not present in `{Language}` translation.");
			});

			var gameSpeeds = modData.Manifest.Get<GameSpeeds>();
			foreach (var speed in gameSpeeds.Speeds.Values)
			{
				if (!translation.HasMessage(speed.Name))
					emitWarning($"`{speed.Name}` is not present in `{Language}` translation.");

				referencedKeys.Add(speed.Name);
			}

			foreach (var modType in modData.ObjectCreator.GetTypes())
			{
				foreach (var fieldInfo in modType.GetFields(Binding).Where(m => Utility.HasAttribute<TranslationReferenceAttribute>(m)))
				{
					if (fieldInfo.IsInitOnly || !fieldInfo.IsStatic)
						continue;

					if (fieldInfo.FieldType != typeof(string))
					{
						emitError($"Translation attribute on non string field `{fieldInfo.Name}`.");
						continue;
					}

					var key = (string)fieldInfo.GetValue(null);
					if (referencedKeys.Contains(key))
						continue;

					if (!translation.HasMessage(key))
						emitWarning($"`{key}` is not present in `{Language}` translation.");

					var translationReference = Utility.GetCustomAttributes<TranslationReferenceAttribute>(fieldInfo, true)[0];
					if (translationReference.RequiredVariableNames != null && translationReference.RequiredVariableNames.Length > 0)
						referencedVariablesPerKey.GetOrAdd(key, translationReference.RequiredVariableNames);

					referencedKeys.Add(key);
				}
			}

			var translatableFields = modData.ObjectCreator.GetTypes()
				.Where(t => t.Name.EndsWith("Widget", StringComparison.InvariantCulture) && t.IsSubclassOf(typeof(Widget)))
				.ToDictionary(
					t => t.Name[..^6],
					t => t.GetFields().Where(f => f.HasAttribute<TranslationReferenceAttribute>()).ToArray())
				.Where(t => t.Value.Length > 0)
				.ToDictionary(
					t => t.Key,
					t => t.Value.Select(f => (f.Name, f, Utility.GetCustomAttributes<TranslationReferenceAttribute>(f, true)[0])).ToArray());

			foreach (var filename in modData.Manifest.ChromeLayout)
			{
				var nodes = MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename);
				foreach (var node in nodes)
					CheckChrome(node, translation, Language, emitError, emitWarning, translatableFields);
			}

			foreach (var file in modData.Manifest.Translations)
			{
				var stream = modData.DefaultFileSystem.Open(file);
				using (var reader = new StreamReader(stream))
				{
					var parser = new LinguiniParser(reader);
					var result = parser.Parse();

					foreach (var entry in result.Entries)
					{
						// Don't flag definitions referenced (only) within the .ftl definitions as unused.
						if (entry.GetType() == typeof(AstTerm))
							continue;

						variableReferences.Clear();
						var key = entry.GetId();

						if (entry is AstMessage message)
						{
							var hasAttributes = message.Attributes.Count > 0;

							if (!hasAttributes)
							{
								CheckUnusedKey(key, null, emitWarning, file);
								CheckMessageValue(message.Value, key, null, emitWarning, file);
								CheckMissingVariable(key, null, emitError, file);
							}
							else
							{
								foreach (var attribute in message.Attributes)
								{
									var attrName = attribute.Id.Name.ToString();

									CheckUnusedKey(key, attrName, emitWarning, file);
									CheckMessageValue(attribute.Value, key, attrName, emitWarning, file);
									CheckMissingVariable(key, attrName, emitError, file);
								}
							}
						}
					}
				}
			}
		}

		void CheckChrome(MiniYamlNode node, Translation translation, string language, Action<string> emitError, Action<string> emitWarning,
			Dictionary<string, (string Name, FieldInfo Field, TranslationReferenceAttribute Attribute)[]> translatables)
		{
			var nodeType = node.Key.Split('@')[0];
			foreach (var childNode in node.Value.Nodes)
			{
				if (!translatables.TryGetValue(nodeType, out var translationNodes))
					continue;

				var childType = childNode.Key.Split('@')[0];
				var field = translationNodes.FirstOrDefault(t => t.Name == childType);
				if (field.Name == null)
					continue;

				var key = childNode.Value.Value;
				if (key == null)
				{
					if (!field.Attribute.Optional)
						emitError($"Widget `{node.Key}` in field `{childType}` has an empty translation reference.");

					continue;
				}

				if (referencedKeys.Contains(key))
					continue;

				if (!key.Any(char.IsLetter))
					continue;

				if (!translation.HasMessage(key))
					emitWarning($"`{key}` defined by `{node.Key}` is not present in `{language}` translation.");

				referencedKeys.Add(key);
			}

			foreach (var childNode in node.Value.Nodes)
				if (childNode.Key == "Children")
					foreach (var n in childNode.Value.Nodes)
						CheckChrome(n, translation, language, emitError, emitWarning, translatables);
		}

		void CheckUnusedKey(string key, string attribute, Action<string> emitWarning, string file)
		{
			var isAttribute = !string.IsNullOrEmpty(attribute);
			var keyWithAtrr = isAttribute ? $"{key}.{attribute}" : key;

			if (!referencedKeys.Contains(keyWithAtrr))
				emitWarning(isAttribute ?
					$"Unused attribute `{attribute}` of key `{key}` in {file}" :
					$"Unused key `{key}` in {file}");
		}

		void CheckMessageValue(Pattern node, string key, string attribute, Action<string> emitWarning, string file)
		{
			foreach (var element in node.Elements)
			{
				if (element is Placeable placeable)
				{
					var expression = placeable.Expression;
					if (expression is IInlineExpression inlineExpression &&
						inlineExpression is VariableReference variableReference)
						CheckVariableReference(variableReference.Id.Name.ToString(), key, attribute, emitWarning, file);

					if (expression is SelectExpression selectExpression)
					{
						foreach (var variant in selectExpression.Variants)
						{
							foreach (var variantElement in variant.Value.Elements)
							{
								if (variantElement is Placeable variantPlaceable)
								{
									var variantExpression = variantPlaceable.Expression;
									if (variantExpression is IInlineExpression variantInlineExpression &&
										variantInlineExpression is VariableReference variantVariableReference)
										CheckVariableReference(variantVariableReference.Id.Name.ToString(), key, attribute, emitWarning, file);
								}
							}
						}
					}
				}
			}
		}

		void CheckMissingVariable(string key, string attribute, Action<string> emitError, string file)
		{
			var isAttribute = !string.IsNullOrEmpty(attribute);
			var keyWithAtrr = isAttribute ? $"{key}.{attribute}" : key;

			if (!referencedVariablesPerKey.TryGetValue(keyWithAtrr, out var referencedVariables))
				return;

			foreach (var referencedVariable in referencedVariables)
				if (!variableReferences.Contains(referencedVariable))
					emitError(isAttribute ?
						$"Missing variable `{referencedVariable}` for attribute `{attribute}` of key `{key}` in {file}" :
						$"Missing variable `{referencedVariable}` for key `{key}` in {file}");
		}

		void CheckVariableReference(string varName, string key, string attribute, Action<string> emitWarning, string file)
		{
			var isAttribute = !string.IsNullOrEmpty(attribute);
			var keyWithAtrr = isAttribute ? $"{key}.{attribute}" : key;

			variableReferences.Add(varName);

			if (!referencedVariablesPerKey.TryGetValue(keyWithAtrr, out var referencedVariables) || !referencedVariables.Contains(varName))
				emitWarning(isAttribute ?
					$"Unused variable `{varName}` for attribute `{attribute}` of key `{key}` in {file}" :
					$"Unused variable `{varName}` for key `{key}` in {file}");
		}
	}
}
