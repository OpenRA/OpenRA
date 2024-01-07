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
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Scripting.Global;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Scripting;
using OpenRA.Support;
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

			var usedKeys = GetUsedTranslationKeysInMap(map, emitWarning);

			foreach (var context in usedKeys.EmptyKeyContexts)
				emitWarning($"Empty key in map translation files required by {context}");

			var mapTranslations = FieldLoader.GetValue<string[]>("value", map.TranslationDefinitions.Value);

			foreach (var language in GetTranslationLanguages(modData))
			{
				// Check keys and variables are not missing across all language files.
				// But for maps we don't warn on unused keys. They might be unused on *this* map,
				// but the mod or another map may use them and we don't have sight of that.
				CheckKeys(
					modData.Manifest.Translations.Concat(mapTranslations), map.Open, usedKeys,
					language, _ => false, emitError, emitWarning);

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
				CheckModWidgets(modData, usedKeys, testedFields, translation, language, emitError, emitWarning);

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
						emitWarning($"Missing key `{group.Key}` in `{language}` language in mod translation files required by {context}");
				}
			}

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

						foreach (var key in LintExts.GetFieldValues(traitInfo, field, translationReference.DictionaryReference))
							usedKeys.Add(key, translationReference, $"Actor `{actorInfo.Key}` trait `{traitType.Name[..^4]}.{field.Name}`");
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
						var translationReference = Utility.GetCustomAttributes<TranslationReferenceAttribute>(field, true).SingleOrDefault();
						if (translationReference == null)
							continue;

						foreach (var key in LintExts.GetFieldValues(warhead, field, translationReference.DictionaryReference))
							usedKeys.Add(key, translationReference, $"Weapon `{weapon.Key}` warhead `{warheadType.Name[..^7]}.{field.Name}`");
					}
				}
			}

			return usedKeys;
		}

		static TranslationKeys GetUsedTranslationKeysInMap(Map map, Action<string> emitWarning)
		{
			var usedKeys = GetUsedTranslationKeysInRuleset(map.Rules);

			var luaScriptInfo = map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<LuaScriptInfo>();
			if (luaScriptInfo != null)
			{
				// Matches expressions such as:
				// UserInterface.Translate("translation-key")
				// UserInterface.Translate("translation-key\"with-escape")
				// UserInterface.Translate("translation-key", { ["attribute"] = foo })
				// UserInterface.Translate("translation-key", { ["attribute\"-with-escape"] = foo })
				// UserInterface.Translate("translation-key", { ["attribute1"] = foo, ["attribute2"] = bar })
				// UserInterface.Translate("translation-key", tableVariable)
				// Extracts groups for the 'key' and each 'attr'.
				// If the table isn't inline like in the last example, extracts it as 'variable'.
				const string UserInterfaceTranslatePattern =
					@"UserInterface\s*\.\s*Translate\s*\(" + // UserInterface.Translate(
					@"\s*""(?<key>(?:[^""\\]|\\.)+?)""\s*" + // "translation-key"
					@"(,\s*({\s*\[\s*""(?<attr>(?:[^""\\]|\\.)*?)""\s*\]\s*=\s*.*?" + // { ["attribute1"] = foo
					@"(\s*,\s*\[\s*""(?<attr>(?:[^""\\]|\\.)*?)""\s*\]\s*=\s*.*?)*\s*}\s*)" + // , ["attribute2"] = bar }
					"|\\s*,\\s*(?<variable>.*?))?" + // tableVariable
					@"\)"; // )
				var translateRegex = new Regex(UserInterfaceTranslatePattern);

				// The script in mods/common/scripts/utils.lua defines some helpers which accept a translation key
				// Matches expressions such as:
				// AddPrimaryObjective(Player, "translation-key")
				// AddSecondaryObjective(Player, "translation-key")
				// AddPrimaryObjective(Player, "translation-key\"with-escape")
				// Extracts groups for the 'key'.
				const string AddObjectivePattern =
					@"(AddPrimaryObjective|AddSecondaryObjective)\s*\(" + // AddPrimaryObjective(
					@".*?\s*,\s*""(?<key>(?:[^""\\]|\\.)+?)""\s*" + // Player, "translation-key"
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
						var scriptTranslations = matches.Select(m =>
						{
							var key = m.Groups["key"].Value.Replace(@"\""", @"""");
							var attrs = m.Groups["attr"].Captures.Select(c => c.Value.Replace(@"\""", @"""")).ToArray();
							var variable = m.Groups["variable"].Value;
							var line = scriptText.Take(m.Index).Count(x => x == '\n') + 1;
							return (Key: key, Attrs: attrs, Variable: variable, Line: line);
						}).ToArray();
						foreach (var (key, attrs, variable, line) in scriptTranslations)
						{
							var context = $"Script {script}:{line}";
							usedKeys.Add(key, new TranslationReferenceAttribute(attrs), context);

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

		static (TranslationKeys UsedKeys, List<FieldInfo> TestedFields) GetUsedTranslationKeysInMod(ModData modData)
		{
			var usedKeys = GetUsedTranslationKeysInRuleset(modData.DefaultRules);
			var testedFields = new List<FieldInfo>();
			testedFields.AddRange(
				modData.ObjectCreator.GetTypes()
				.Where(t => t.IsSubclassOf(typeof(TraitInfo)) || t.IsSubclassOf(typeof(Warhead)))
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
						usedKeys.Add(
							resourceTypes.Value.Name,
							resourceTypeTranslationReference,
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

		static void CheckModWidgets(
			ModData modData, TranslationKeys usedKeys, List<FieldInfo> testedFields,
			Translation translation, string language, Action<string> emitError, Action<string> emitWarning)
		{
			var (minEffectiveResolution, chromeLayoutNodes, rootsByNodeId) = BuildChromeTree(modData);

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

			// Set up data we need to check the translation text fits on the widgets.
			var platform = Game.CreatePlatform("Default");
			var fontSheetBuilder = new SheetBuilder(SheetType.BGRA, 512);
			var fonts = modData.Manifest.Get<Fonts>().FontList.ToDictionary(x => x.Key,
				x => new SpriteFont(
					platform, x.Value.Font, modData.DefaultFileSystem.Open(x.Value.Font).ReadAllBytes(),
					x.Value.Size, x.Value.Ascender, 1f, fontSheetBuilder));
			ChromeMetrics.Initialize(modData);

			// Check that translations fit onto the widget.
			var uncheckedNodes = new List<MiniYamlNode>();
			foreach (var node in chromeLayoutNodes)
			{
				var nodeId = node.Key.Split('@')[1];
				if (rootsByNodeId.TryGetValue(nodeId, out var rootContext))
				{
					var allBounds = rootContext.Entries.Select(e => e.Bounds).ToArray();
					CheckChrome(
						node, translation, language, emitError, emitWarning, translationReferencesByWidgetField,
						allBounds, usedKeys, minEffectiveResolution, fonts);
				}
				else
					uncheckedNodes.Add(node);
			}

			// For any nodes where we couldn't work out what their parent should be, we don't know the available size of the parent widget.
			// Instead, check them assuming they have the full window size available.
			foreach (var node in uncheckedNodes)
			{
				emitWarning($"Widget `{node.Key}` in {node.Location} does not have a known parent in the widget hierarchy, validation performed assuming window bounds.");
				var windowBounds = new WidgetBounds(0, 0, minEffectiveResolution.X, minEffectiveResolution.Y);
				CheckChrome(
					node, translation, language, emitError, emitWarning, translationReferencesByWidgetField,
					new[] { windowBounds }, usedKeys, minEffectiveResolution, fonts);
			}
		}

		static WidgetBounds GetWidgetBounds(MiniYamlNode node, WidgetBounds parentBounds, int2 minEffectiveResolution)
		{
			// See Widget.Initialize & DropDownButtonWidget.ShowDropDown for reference.
			var substitutions = new Dictionary<string, int>
			{
				{ "WINDOW_RIGHT", minEffectiveResolution.X },
				{ "WINDOW_BOTTOM", minEffectiveResolution.Y },
				{ "PARENT_RIGHT", parentBounds.Right },
				{ "PARENT_BOTTOM", parentBounds.Bottom },
				{ "DROPDOWN_WIDTH", parentBounds.Width },
			};
			var xExpr = new IntegerExpression(node.Value.NodeWithKeyOrDefault("X")?.Value.Value ?? "0");
			var yExpr = new IntegerExpression(node.Value.NodeWithKeyOrDefault("Y")?.Value.Value ?? "0");
			var widthExpr = new IntegerExpression(node.Value.NodeWithKeyOrDefault("Width")?.Value.Value ?? "0");
			var heightExpr = new IntegerExpression(node.Value.NodeWithKeyOrDefault("Height")?.Value.Value ?? "0");
			var x = xExpr.Evaluate(substitutions);
			var y = yExpr.Evaluate(substitutions);
			var width = widthExpr.Evaluate(substitutions);
			var height = heightExpr.Evaluate(substitutions);
			return new WidgetBounds(x, y, width, height);
		}

		static (
			int2 MinEffectiveResolution,
			MiniYamlNode[] ChromeLayoutNodes,
			Dictionary<string, RootContext> RootsByNodeId) BuildChromeTree(ModData modData)
		{
			// MinEffectiveResolution is the minimum resolution we design the UI around.
			// This means we can check the translations fit for our minimum supported size.
			var minEffectiveResolution = new int2(modData.Manifest.Get<WorldViewportSizes>().MinEffectiveResolution);
			var windowBounds = new WidgetBounds(0, 0, minEffectiveResolution.X, minEffectiveResolution.Y);

			// Initial roots for possible widgets trees are given by LoadWidgetAtGameStartInfo.
			// Also handle windows created by ModContentLoadScreen.
			var rootsByNodeId = new Dictionary<string, RootContext>();
			var loadWidgetAtGameStartInfo = modData.DefaultRules.Actors[SystemActors.World].TraitInfo<LoadWidgetAtGameStartInfo>();
			rootsByNodeId[loadWidgetAtGameStartInfo.ShellmapRoot] = RootContext.CreateInitial(windowBounds);
			rootsByNodeId[loadWidgetAtGameStartInfo.IngameRoot] = RootContext.CreateInitial(windowBounds);
			rootsByNodeId[loadWidgetAtGameStartInfo.EditorRoot] = RootContext.CreateInitial(windowBounds);
			rootsByNodeId[loadWidgetAtGameStartInfo.GameSaveLoadingRoot] = RootContext.CreateInitial(windowBounds);
			rootsByNodeId[ModContentLoadScreen.ContentPromptPanelWidgetId] = RootContext.CreateInitial(windowBounds);
			rootsByNodeId[ModContentLoadScreen.ContentPanelWidgetId] = RootContext.CreateInitial(windowBounds);
			rootsByNodeId[ModContentLoadScreen.ModContentBackgroundWidgetId] = RootContext.CreateInitial(windowBounds);

			// Gather all the nodes together for evaluation.
			var chromeLayoutNodes = modData.Manifest.ChromeLayout
				.SelectMany(filename => MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename))
				.ToArray();

			// Stitch parent-> child widget relations together, until we have built the whole widget tree.
			// We loop multiple times, as each time we resolve a parent->child that allows
			// on the next pass for the children of those children to be resolved.
			// rootsByNodeId stores the state at the time the widget tree reached that location.
			// As child widgets might be parented to multiple places in the tree, multiple entrypoints are possible.
			// e.g. the same widget is used on two different screens. We track the bounds across all branches.
			var nodesLeftToBuild = chromeLayoutNodes.ToList();
			while (nodesLeftToBuild.Count > 0)
			{
				var builtNodes = new HashSet<MiniYamlNode>();
				foreach (var node in nodesLeftToBuild)
				{
					var nodeId = node.Key.Split('@')[1];
					if (rootsByNodeId.TryGetValue(nodeId, out var rootContext))
					{
						builtNodes.Add(node);

						// Snapshot Entries as it can be mutated.
						foreach (var entrypoint in rootContext.Entries.ToArray())
						{
							var outOfTreeParentChildWidgetIds = new Dictionary<string, HashSet<string>>();
							BuildChromeTreeBranch(
								modData, minEffectiveResolution, rootsByNodeId, outOfTreeParentChildWidgetIds,
								node, entrypoint.Bounds, new Stack<LogicCall>(entrypoint.Calls));
							BuildChromeTreeBranchForOutOfTree(
								minEffectiveResolution, rootsByNodeId, outOfTreeParentChildWidgetIds,
								node, entrypoint.Bounds, new Stack<LogicCall>(entrypoint.Calls));
						}
					}
				}

				if (builtNodes.Count == 0)
					break;

				nodesLeftToBuild.RemoveAll(builtNodes.Contains);
			}

			return (minEffectiveResolution, chromeLayoutNodes, rootsByNodeId);
		}

		static void WalkChromeTree(
			int2 minEffectiveResolution, MiniYamlNode node, WidgetBounds parentBounds, Stack<LogicCall> logicCallStack,
			Action<string, string, MiniYamlNode, WidgetBounds> nodeAction)
		{
			LogicCall logicCall = null;
			var logicNode = node.Value.NodeWithKeyOrDefault("Logic");
			if (logicNode != null)
			{
				var logics = logicNode.Value.Value.Split(",").Select(x => x.Trim()).ToArray();
				var logicArgs = logicNode.Value.ToDictionary();
				logicCallStack.Push(logicCall = new LogicCall(logics, logicArgs));
			}

			var bounds = GetWidgetBounds(node, parentBounds, minEffectiveResolution);

			var split = node.Key.Split('@');
			var nodeType = split[0];
			var nodeId = split.ElementAtOrDefault(1);
			nodeAction(nodeType, nodeId, node, bounds);

			foreach (var childNode in node.Value.Nodes)
				if (childNode.Key == "Children")
					foreach (var n in childNode.Value.Nodes)
						WalkChromeTree(minEffectiveResolution, n, bounds, logicCallStack, nodeAction);

			if (logicCall != null)
				logicCallStack.Pop();
		}

		static void BuildChromeTreeBranch(
			ModData modData, int2 minEffectiveResolution,
			Dictionary<string, RootContext> rootsByNodeId, Dictionary<string, HashSet<string>> outOfTreeParentChildWidgetIds,
			MiniYamlNode rootNode, WidgetBounds parentBounds, Stack<LogicCall> logicCallStack)
		{
			WalkChromeTree(minEffectiveResolution, rootNode, parentBounds, logicCallStack, (nodeType, nodeId, node, bounds) =>
			{
				if (nodeId == null)
					return;

				var windowBounds = new WidgetBounds(0, 0, minEffectiveResolution.X, minEffectiveResolution.Y);

				// Determine parent->child widget links that are created dynamically at runtime.
				// We can get a static reference of such relationships via derived classes of DynamicWidgets.
				var parentChildWidgetIds = GetParentChildWidgetIds(
					modData, logicCallStack, dw => dw.ParentWidgetIdForChildWidgetId, true);
				var dropdownParentChildWidgetIds = GetMultiParentChildWidgetIds(
					modData, logicCallStack, dw => dw.ParentDropdownWidgetIdsFromPanelWidgetId, true);
				var allParentChildWidgetIds = parentChildWidgetIds.Concat(dropdownParentChildWidgetIds)
					.GroupBy(x => x.Key)
					.ToDictionary(g => g.Key, g => g.SelectMany(kvp => kvp.Value).ToArray());

				// Determine out-of-tree links. This is where the logic grabs a widget outside the widget it has been given to manage.
				// e.g. it goes to Ui.Root and finds a widget from there.
				// This means the logic might be manging something outside its call stack.
				var localOutOfTreeParentChildWidgetIds = GetParentChildWidgetIds(
					modData, logicCallStack, dw => dw.OutOfTreeParentWidgetIdForChildWidgetId, false);
				foreach (var kvp in localOutOfTreeParentChildWidgetIds)
				{
					var parentWidgetId = kvp.Key.ParentWidgetId;
					if (parentWidgetId == "")
					{
						// A blank parent indicates the parent is Ui.Root. Add it with the window area.
						foreach (var childWidgetId in kvp.Value)
							rootsByNodeId.TryAdd(childWidgetId, RootContext.CreateInitial(windowBounds));
					}
					else
					{
						// Save this link for later, we'll walk the tree again and link up out-of-tree elements.
						var entries = outOfTreeParentChildWidgetIds.GetOrAdd(parentWidgetId, _ => new HashSet<string>());
						entries.UnionWith(kvp.Value);
					}
				}

				// Add any windows the logic can open.
				var windowWidgetIds = GetLogicWidgets(modData, logicCallStack, true)
					.SelectMany(x => x.DynamicWidgets.WindowWidgetIds);
				foreach (var windowWidgetId in windowWidgetIds)
					rootsByNodeId.TryAdd(windowWidgetId, RootContext.CreateInitial(windowBounds));

				// If we've resolved the parent, set up the child bounds for the next pass.
				// For every logic that is if effect in this call stack we'll
				// add bounds for every child widget it links up dynamically.
				foreach (var logic in logicCallStack.SelectMany(c => c.Logics).Distinct())
					if (allParentChildWidgetIds.TryGetValue((logic, nodeId), out var childOfParentNodeIds))
						foreach (var childOfParentNodeId in childOfParentNodeIds)
							rootsByNodeId.GetOrAdd(childOfParentNodeId, _ => RootContext.CreateEmpty()).Add(bounds, logicCallStack);
			});

			static Dictionary<(string Logic, string ParentWidgetId), string[]> GetParentChildWidgetIds(
				ModData modData, Stack<LogicCall> logicCallStack,
				Func<ChromeLogic.DynamicWidgets, IReadOnlyDictionary<string, string>> parentWidgetIdForChildWidgetId,
				bool logicMustBeOnCallStack)
			{
				return GetLogicWidgets(modData, logicCallStack, logicMustBeOnCallStack)
					.SelectMany(x =>
						parentWidgetIdForChildWidgetId(x.DynamicWidgets)
							.GroupBy(kvp => kvp.Value)
							.Select(g => (x.Logic, ParentWidgetId: g.Key, ChildWidgetIds: g.Select(kvp => kvp.Key).ToArray())))
					.GroupBy(x => (x.Logic, x.ParentWidgetId))
					.ToDictionary(g => g.Key, g => g.SelectMany(x => x.ChildWidgetIds).ToArray());
			}

			static Dictionary<(string Logic, string ParentWidgetId), string[]> GetMultiParentChildWidgetIds(
				ModData modData, Stack<LogicCall> logicCallStack,
				Func<ChromeLogic.DynamicWidgets, IReadOnlyDictionary<string, IReadOnlyCollection<string>>> parentWidgetIdsForChildWidgetId,
				bool logicMustBeOnCallStack)
			{
				return GetLogicWidgets(modData, logicCallStack, logicMustBeOnCallStack)
					.SelectMany(x =>
						parentWidgetIdsForChildWidgetId(x.DynamicWidgets)
							.SelectMany(kvp => kvp.Value.Select(v => (ChildWidgetId: kvp.Key, ParentWidgetId: v)))
							.GroupBy(x => x.ParentWidgetId)
							.Select(g => (x.Logic, ParentWidgetId: g.Key, ChildWidgetIds: g.Select(x => x.ChildWidgetId).ToArray())))
					.GroupBy(x => (x.Logic, x.ParentWidgetId))
					.ToDictionary(g => g.Key, g => g.SelectMany(x => x.ChildWidgetIds).ToArray());
			}

			static IEnumerable<(string Logic, ChromeLogic.DynamicWidgets DynamicWidgets)> GetLogicWidgets(
				ModData modData, Stack<LogicCall> logicCallStack, bool logicMustBeOnCallStack)
			{
				return modData.ObjectCreator.GetTypes()
					.Where(t =>
						t.IsSubclassOf(typeof(ChromeLogic.DynamicWidgets)) &&
						typeof(ChromeLogic).IsAssignableFrom(t.ReflectedType))
					.SelectMany(t =>
					{
						var reflectedTypeName = t.ReflectedType.Name;
						return logicCallStack
							.Where(c => !logicMustBeOnCallStack || c.Logics.Contains(reflectedTypeName))
							.Select(c =>
								modData.ObjectCreator.CreateObject<ChromeLogic.DynamicWidgets>(
									$"{reflectedTypeName}+{t.Name}",
									new Dictionary<string, object> { { "logicArgs", c.LogicArgs } }))
							.Select(dw => (Logic: reflectedTypeName, DynamicWidgets: dw));
					});
			}
		}

		static void BuildChromeTreeBranchForOutOfTree(
			int2 minEffectiveResolution,
			Dictionary<string, RootContext> rootsByNodeId, Dictionary<string, HashSet<string>> outOfTreeParentChildWidgetIds,
			MiniYamlNode rootNode, WidgetBounds parentBounds, Stack<LogicCall> logicCallStack)
		{
			WalkChromeTree(minEffectiveResolution, rootNode, parentBounds, logicCallStack, (nodeType, nodeId, node, bounds) =>
			{
				// Tooltips operate out-of-tree, as the widget tree has a single container widget for all tooltips.
				var tooltipContainer = node.Value.NodeWithKeyOrDefault("TooltipContainer");
				var tooltipTemplate = node.Value.NodeWithKeyOrDefault("TooltipTemplate");
				if (tooltipContainer != null || tooltipTemplate != null)
				{
					var container = tooltipContainer?.Value.Value;
					var template = tooltipTemplate?.Value.Value;

					// HACK: Hardcode the default values for nodes that have a default in code and don't force a value in YAML.
					container ??= "TOOLTIP_CONTAINER"; // Fallback, if a new type ever gets added that doesn't require this to be set in YAML.
					template ??= nodeType switch
					{
						"ClientTooltipRegion" =>
							node.Value.NodeWithKey("Template").Value.Value, // Breaks the usual convention of 'TooltipTemplate'.
						"Button" or "DropDownButton" or "Checkbox" or "MenuButton" or "WorldButton" or "ProductionTypeButton" or "ScrollItem" =>
							"BUTTON_TOOLTIP",
						"ObserverProductionIcons" or "ProductionPalette" =>
							"PRODUCTION_TOOLTIP",
						"ObserverSupportPowerIcons" or "SupportPowers" =>
							"SUPPORT_POWER_TOOLTIP",
						"ObserverArmyIcons" =>
							"ARMY_TOOLTIP",
						"MapPreview" =>
							"SPAWN_TOOLTIP",
						"ViewportController" =>
							"WORLD_TOOLTIP",
						_ => "SIMPLE_TOOLTIP", // Fallback, for any type we haven't got the correct hardcoded value for.
					};

					// Add discovered tooltips. Tooltips determine their own size so the bounds are irrelevant.
					// However adding them to the roots list allows us to mark them as widgets with known parents.
					foreach (var logic in logicCallStack.SelectMany(c => c.Logics).Distinct())
						rootsByNodeId.GetOrAdd(template, _ => RootContext.CreateEmpty()).Add(new WidgetBounds(0, 0, 0, 0), logicCallStack);
				}

				if (nodeId == null)
					return;

				// For out-of-tree widgets, assume the full window bounds is available to them.
				// As out-of-tree widgets might be managed by a logic outside their call stack,
				// we ignore the callstack when making checks here.
				var windowBounds = new WidgetBounds(0, 0, minEffectiveResolution.X, minEffectiveResolution.Y);
				if (outOfTreeParentChildWidgetIds.TryGetValue(nodeId, out var childOfParentNodeIds))
					foreach (var childOfParentNodeId in childOfParentNodeIds)
						rootsByNodeId.GetOrAdd(childOfParentNodeId, _ => RootContext.CreateEmpty()).Add(windowBounds, logicCallStack);
			});
		}

		static void CheckChrome(
			MiniYamlNode rootNode, Translation translation, string language,
			Action<string> emitError, Action<string> emitWarning,
			Dictionary<(string WidgetName, string FieldName), TranslationReferenceAttribute> translationReferencesByWidgetField,
			IReadOnlyCollection<WidgetBounds> allParentBounds,
			TranslationKeys usedKeys,
			int2 minEffectiveResolution,
			Dictionary<string, SpriteFont> fonts)
		{
			var allWidgetBounds = allParentBounds.Select(parentBounds => GetWidgetBounds(rootNode, parentBounds, minEffectiveResolution));

			// HACK: Some widgets that display icons don't bother with bounds, but instead use a icon size.
			// So we need to check if text fits on the icon, rather than within the bounds.
			var iconSize = rootNode.Value.NodeWithKeyOrDefault("IconSize")?.Value.Value;
			if (iconSize != null)
			{
				var iconSizeValues = iconSize.Split(",").Select(int.Parse).ToArray();
				allWidgetBounds = allWidgetBounds.Select(wb => new WidgetBounds(wb.X, wb.Y, iconSizeValues[0], iconSizeValues[1]));
			}

			var allWidgetBoundsArray = allWidgetBounds.ToArray();

			var nodeType = rootNode.Key.Split('@')[0];
			foreach (var childNode in rootNode.Value.Nodes)
			{
				var childType = childNode.Key.Split('@')[0];
				if (!translationReferencesByWidgetField.TryGetValue((nodeType, childType), out var translationReference))
					continue;

				var key = childNode.Value.Value;
				usedKeys.Add(key, translationReference, $"Widget `{rootNode.Key}` field `{childType}` in {rootNode.Location}");

				if (key == null)
					continue;

				// HACK: Tooltips don't display on the widget directly, don't validate their sizes.
				if (childType == "TooltipText" || childType == "TooltipDesc")
					continue;

				// HACK: Hardcode how each widget determines available fonts.
				var fontName = nodeType switch
				{
					"Button" or "DropDownButton" or "Checkbox" or "MenuButton" or "WorldButton" =>
						rootNode.Value.NodeWithKeyOrDefault("Font")?.Value.Value ?? ChromeMetrics.Get<string>("ButtonFont"),
					"Label" or "LabelWithHighlight" or "LabelForInput" =>
						rootNode.Value.NodeWithKeyOrDefault("Font")?.Value.Value ?? ChromeMetrics.Get<string>("TextFont"),
					"SupportPowers" =>
						rootNode.Value.NodeWithKeyOrDefault("OverlayFont")?.Value.Value ?? "TinyBold",
					"ProductionPalette" =>
						rootNode.Value.NodeWithKeyOrDefault("OverlayFont")?.Value.Value ?? "TinyBold",
					_ => null,
				};
				if (fontName == null)
				{
					emitWarning(
						$"`{key}` defined by `{rootNode.Key}` in field `{childType}` in {rootNode.Location} " +
						"is not a widget type whose font is recognised, validation performed using TextFont from ChromeMetrics.");
					fontName = ChromeMetrics.Get<string>("TextFont");
				}

				var font = fonts[fontName];
				var text = translation.GetString(key);
				foreach (var widgetBounds in allWidgetBoundsArray)
				{
					var widgetSize = new int2(widgetBounds.Width, widgetBounds.Height);

					// HACK: Apply the WordWrap that labels can apply.
					if ((nodeType == "Label" || nodeType == "LabelWithHighlight") &&
						bool.Parse(rootNode.Value.NodeWithKeyOrDefault("WordWrap")?.Value.Value ?? bool.FalseString))
						text = WidgetUtils.WrapText(text, widgetSize.X, font);

					var textSize = font.Measure(text);
					if (textSize.X > widgetSize.X || textSize.Y > widgetSize.Y)
						emitWarning(
							$"`{key}` defined by `{rootNode.Key}` in field `{childType}` in {rootNode.Location} " +
							$"has value `{text}` in `{language}` translation. Text is too large for widget. " +
							$"Text is {textSize}. Widget is {widgetSize}.");
				}
			}

			foreach (var childNode in rootNode.Value.Nodes)
				if (childNode.Key == "Children")
					foreach (var n in childNode.Value.Nodes)
						CheckChrome(
							n, translation, language, emitError, emitWarning, translationReferencesByWidgetField,
							allWidgetBoundsArray, usedKeys, minEffectiveResolution, fonts);
		}

		static HashSet<string> CheckKeys(
			IEnumerable<string> translationFiles, Func<string, Stream> openFile, TranslationKeys usedKeys,
			string language, Func<string, bool> checkUnusedKeysForFile,
			Action<string> emitError, Action<string> emitWarning)
		{
			var keyWithAttrs = new HashSet<string>();
			foreach (var file in translationFiles)
			{
				if (!file.EndsWith($"{language}.ftl", StringComparison.Ordinal))
					continue;

				var stream = openFile(file);
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
							if (checkUnusedKeysForFile(file))
								CheckUnusedKey(key, attributeName, file, usedKeys, emitWarning);
							CheckVariables(node, key, attributeName, file, usedKeys, emitError, emitWarning);
						}
					}
				}
			}

			return keyWithAttrs;

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

				if (translationReference.RequiredVariableNames != null && translationReference.RequiredVariableNames.Length > 0)
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

		class LogicCall
		{
			public string[] Logics { get; }

			public Dictionary<string, MiniYaml> LogicArgs { get; }

			public LogicCall(string[] logics, Dictionary<string, MiniYaml> logicArgs)
			{
				Logics = logics;
				LogicArgs = logicArgs;
			}
		}

		class RootContext
		{
			public sealed class Entry
			{
				public WidgetBounds Bounds { get; }
				public LogicCall[] Calls { get; }

				public Entry(WidgetBounds bounds, LogicCall[] calls)
				{
					Bounds = bounds;
					Calls = calls;
				}
			}

			public List<Entry> Entries { get; }

			RootContext(List<Entry> entries) { Entries = entries; }

			public static RootContext CreateEmpty()
			{
				return new RootContext(new List<Entry>());
			}

			public static RootContext CreateInitial(WidgetBounds bounds)
			{
				return new RootContext(new List<Entry>() { new(bounds, Array.Empty<LogicCall>()) });
			}

			public void Add(WidgetBounds bounds, IEnumerable<LogicCall> calls)
			{
				Entries.Add(new Entry(bounds, calls.ToArray()));
			}
		}
	}
}
