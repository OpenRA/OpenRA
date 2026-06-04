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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public sealed class MapEditorHelpLogic : ChromeLogic
	{
		readonly struct HelpRow
		{
			public readonly string Key;
			public readonly string Description;

			public HelpRow(string key, string description)
			{
				Key = key;
				Description = description;
			}
		}

		readonly struct HelpSection
		{
			public readonly string Title;
			public readonly string Intro;
			public readonly HelpRow[] Rows;

			public HelpSection(string title, string intro, HelpRow[] rows)
			{
				Title = title;
				Intro = intro;
				Rows = rows ?? [];
			}
		}

		[FluentReference]
		const string Title = "label-editor-help-title";

		[FluentReference]
		const string Overview = "label-editor-help-overview";

		[FluentReference]
		const string HotkeysHeader = "label-editor-help-hotkeys-header";

		[FluentReference]
		const string ExtraHotkeysHeader = "label-editor-help-extra-hotkeys-header";

		const string EditorContext = HotkeyDefinition.ContextFluentPrefix + ".editor";
		const int KeyColumnPadding = 10;

		static readonly (string GroupKey, FrozenSet<string> Types)[] HotkeyGroups =
		[
			("hotkey-group-editor-commands", new HashSet<string> { "Editor" }.ToFrozenSet()),
			("hotkey-group-viewport-commands", new HashSet<string> { "Viewport" }.ToFrozenSet()),
		];

		static readonly string[] ExtraEditorHotkeys =
		[
			"EditorQuickSave",
			"TakeScreenshot",
			"ToggleMute",
			"DisableUserInterface",
			"DisableAllUserInterface",
			"StopMusic",
			"PauseMusic",
			"PrevMusic",
			"NextMusic",
			"ResetZoom",
		];

		[ObjectCreator.UseCtor]
		public MapEditorHelpLogic(Widget widget, ModData modData)
		{
			widget.Get<LabelWidget>("TITLE").GetText = () => FluentProvider.GetMessage(Title);

			var scrollPanel = widget.Get<ScrollPanelWidget>("CONTENT_PANEL");
			var content = scrollPanel.Get<MapEditorHelpLabelWidget>("CONTENT");
			var font = Game.Renderer.Fonts[content.Font];
			var descFont = Game.Renderer.Fonts[content.DescriptionFont];
			var introFont = Game.Renderer.Fonts.TryGetValue(content.SectionIntroFont, out var italic)
				? italic
				: descFont;
			var text = BuildHelpText(modData, font, descFont, content.Bounds.Width, out var descColumnX);
			content.DescriptionColumnX = descColumnX;
			var height = MapEditorHelpLabelWidget.MeasureHelpTextHeight(text, font, descFont, introFont);

			Game.RunAfterTick(() =>
			{
				content.GetText = () => text;
				content.Bounds.Height = height;
				scrollPanel.Layout.AdjustChildren();
			});

			var closeButton = widget.Get<ButtonWidget>("CLOSE_BUTTON");
			closeButton.OnClick = Ui.CloseWindow;
		}

		static string BuildHelpText(ModData modData, SpriteFont font, SpriteFont descFont, int contentWidth, out int descColumnX)
		{
			var sections = BuildSections(modData);
			var allRows = sections.SelectMany(s => s.Rows).ToList();
			descColumnX = MeasureKeyColumnWidth(allRows, font) + KeyColumnPadding;

			var sb = new StringBuilder();
			sb.AppendLine($"%{FluentProvider.GetMessage(Overview)}%");
			sb.AppendLine();

			foreach (var section in sections)
				AppendSectionText(sb, section, descColumnX, contentWidth, descFont);

			return SanitizeText(sb.ToString().TrimEnd());
		}

		static List<HelpSection> BuildSections(ModData modData)
		{
			var sections = new List<HelpSection>
			{
				new("Top bar", null, TopBarRows(modData)),
				new("Map canvas", null, MapCanvasRows(modData)),
				new("Selected Preview", null, SelectedPreviewRows(modData)),
				new("Asset browser tabs", null, TabRows()),
				new("Tiles tab", "Search and category filter for terrain templates.", TileRows()),
				new("Overlays tab", "Paint overlay layers defined by the tileset.", []),
				new("Actors tab", "Search, filter, and set Owner before placing.", ActorRows()),
				new("Tools tab", "Generators, markers, path tiler, move tool, etc.", ToolRows(modData)),
				new("History tab", "Re-select a recent tile or actor.", HistoryRows()),
				new("Mouse and placement", null, MouseRows(modData)),
				new("Modifier keys", "Left or right Ctrl, Shift, and Alt work the same.", ModifierRows()),
			};

			sections.AddRange(BuildHotkeySections(modData));
			return sections;
		}

		static IEnumerable<HelpSection> BuildHotkeySections(ModData modData)
		{
			yield return new HelpSection(FluentProvider.GetMessage(HotkeysHeader), null, []);

			foreach (var (groupKey, types) in HotkeyGroups)
			{
				var entries = modData.Hotkeys.Definitions
					.Where(hd => hd.Contexts.Contains(EditorContext) && hd.Types.Overlaps(types))
					.OrderBy(hd => FluentProvider.GetMessage(hd.Description), StringComparer.CurrentCultureIgnoreCase)
					.Select(hd => HotkeyRow(modData, hd))
					.ToArray();

				if (entries.Length > 0)
					yield return new HelpSection(FluentProvider.GetMessage(groupKey), null, entries);
			}

			var extra = ExtraEditorHotkeys
				.Select(name => modData.Hotkeys.Definitions.FirstOrDefault(hd => hd.Name == name))
				.Where(hd => hd != null)
				.Select(hd => HotkeyRow(modData, hd))
				.ToArray();

			if (extra.Length > 0)
				yield return new HelpSection(FluentProvider.GetMessage(ExtraHotkeysHeader), null, extra);
		}

		static void AppendSectionText(StringBuilder sb, HelpSection section, int descColumnX, int contentWidth, SpriteFont descFont)
		{
			sb.AppendLine($"[{section.Title}]");

			if (!string.IsNullOrEmpty(section.Intro))
			{
				sb.AppendLine($"%{section.Intro}%");
				if (section.Rows.Length > 0)
					sb.AppendLine();
			}

			foreach (var line in FormatSectionRows(section.Rows, descColumnX, contentWidth, descFont))
				sb.AppendLine(line);

			sb.AppendLine();
		}

		static IEnumerable<string> FormatSectionRows(HelpRow[] rows, int descColumnX, int contentWidth, SpriteFont descFont)
		{
			var descWidth = Math.Max(40, contentWidth - descColumnX - KeyColumnPadding);

			foreach (var row in rows)
			{
				if (string.IsNullOrEmpty(row.Key))
				{
					yield return $"~{row.Description}~";
					continue;
				}

				var wrappedDesc = WidgetUtils.WrapText(row.Description, descWidth, descFont);
				var descLines = wrappedDesc.Split('\n', StringSplitOptions.RemoveEmptyEntries);

				for (var i = 0; i < descLines.Length; i++)
				{
					if (i == 0)
						yield return $"<{row.Key}>~{descLines[i]}~";
					else
						yield return $"|~{descLines[i]}~";
				}
			}
		}

		static int MeasureKeyColumnWidth(IEnumerable<HelpRow> rows, SpriteFont font)
		{
			var max = 0;
			foreach (var row in rows)
			{
				if (string.IsNullOrEmpty(row.Key))
					continue;

				max = Math.Max(max, font.Measure(row.Key).X);
			}

			return max;
		}

		static HelpRow[] TopBarRows(ModData modData) =>
		[
			new("Menu", "Open the editor menu (save, load, quit, settings)."),
			new("Escape", "Also opens the Menu."),
			new(Hotkey(modData, "EditorCopy", "Copy"), "Copy the selected area (toolbar icon does the same)."),
			new(Hotkey(modData, "EditorPaste", "Paste"), "Paste the saved area."),
			new(Hotkey(modData, "EditorUndo", "Undo"), "Undo the last edit."),
			new(Hotkey(modData, "EditorRedo", "Redo"), "Redo the last undone edit."),
			new("Overlays", "Toggle grid, buildable, walkable, ship paths, markers, tile/actor bounds."),
			new("Coordinates", "Cursor cell, height, and tile type under the mouse."),
		];

		static HelpRow[] MapCanvasRows(ModData modData) =>
		[
			new("Left drag", "Draw a rectangular selection on empty map."),
			new("Shift", "While dragging, add cells to the selection."),
			new("Alt", "While dragging, remove cells from the selection."),
			new("Right-click", "Cancel the active brush or clear placement."),
			new("Arrow keys", "Scroll the map view."),
			new(Hotkey(modData, "MapJumpToTopEdge", "Alt + Up"), "Jump view to the top edge (other arrows similar)."),
			new(Hotkey(modData, "MapBookmarkSave01", "Ctrl + Q"), "Save view bookmark 1 (W/E/R for 2-4)."),
			new(Hotkey(modData, "MapBookmarkRestore01", "Alt + Q"), "Restore view bookmark 1 (W/E/R for 2-4)."),
			new(Hotkey(modData, "ZoomOut", "["), "Zoom out."),
			new(Hotkey(modData, "ZoomIn", "]"), "Zoom in."),
			new(Hotkey(modData, "ResetZoom", "."), "Reset zoom."),
			new(Hotkey(modData, "DisableUserInterface", "Shift + ="), "Hide editor chrome."),
			new(Hotkey(modData, "DisableAllUserInterface", "Shift + Alt + ="), "Hide all UI."),
		];

		static HelpRow[] SelectedPreviewRows(ModData modData) =>
		[
			new("i", "Open this help window."),
			new("X", "Hide the Selected Preview panel."),
			new("Current / Original", "Toggle tile placement preview mode."),
			new("Mix", "Random or sequential fill order for multiple tiles."),
			new("Opposites / Similar", "Quick-pick related shore tiles (Island / Ring)."),
			new("Train", "Open tile metadata training (local dev)."),
			new("Filters", "Terrain / Resources / Actors for copy, cut, paste, fill, replace."),
			new(Hotkey(modData, "EditorCut", "Cut"), "Copy and remove the selection."),
			new(Hotkey(modData, "EditorCopy", "Copy"), "Copy the selection."),
			new(Hotkey(modData, "EditorPaste", "Paste"), "Paste at the selection."),
			new(Hotkey(modData, "EditorDeleteSelection", "Delete"), "Remove the selection."),
			new("Fill", "Fill with the active tile, resource, or actor."),
			new("Replace", "Swap layers in the selection for another asset."),
			new("Rotate Left/Right", "Rotate copied data."),
			new("Clear Copy", "Discard the clipboard."),
			new("Find", "Jump to the selection in the asset browser."),
			new("Cancel", "Clear the selection rectangle."),
		];

		static HelpRow[] TabRows() =>
		[
			new("E", "Selection tab - area tools and Selected Preview."),
			new("R", "Tiles tab - terrain picker."),
			new("T", "Overlays tab - overlay layers."),
			new("Y", "Actors tab - place actors."),
			new("U", "Tools tab - generators and utilities."),
			new("I", "History tab - recent placements."),
		];

		static HelpRow[] TileRows() =>
		[
			new("Left drag", "Paint the selected terrain template."),
			new("Search", "Match id, filename, categories, and terrain types."),
		];

		static HelpRow[] ActorRows() =>
		[
			new("Click", "Place the selected actor."),
			new("Selected Preview", "Edit ID, facings, and trait options when an actor is selected."),
		];

		static HelpRow[] ToolRows(ModData modData) =>
		[
			new("W/A/S/D", "Nudge the move-tool preview."),
			new("Enter", "Apply move (Place mouse button also applies)."),
		];

		static HelpRow[] HistoryRows() =>
		[
			new("Click entry", "Select that tile or actor again in the browser."),
		];

		static HelpRow[] MouseRows(ModData modData) =>
		[
			new("Left button", "Paint, place, or drag a selection."),
			new("Right button", "Cancel brush / abort operation."),
			new("Mouse wheel", "Not used for zoom."),
			new($"{Hotkey(modData, "ZoomOut", "[")} / {Hotkey(modData, "ZoomIn", "]")}", "Zoom keys."),
		];

		static HelpRow[] ModifierRows() =>
		[
			new("Ctrl", "Hold with shortcuts: copy, cut, paste, undo, redo, save, tabs, bookmarks."),
			new("Shift", "Add to selection while dragging; hide UI with Shift + =."),
			new("Alt", "Subtract from selection while dragging; jump to edges; restore bookmarks."),
		];

		static HelpRow HotkeyRow(ModData modData, HotkeyDefinition hd)
		{
			var key = modData.Hotkeys[hd.Name].GetValue().DisplayString();
			var description = FluentProvider.GetMessage(hd.Description);
			return new HelpRow(key, description);
		}

		static string Hotkey(ModData modData, string name, string fallback)
		{
			if (!modData.Hotkeys.Definitions.Any(hd => hd.Name == name))
				return fallback;

			return modData.Hotkeys[name].GetValue().DisplayString();
		}

		static string SanitizeText(string text)
		{
			return text
				.Replace("\r\n", "\n", StringComparison.Ordinal)
				.Replace('\r', '\n')
				.Replace('\u2013', '-')
				.Replace('\u2014', '-');
		}

		static string StripMarkup(string text)
		{
			return SanitizeText(text)
				.Replace("[", "", StringComparison.Ordinal)
				.Replace("]", "", StringComparison.Ordinal)
				.Replace("<", "", StringComparison.Ordinal)
				.Replace(">", "", StringComparison.Ordinal)
				.Replace("~", "", StringComparison.Ordinal)
				.Replace("|", "", StringComparison.Ordinal)
				.Replace("%", "", StringComparison.Ordinal);
		}
	}
}
