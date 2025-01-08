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
using System.Collections.Immutable;
using System.Linq;

namespace OpenRA.Mods.Common.MapGenerator
{
	public sealed class MapGeneratorSettings
	{
		/// <summary>How an option should be treated for UI purposes.</summary>
		public enum UiType
		{
			Hidden,
			DropDown,
			Checkbox,
			Integer,
			Float,
			String,
		}

		/// <summary>Represents a bunch of settings, along with UI information.</summary>
		public sealed class Choice
		{
			/// <summary>Uniquely identifies a Choice within an Option.</summary>
			public readonly string Id;

			/// <summary>The label to use for UI selection. Post-fluent.</summary>
			public readonly string Label = null;

			/// <summary>
			/// Only offer the Choice for these tilesets. (If null, show for all.)
			/// </summary>
			public readonly IReadOnlySet<string> Tileset = null;

			/// <summary>(Partial) settings to combine into the final overall settings.</summary>
			public readonly MiniYaml Settings;

			public Choice(string id, MiniYaml my)
			{
				Id = id;
				var label = my.NodeWithKeyOrDefault("Label")?.Value.Value;
				if (label != null)
					Label = FluentProvider.GetMessage(label);
				Tileset = my.NodeWithKeyOrDefault("Tileset")?.Value.Value
					?.Split(',')
					.ToImmutableHashSet();
				Settings = my.NodeWithKey("Settings").Value;
			}

			/// <summary>Create a choice that represents a top-level setting with a given value.</summary>
			public Choice(string setting, string value)
			{
				Id = value;
				Label = value;
				Tileset = null;
				Settings = new MiniYaml(null, new[] { new MiniYamlNode(setting, value) });
			}

			public static void DumpFluent(MiniYaml my, List<string> references)
			{
				var label = my.NodeWithKeyOrDefault("Label")?.Value.Value;
				if (label != null)
					references.Add(label);
			}

			/// <summary>Check whether this choice is permitted for this map.</summary>
			public bool Allowed(Map map)
			{
				if (Tileset != null && !Tileset.Contains(map.Tileset))
					return false;

				return true;
			}

			/// <summary>For single-setting choices, creates a new choice for that setting with a given value.</summary>
			public Choice NewValue(string value)
			{
				if (Settings.Nodes.Length != 1)
					throw new InvalidOperationException("NewValue can only be used on single-setting Choices");
				return new(Settings.Nodes[0].Key, value);
			}
		}

		public sealed class Option
		{
			/// <summary>Unique identifier for this Option.</summary>
			[FieldLoader.Ignore]
			public readonly string Id;

			/// <summary>The label to use for UI selection. Post-fluent.</summary>
			[FieldLoader.Ignore]
			public readonly string Label = null;

			/// <summary>
			/// <para>For multiple-choice options (dropdowns/checkboxes) holds the allowed Choices.</para>
			/// <para>For text entry Options, contains the default value.</para>
			/// <para>If this is empty, the map is not compatible with the generator.</para>
			/// </summary>
			[FieldLoader.Ignore]
			public readonly IReadOnlyList<Choice> Choices;

			/// <summary>
			/// <para>The default Choice for this option.</para>
			/// <para>Default will be ignored if it is not valid for the map.</para>
			/// </summary>
			[FieldLoader.Ignore]
			public readonly Choice Default;

			public readonly double Min = double.NegativeInfinity;
			public readonly double Max = double.PositiveInfinity;

			/// <summary>Whether the Option can be randomized.</summary>
			public readonly bool Random = false;

			/// <summary>How an option should be treated for UI purposes.</summary>
			[FieldLoader.Ignore]
			public readonly UiType Ui;

			/// <summary>Settings layering priority. Higher overrides lower.</summary>
			public readonly int Priority = 0;

			public Option(string id, MiniYaml my, Map map)
			{
				Id = id;
				FieldLoader.Load(this, my);
				var label = my.NodeWithKeyOrDefault("Label")?.Value.Value;
				if (label != null)
					Label = FluentProvider.GetMessage(label);

				var choices = new List<Choice>();
				Choices = choices;

				var integerSetting = my.NodeWithKeyOrDefault("Integer");
				var floatSetting = my.NodeWithKeyOrDefault("Float");
				var stringSetting = my.NodeWithKeyOrDefault("String");
				var checkboxSetting = my.NodeWithKeyOrDefault("Checkbox");
				var simpleChoiceSetting = my.NodeWithKeyOrDefault("SimpleChoice");
				var textSetting = integerSetting ?? floatSetting ?? stringSetting;
				if (textSetting != null)
				{
					var setting = textSetting.Value.Value;
					var value = my.NodeWithKeyOrDefault("Default")?.Value.Value ?? "";
					var choice = new Choice(setting, value);
					choices.Add(choice);
					Default = choice;
					Ui =
						integerSetting != null ? UiType.Integer :
						floatSetting != null ? UiType.Float :
						UiType.String;

					if (Ui == UiType.Integer)
					{
						if (Min == double.NegativeInfinity)
							Min = int.MinValue;
						if (Max == double.PositiveInfinity)
							Max = int.MaxValue;
					}
				}
				else if (checkboxSetting != null)
				{
					var setting = checkboxSetting.Value.Value;
					var falseChoice = new Choice(setting, "False");
					var trueChoice = new Choice(setting, "True");
					choices.Add(falseChoice);
					choices.Add(trueChoice);
					var enabled = (my.NodeWithKeyOrDefault("Default")?.Value.Value ?? "False") == "True";
					Default = enabled ? trueChoice : falseChoice;
					Ui = UiType.Checkbox;
				}
				else
				{
					if (simpleChoiceSetting != null)
					{
						var setting = simpleChoiceSetting.Value.Value;
						var values = simpleChoiceSetting.Value
							.NodeWithKey("Values").Value.Value
							.Split(',');
						foreach (var value in values)
							choices.Add(new Choice(setting, value));
					}
					else
					{
						foreach (var node in my.Nodes)
						{
							var split = node.Key.Split('@');
							if (split[0] == "Choice")
							{
								string choiceId = null;
								if (split.Length >= 2)
									choiceId = split[1];
								var choice = new Choice(choiceId, node.Value);
								if (choice.Allowed(map))
									choices.Add(choice);
							}
						}
					}

					if (Choices.Count > 0)
					{
						var defaultOrder = my.NodeWithKeyOrDefault("Default")?.Value.Value;
						if (defaultOrder != null)
						{
							foreach (var defaultChoice in defaultOrder.Split(','))
							{
								Default = Choices.FirstOrDefault(choice => choice.Id == defaultChoice);
								if (Default != null)
									break;
							}

							if (Default == null)
								throw new YamlException($"None of option `{id}`'s default choices `{defaultOrder}` are not valid");
						}
						else
						{
							Default = Choices[0];
						}
					}

					Ui = Label == null ? UiType.Hidden : UiType.DropDown;
				}
			}

