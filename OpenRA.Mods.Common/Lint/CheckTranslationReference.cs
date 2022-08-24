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
using Linguini.Syntax.Ast;
using Linguini.Syntax.Parser;

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
					if (!translation.HasMessage(key))
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
					var parser = new LinguiniParser(reader);
					var result = parser.Parse();

					foreach (var entry in result.Entries)
					{
						// Don't flag definitions referenced (only) within the .ftl definitions as unused
						var isReusableTerm = entry.GetType() == typeof(AstTerm);
						var key = entry.GetId();
						if (!referencedKeys.Contains(key) && !isReusableTerm)
							emitWarning($"Unused key `{key}` in {file}.");

						variableReferences.Clear();
						if (entry is AstMessage message)
						{
							var node = message.Value;
							foreach (var element in node.Elements)
							{
								if (element is Placeable placeable)
								{
									var expression = placeable.Expression;
									if (expression is IInlineExpression inlineExpression)
									{
										if (inlineExpression is VariableReference variableReference)
											CheckVariableReference(variableReference.Id.Name.ToString(), entry, emitWarning, file);
									}

									if (expression is SelectExpression selectExpression)
									{
										foreach (var variant in selectExpression.Variants)
										{
											foreach (var variantElement in variant.Value.Elements)
											{
												if (variantElement is Placeable variantPlaceable)
												{
													var variantExpression = variantPlaceable.Expression;
													if (variantExpression is IInlineExpression variantInlineExpression)
													{
														if (variantInlineExpression is VariableReference variantVariableReference)
															CheckVariableReference(variantVariableReference.Id.Name.ToString(), entry, emitWarning, file);
													}
												}
											}
										}
									}
								}
							}

							if (referencedVariablesPerKey.ContainsKey(entry.GetId()))
							{
								var referencedVariables = referencedVariablesPerKey[entry.GetId()];
								foreach (var referencedVariable in referencedVariables)
								{
									if (!variableReferences.Contains(referencedVariable))
										emitError($"Missing variable `{referencedVariable}` for key `{entry.GetId()}` in {file}.");
								}
							}
						}
					}
				}
			}
		}

		void CheckVariableReference(string element, IEntry entry, Action<string> emitWarning, string file)
		{
			variableReferences.Add(element);

			if (referencedVariablesPerKey.ContainsKey(entry.GetId()))
			{
				var referencedVariables = referencedVariablesPerKey[entry.GetId()];
				if (!referencedVariables.Contains(element))
					emitWarning($"Unused variable `{element}` for key `{entry.GetId()}` in {file}.");
			}
			else
				emitWarning($"Unused variable `{element}` for key `{entry.GetId()}` in {file}.");
		}
	}
}
