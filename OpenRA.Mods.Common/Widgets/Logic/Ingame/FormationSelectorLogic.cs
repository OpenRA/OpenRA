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
using System.Linq;
using OpenRA;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class FormationSelectorLogic : ChromeLogic
	{
		enum FormationMenuKind { Preview, Shape, Spacing }

		sealed class FormationMenuEntry
		{
			public readonly FormationMenuKind Kind;
			public readonly FormationType Formation;
			public readonly FormationSpacing Spacing;

			FormationMenuEntry(FormationMenuKind kind, FormationType formation, FormationSpacing spacing)
			{
				Kind = kind;
				Formation = formation;
				Spacing = spacing;
			}

			public static FormationMenuEntry PreviewToggle() =>
				new(FormationMenuKind.Preview, default, default);

			public static FormationMenuEntry Shape(FormationType formation) =>
				new(FormationMenuKind.Shape, formation, default);

			public static FormationMenuEntry SpacingOption(FormationSpacing spacing) =>
				new(FormationMenuKind.Spacing, default, spacing);
		}

		[FluentReference]
		const string DefaultLabel = "options-formation-default";

		[FluentReference]
		const string SquareLabel = "options-formation-square";

		[FluentReference]
		const string CircleLabel = "options-formation-circle";

		[FluentReference]
		const string LineHorizontalLabel = "options-formation-line-horizontal";

		[FluentReference]
		const string LineVerticalLabel = "options-formation-line-vertical";

		[FluentReference]
		const string PyramidLabel = "options-formation-pyramid";

		[FluentReference]
		const string PyramidInvertedLabel = "options-formation-pyramid-inverted";

		[FluentReference]
		const string PyramidRightLabel = "options-formation-pyramid-right";

		[FluentReference]
		const string PyramidLeftLabel = "options-formation-pyramid-left";

		[FluentReference]
		const string VFormationLabel = "options-formation-v-formation";

		[FluentReference]
		const string VInvertedLabel = "options-formation-v-inverted";

		[FluentReference]
		const string VLeftLabel = "options-formation-v-left";

		[FluentReference]
		const string VRightLabel = "options-formation-v-right";

		[FluentReference]
		const string PreviewLabel = "options-formation-orange-preview";

		[FluentReference]
		const string ShapeGroupLabel = "options-formation-group-shape";

		[FluentReference]
		const string SpacingGroupLabel = "options-formation-group-spacing";

		[FluentReference]
		const string SpacingTightLabel = "options-formation-spacing-tight";

		[FluentReference]
		const string SpacingNormalLabel = "options-formation-spacing-normal";

		[FluentReference]
		const string SpacingMediumLabel = "options-formation-spacing-medium";

		[FluentReference]
		const string SpacingFarLabel = "options-formation-spacing-far";

		readonly World world;
		int selectionHash;
		bool formationDisabled = true;

		static readonly FormationType[] ShapeOptions =
		[
			FormationType.Default,
			FormationType.Square,
			FormationType.Circle,
			FormationType.LineHorizontal,
			FormationType.LineVertical,
			FormationType.Pyramid,
			FormationType.PyramidInverted,
			FormationType.PyramidRight,
			FormationType.PyramidLeft,
			FormationType.VFormation,
			FormationType.VInverted,
			FormationType.VLeft,
			FormationType.VRight,
		];

		static readonly FormationSpacing[] SpacingOptions =
		[
			FormationSpacing.Tight,
			FormationSpacing.Normal,
			FormationSpacing.Medium,
			FormationSpacing.Far,
		];

		[ObjectCreator.UseCtor]
		public FormationSelectorLogic(Widget widget, World world)
		{
			this.world = world;

			var formationButton = widget.GetOrNull<DropDownButtonWidget>("FORMATION");
			if (formationButton == null)
				return;

			WidgetUtils.BindButtonIcon(formationButton);
			FormationPreferences.SetFormationDropdown(formationButton);

			formationButton.IsDisabled = () => { UpdateStateIfNecessary(); return formationDisabled; };
			formationButton.IsHighlighted = () =>
				formationButton.IsPanelOpen
				|| FormationPreferences.Selected != FormationType.Default
				|| FormationPreferences.SelectedSpacing != FormationSpacing.Normal;

			formationButton.OnMouseDown = _ =>
			{
				if (formationButton.IsDisabled())
					return;

				if (formationButton.IsPanelOpen)
				{
					formationButton.RemovePanel();
					return;
				}

				var groups = new Dictionary<string, IEnumerable<FormationMenuEntry>>
				{
					{ "", [FormationMenuEntry.PreviewToggle()] },
					{ FluentProvider.GetMessage(ShapeGroupLabel), ShapeOptions.Select(FormationMenuEntry.Shape) },
					{ FluentProvider.GetMessage(SpacingGroupLabel), SpacingOptions.Select(FormationMenuEntry.SpacingOption) },
				};

				ScrollItemWidget SetupItem(FormationMenuEntry entry, ScrollItemWidget template)
				{
					bool IsSelected() => entry.Kind switch
					{
						FormationMenuKind.Shape => FormationPreferences.Selected == entry.Formation,
						FormationMenuKind.Spacing => FormationPreferences.SelectedSpacing == entry.Spacing,
						_ => false,
					};

					void OnClick()
					{
						if (entry.Kind == FormationMenuKind.Preview)
							FormationPreferences.OrangePreviewEnabled = !FormationPreferences.OrangePreviewEnabled;
						else if (entry.Kind == FormationMenuKind.Shape)
						{
							FormationPreferences.Selected = entry.Formation;
							FormationResolver.ApplyImmediateFormation(world, world.Selection.Actors, entry.Formation);
						}
						else
						{
							FormationPreferences.SelectedSpacing = entry.Spacing;
							FormationResolver.ApplyImmediateFormation(world, world.Selection.Actors, FormationPreferences.Selected);
						}
					}

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					var label = item.Get<LabelWidget>("LABEL");
					var checkbox = item.GetOrNull<CheckboxWidget>("CHECKBOX");

					if (entry.Kind == FormationMenuKind.Preview && checkbox != null)
					{
						label.IsVisible = () => false;
						checkbox.IsVisible = () => true;
						checkbox.IsChecked = () => FormationPreferences.OrangePreviewEnabled;
						checkbox.GetText = () => GetLabel(entry);
						checkbox.OnClick = OnClick;
					}
					else
					{
						if (checkbox != null)
							checkbox.IsVisible = () => false;

						label.GetText = () => GetLabel(entry);
					}

					return item;
				}

				var itemCount = 1 + ShapeOptions.Length + SpacingOptions.Length;
				formationButton.ShowDropDown("FORMATION_DROPDOWN_TEMPLATE", itemCount * 25 + 30, groups, SetupItem,
					closeOnSelect: false, dismissOnMaskClick: false, blockWorldClicks: false);
			};
		}

		void UpdateStateIfNecessary()
		{
			if (selectionHash == world.Selection.Hash)
				return;

			formationDisabled = !world.Selection.Actors
				.Any(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<IMoveInfo>());

			selectionHash = world.Selection.Hash;
		}

		static string GetLabel(FormationMenuEntry entry)
		{
			if (entry.Kind == FormationMenuKind.Preview)
				return FluentProvider.GetMessage(PreviewLabel);

			if (entry.Kind == FormationMenuKind.Spacing)
			{
				return entry.Spacing switch
				{
					FormationSpacing.Tight => FluentProvider.GetMessage(SpacingTightLabel),
					FormationSpacing.Normal => FluentProvider.GetMessage(SpacingNormalLabel),
					FormationSpacing.Medium => FluentProvider.GetMessage(SpacingMediumLabel),
					FormationSpacing.Far => FluentProvider.GetMessage(SpacingFarLabel),
					_ => entry.Spacing.ToString(),
				};
			}

			return GetShapeLabel(entry.Formation);
		}

		static string GetShapeLabel(FormationType option)
		{
			return option switch
			{
				FormationType.Default => FluentProvider.GetMessage(DefaultLabel),
				FormationType.Square => FluentProvider.GetMessage(SquareLabel),
				FormationType.Circle => FluentProvider.GetMessage(CircleLabel),
				FormationType.LineHorizontal => FluentProvider.GetMessage(LineHorizontalLabel),
				FormationType.LineVertical => FluentProvider.GetMessage(LineVerticalLabel),
				FormationType.Pyramid => FluentProvider.GetMessage(PyramidLabel),
				FormationType.PyramidInverted => FluentProvider.GetMessage(PyramidInvertedLabel),
				FormationType.PyramidRight => FluentProvider.GetMessage(PyramidRightLabel),
				FormationType.PyramidLeft => FluentProvider.GetMessage(PyramidLeftLabel),
				FormationType.VFormation => FluentProvider.GetMessage(VFormationLabel),
				FormationType.VInverted => FluentProvider.GetMessage(VInvertedLabel),
				FormationType.VLeft => FluentProvider.GetMessage(VLeftLabel),
				FormationType.VRight => FluentProvider.GetMessage(VRightLabel),
				_ => option.ToString(),
			};
		}
	}
}