			public static void DumpFluent(MiniYaml my, List<string> references)
			{
				var label = my.NodeWithKeyOrDefault("Label")?.Value.Value;
				if (label != null)
					references.Add(label);
				foreach (var node in my.Nodes)
					if (node.Key.Split('@')[0] == "Choice")
						Choice.DumpFluent(node.Value, references);
			}

			public bool ValidateChoice(Choice choice)
			{
				switch (Ui)
				{
					case UiType.Integer:
					{
						var valid = long.TryParse(choice.Id, out var value);
						if (value < Min || value > Max)
							valid = false;
						return valid;
					}

					case UiType.Float:
					{
						var valid = double.TryParse(choice.Id, out var value);
						if (value < Min || value > Max)
							valid = false;
						return valid;
					}

					case UiType.String:
						return true;

					default:
						return Choices.Contains(choice);
				}
			}

			public Choice RandomChoice()
			{
				if (Random)
				{
					var random = (long)Guid.NewGuid().GetHashCode() - int.MinValue;
					switch (Ui)
					{
						case UiType.Integer:
							var intRandom = (int)(random % (long)(Max - Min + 1) + (long)Min);
							return Default.NewValue(intRandom.ToStringInvariant());
						case UiType.Float:
							var floatRandom = (float)((double)0xffffffff * random / (Max - Min) + Min);
							return Default.NewValue(floatRandom.ToStringInvariant());
						case UiType.Checkbox:
						case UiType.DropDown:
							if (Choices.Count == 0)
								throw new InvalidOperationException($"Map is not compatible with Option `{Id}`");
							return Choices[(int)(random % Choices.Count)];
						default:
							throw new InvalidOperationException($"Option `{Id}` does not have a randomizable type");
					}
				}
				else
				{
					return Default;
				}
			}
		}

		public readonly IReadOnlyList<Option> Options;

		/// <summary>
		/// Parse settings from a MiniYaml definition. Returns null if the map isn't compatible.
		/// </summary>
		public static MapGeneratorSettings LoadSettings(MiniYaml my, Map map)
		{
			var options = new List<Option>();

			foreach (var node in my.Nodes)
			{
				var split = node.Key.Split('@');
				if (split[0] == "Option")
				{
					string id = null;
					if (split.Length >= 2)
						id = split[1];
					var option = new Option(id, node.Value, map);
					if (option.Choices.Count == 0)
						return null;
					options.Add(option);
				}
			}

			return new MapGeneratorSettings(options);
		}

		public static List<string> DumpFluent(MiniYaml my)
		{
			var references = new List<string>();
			DumpFluent(my, references);
			return references;
		}

		public static void DumpFluent(MiniYaml my, List<string> references)
		{
			foreach (var node in my.Nodes)
				if (node.Key.Split('@')[0] == "Option")
					Option.DumpFluent(node.Value, references);
		}

		MapGeneratorSettings(IReadOnlyList<Option> options)
		{
			Options = options;
		}

		public Dictionary<Option, Choice> DefaultChoices()
		{
			return Options.ToDictionary(option => option, option => option.Default);
		}

		/// <summary>Merge all choices into a complete settings MiniYaml.</summary>
		public MiniYaml Compile(IReadOnlyDictionary<Option, Choice> choices)
		{
			var layers = new List<IReadOnlyCollection<MiniYamlNode>>();

			// Apply the choices in their canonical order.
			foreach (var option in Options.OrderBy(option => option.Priority))
			{
				var choice = choices[option];
				if (!option.ValidateChoice(choice))
					throw new ArgumentException($"Option `{option.Id}` has illegal choice");
				layers.Add(choice.Settings.Nodes);
			}

			var settingsNodes = MiniYaml.Merge(layers);
			return new MiniYaml(null, settingsNodes);
		}
	}
}
