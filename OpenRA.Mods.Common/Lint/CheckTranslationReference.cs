#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using Fluent.Net;
using Fluent.Net.RuntimeAst;

namespace OpenRA.Mods.Common.Lint
{
	class CheckTranslationReference : ILintPass
	{
		const BindingFlags Binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

		readonly List<string> referencedKeys = new List<string>();
		readonly Dictionary<string, string[]> referencedVariablesPerKey = new Dictionary<string, string[]>();
		readonly List<string> variableReferences = new List<string>();

		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			var language = "en";
			var translation = new Translation(language, modData.Manifest.Translations, modData.DefaultFileSystem);

			foreach (var modType in modData.ObjectCreator.GetTypes())
			{
				foreach (var fieldInfo in modType.GetFields(Binding).Where(m => m.HasAttribute<TranslationReferenceAttribute>()))
				{
					if (fieldInfo.FieldType != typeof(string))
						emitError($"Translation attribute on non string field {fieldInfo.Name}.");

					var key = (string)fieldInfo.GetValue(string.Empty);
					if (!translation.HasAttribute(key))
						emitError($"{key} not present in {language} translation.");

					var translationReference = fieldInfo.GetCustomAttributes<TranslationReferenceAttribute>(true)[0];
					if (translationReference.RequiredVariableNames != null && translationReference.RequiredVariableNames.Length > 0)
						referencedVariablesPerKey.GetOrAdd(key, translationReference.RequiredVariableNames);

					referencedKeys.Add(key);
				}
			}

			foreach (var file in modData.Manifest.Translations)
			{
				var stream = modData.DefaultFileSystem.Open(file);
				using (var reader = new StreamReader(stream))
				{
					var runtimeParser = new RuntimeParser();
					var result = runtimeParser.GetResource(reader);

					foreach (var entry in result.Entries)
					{
						if (!referencedKeys.Contains(entry.Key))
							emitWarning($"Unused key `{entry.Key}` in {file}.");

						var message = entry.Value;
						var node = message.Value;
						variableReferences.Clear();
						if (node is Pattern pattern)
						{
							foreach (var element in pattern.Elements)
							{
								if (element is SelectExpression selectExpression)
								{
									foreach (var variant in selectExpression.Variants)
									{
										if (variant.Value is Pattern variantPattern)
										{
											foreach (var variantElement in variantPattern.Elements)
												CheckVariableReference(variantElement, entry, emitWarning, file);
										}
									}
								}

								CheckVariableReference(element, entry, emitWarning, file);
							}

							if (referencedVariablesPerKey.ContainsKey(entry.Key))
							{
								var referencedVariables = referencedVariablesPerKey[entry.Key];
								foreach (var referencedVariable in referencedVariables)
								{
									if (!variableReferences.Contains(referencedVariable))
										emitError($"Missing variable `{referencedVariable}` for key `{entry.Key}` in {file}.");
								}
							}
						}
					}
				}
			}
		}

		void CheckVariableReference(Node element, KeyValuePair<string, Message> entry, Action<string> emitWarning, string file)
		{
			if (element is VariableReference variableReference)
			{
				variableReferences.Add(variableReference.Name);

				if (referencedVariablesPerKey.ContainsKey(entry.Key))
				{
					var referencedVariables = referencedVariablesPerKey[entry.Key];
					if (!referencedVariables.Contains(variableReference.Name))
						emitWarning($"Unused variable `{variableReference.Name}` for key `{entry.Key}` in {file}.");
				}
				else
					emitWarning($"Unused variable `{variableReference.Name}` for key `{entry.Key}` in {file}.");
			}
		}
	}
}
