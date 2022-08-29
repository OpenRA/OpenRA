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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public abstract class CommonSelectorLogic : ChromeLogic
	{
		protected readonly Widget Widget;
		protected readonly ModData ModData;
		protected readonly TextFieldWidget SearchTextField;
		protected readonly World World;
		protected readonly WorldRenderer WorldRenderer;
		protected readonly EditorViewportControllerWidget Editor;
		protected readonly ScrollPanelWidget Panel;
		protected readonly ScrollItemWidget ItemTemplate;

		protected readonly HashSet<string> SelectedCategories = new HashSet<string>();
		protected readonly List<string> FilteredCategories = new List<string>();

		protected string[] allCategories;
		protected string searchFilter;

		[TranslationReference]
		static readonly string None = "none";

		[TranslationReference]
		static readonly string SearchResults = "search-results";

		[TranslationReference]
		static readonly string All = "all";

		[TranslationReference]
		static readonly string Multiple = "multiple";

		public CommonSelectorLogic(Widget widget, ModData modData, World world, WorldRenderer worldRenderer, string templateListId, string previewTemplateId)
		{
			Widget = widget;
			ModData = modData;
			World = world;
			WorldRenderer = worldRenderer;
			Editor = widget.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			Panel = widget.Get<ScrollPanelWidget>(templateListId);
			ItemTemplate = Panel.Get<ScrollItemWidget>(previewTemplateId);
			Panel.Layout = new GridLayout(Panel);

			SearchTextField = widget.Get<TextFieldWidget>("SEARCH_TEXTFIELD");
			SearchTextField.OnEscKey = _ =>
			{
				if (string.IsNullOrEmpty(SearchTextField.Text))
					SearchTextField.YieldKeyboardFocus();
				else
				{
					SearchTextField.Text = "";
					SearchTextField.OnTextEdited();
				}

				return true;
			};

			var none = ModData.Translation.GetString(None);
			var searchResults = ModData.Translation.GetString(SearchResults);
			var all = ModData.Translation.GetString(All);
			var multiple = ModData.Translation.GetString(Multiple);

			var categorySelector = widget.Get<DropDownButtonWidget>("CATEGORIES_DROPDOWN");
			categorySelector.GetText = () =>
			{
				if (SelectedCategories.Count == 0)
					return none;

				if (!string.IsNullOrEmpty(searchFilter))
					return searchResults;

				if (SelectedCategories.Count == 1)
					return SelectedCategories.First();

				if (SelectedCategories.Count == allCategories.Length)
					return all;

				return multiple;
			};

			categorySelector.OnMouseDown = _ =>
			{
				SearchTextField?.YieldKeyboardFocus();

				categorySelector.RemovePanel();
				categorySelector.AttachPanel(CreateCategoriesPanel(Panel));
			};
		}

		protected Widget CreateCategoriesPanel(ScrollPanelWidget panel)
		{
			var categoriesPanel = Ui.LoadWidget("CATEGORY_FILTER_PANEL", null, new WidgetArgs());
			var categoryTemplate = categoriesPanel.Get<CheckboxWidget>("CATEGORY_TEMPLATE");

			var selectButtons = categoriesPanel.Get<ContainerWidget>("SELECT_CATEGORIES_BUTTONS");
			categoriesPanel.AddChild(selectButtons);

			var selectAll = selectButtons.Get<ButtonWidget>("SELECT_ALL");
			selectAll.OnClick = () =>
			{
				SelectedCategories.Clear();
				foreach (var c in allCategories)
					SelectedCategories.Add(c);

				InitializePreviews();
			};

			var selectNone = selectButtons.Get<ButtonWidget>("SELECT_NONE");
			selectNone.OnClick = () =>
			{
				SelectedCategories.Clear();
				InitializePreviews();
			};

			var categoryHeight = 5 + selectButtons.Bounds.Height;
			foreach (var cat in FilteredCategories)
			{
				var category = (CheckboxWidget)categoryTemplate.Clone();
				category.GetText = () => cat;
				category.IsChecked = () => SelectedCategories.Contains(cat);
				category.IsVisible = () => true;
				category.OnClick = () =>
				{
					if (!SelectedCategories.Remove(cat))
						SelectedCategories.Add(cat);

					InitializePreviews();
				};

				categoriesPanel.AddChild(category);
				categoryHeight += categoryTemplate.Bounds.Height;
			}

			categoriesPanel.Bounds.Height = Math.Min(categoryHeight, panel.Bounds.Height);

			return categoriesPanel;
		}

		protected abstract void InitializePreviews();
	}
}
