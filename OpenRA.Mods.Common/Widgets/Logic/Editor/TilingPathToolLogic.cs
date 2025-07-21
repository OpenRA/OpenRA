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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(
		typeof(UpdateTilingPathPlanEditorAction),
		typeof(PaintTilingPathEditorAction),
		typeof(TilingPathToolInfo))]
	public sealed class TilingPathToolLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public TilingPathToolLogic(
			Widget widget,
			World world,
			ModData modData,
			WorldRenderer worldRenderer,
			Dictionary<string, MiniYaml> logicArgs)
		{
			var tool = world.WorldActor.Trait<TilingPathTool>();
			var editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			var editorWidget = widget.Parent.Parent.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");

			((ScrollPanelWidget)widget).Layout.AdjustChildren();

			var editCheckbox = widget.Get<CheckboxWidget>("EDIT");
			editCheckbox.IsChecked = () => editorWidget.CurrentBrush is EditorTilingPathBrush;
			editCheckbox.OnClick = () =>
				editorWidget.SetBrush(
					editCheckbox.IsChecked()
						? null
						: new EditorTilingPathBrush(tool));

			void SetupDropDown(
				DropDownButtonWidget dropDown,
				ImmutableArray<string> choices,
				Func<string> read,
				Action<string> write)
			{
				dropDown.OnMouseDown = _ =>
				{
					ScrollItemWidget SetupItem(string choice, ScrollItemWidget template)
					{
						bool IsSelected() => choice == read();
						void OnClick() => write(choice);
						var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => choice;
						return item;
					}

					dropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", choices.Length * 30, choices, SetupItem);
				};
			}

			var startDropdown = widget
				.Get<ContainerWidget>("START_TYPE")
				.Get<DropDownButtonWidget>("DROPDOWN");
			startDropdown.GetText = () => tool.StartType;

			var endDropdown = widget
				.Get<ContainerWidget>("END_TYPE")
				.Get<DropDownButtonWidget>("DROPDOWN");
			endDropdown.GetText = () => tool.EndType;

			var innerDropDown = widget
				.Get<ContainerWidget>("INNER_TYPE")
				.Get<DropDownButtonWidget>("DROPDOWN");
			innerDropDown.GetText = () => tool.InnerType;

			SetupDropDown(startDropdown, tool.StartTypesByInner[tool.InnerType], () => tool.StartType, tool.SetStartType);
			SetupDropDown(endDropdown, tool.EndTypesByInner[tool.InnerType], () => tool.EndType, tool.SetEndType);

			void PickInnerType(string choice)
			{
				tool.SetInnerType(choice);
				SetupDropDown(startDropdown, tool.StartTypesByInner[choice], () => tool.StartType, tool.SetStartType);
				SetupDropDown(endDropdown, tool.EndTypesByInner[choice], () => tool.EndType, tool.SetEndType);
			}

			SetupDropDown(innerDropDown, tool.InnerTypes, () => tool.InnerType, PickInnerType);

			var deviationSlider = widget.Get<ContainerWidget>("DEVIATION").Get<SliderWidget>("SLIDER");
			deviationSlider.GetValue = () => tool.MaxDeviation;
			deviationSlider.OnChange += (value) => tool.SetMaxDeviation((int)value);

			var closedLoopsCheckbox = widget.Get<CheckboxWidget>("CLOSED_LOOPS");
			closedLoopsCheckbox.IsChecked = () => tool.ClosedLoops;
			closedLoopsCheckbox.OnClick = () => tool.SetClosedLoops(!tool.ClosedLoops);

			var resetButton = widget.Get<ButtonWidget>("RESET");
			resetButton.IsDisabled = () => tool.Plan == null;
			resetButton.OnClick = () => editorActionManager.Add(
				new UpdateTilingPathPlanEditorAction(tool, null));

			var reverseButton = widget.Get<ButtonWidget>("REVERSE");
			reverseButton.IsDisabled = () => tool.Plan == null;
			reverseButton.OnClick = () => editorActionManager.Add(
				new UpdateTilingPathPlanEditorAction(tool, tool.Plan.Reversed()));

			var randomizeButton = widget.Get<ButtonWidget>("RANDOMIZE");
			randomizeButton.IsDisabled = () => tool.Plan == null;
			randomizeButton.OnClick = () => tool.SetRandomSeed(Environment.TickCount);

			var paintButton = widget.Get<ButtonWidget>("PAINT");
			paintButton.IsDisabled = () => tool.EditorBlitSource == null;
			paintButton.OnClick = () => editorActionManager.Add(
				new PaintTilingPathEditorAction(tool));
		}
	}
}
