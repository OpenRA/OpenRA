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
using System.Threading;
using System.Threading.Tasks;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.GeoMapGenerator;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class GeoMapGeneratorLogic : ChromeLogic
	{
		static readonly int[] MapSizes = { 128, 256, 512 };
		static readonly double[] MetersPerCellOptions = { 4.0, 8.0, 16.0 };

		int selectedSize = 128;
		double selectedMpc = 8.0;

		volatile bool generating;
		volatile string statusText = "";
		volatile int progressValue;

		Map generatedMap;
		CancellationTokenSource cts;

		[ObjectCreator.UseCtor]
		public GeoMapGeneratorLogic(Widget widget, ModData modData, Action<string> onSelect, Action onExit)
		{
			var panel = widget;

			// MGRS input
			var mgrsField = panel.Get<TextFieldWidget>("MGRS_TEXTFIELD");

			// Size dropdown
			var sizeDropdown = panel.Get<DropDownButtonWidget>("SIZE_DROPDOWN");
			sizeDropdown.GetText = () => $"{selectedSize} x {selectedSize}";
			sizeDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupSizeItem(int size, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template,
						() => selectedSize == size,
						() => selectedSize = size);
					item.Get<LabelWidget>("LABEL").GetText = () => $"{size} x {size}";
					return item;
				}

				sizeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE",
					MapSizes.Length * 30, MapSizes, SetupSizeItem);
			};

			// Meters per cell dropdown
			var mpcDropdown = panel.Get<DropDownButtonWidget>("MPC_DROPDOWN");
			mpcDropdown.GetText = () => $"{selectedMpc:F1} m/cell";
			mpcDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupMpcItem(double mpc, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template,
						() => Math.Abs(selectedMpc - mpc) < 0.01,
						() => selectedMpc = mpc);
					item.Get<LabelWidget>("LABEL").GetText = () => $"{mpc:F1} m/cell";
					return item;
				}

				mpcDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE",
					MetersPerCellOptions.Length * 30, MetersPerCellOptions, SetupMpcItem);
			};

			// Feature checkboxes
			var roadsEnabled = true;
			var roadsCheckbox = panel.Get<CheckboxWidget>("ROADS_CHECKBOX");
			roadsCheckbox.IsChecked = () => roadsEnabled;
			roadsCheckbox.OnClick = () => roadsEnabled = !roadsEnabled;

			var waterEnabled = true;
			var waterCheckbox = panel.Get<CheckboxWidget>("WATER_CHECKBOX");
			waterCheckbox.IsChecked = () => waterEnabled;
			waterCheckbox.OnClick = () => waterEnabled = !waterEnabled;

			var vegEnabled = true;
			var vegetationCheckbox = panel.Get<CheckboxWidget>("VEGETATION_CHECKBOX");
			vegetationCheckbox.IsChecked = () => vegEnabled;
			vegetationCheckbox.OnClick = () => vegEnabled = !vegEnabled;

			var buildingsEnabled = true;
			var buildingsCheckbox = panel.Get<CheckboxWidget>("BUILDINGS_CHECKBOX");
			buildingsCheckbox.IsChecked = () => buildingsEnabled;
			buildingsCheckbox.OnClick = () => buildingsEnabled = !buildingsEnabled;

			var coastlineEnabled = true;
			var coastlineCheckbox = panel.Get<CheckboxWidget>("COASTLINE_CHECKBOX");
			coastlineCheckbox.IsChecked = () => coastlineEnabled;
			coastlineCheckbox.OnClick = () => coastlineEnabled = !coastlineEnabled;

			var invertCoastline = false;
			var invertCoastlineCheckbox = panel.Get<CheckboxWidget>("INVERT_COASTLINE_CHECKBOX");
			invertCoastlineCheckbox.IsChecked = () => invertCoastline;
			invertCoastlineCheckbox.OnClick = () => invertCoastline = !invertCoastline;

			// Status and progress
			var statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");
			statusLabel.GetText = () => statusText;

			var progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			progressBar.GetPercentage = () => progressValue;

			var statsLabel = panel.Get<LabelWidget>("STATS_LABEL");
			statsLabel.GetText = () => "";

			// Generate button
			var generateButton = panel.Get<ButtonWidget>("GENERATE_BUTTON");
			generateButton.IsDisabled = () => generating;
			generateButton.OnClick = () =>
			{
				var mgrs = mgrsField.Text?.Trim();
				if (string.IsNullOrWhiteSpace(mgrs))
				{
					statusText = "Please enter an MGRS coordinate.";
					return;
				}

				var opts = new GeoMapOptions
				{
					MgrsCoordinate = mgrs,
					Cells = selectedSize,
					MetersPerCell = selectedMpc,
					IncludeRoads = roadsEnabled,
					IncludeWater = waterEnabled,
					IncludeVegetation = vegEnabled,
					IncludeBuildings = buildingsEnabled,
					IncludeCoastline = coastlineEnabled,
					InvertCoastline = invertCoastline,
				};

				RunGeneration(modData, opts);
			};

			// Open in Map Editor button
			var openEditorButton = panel.Get<ButtonWidget>("OPEN_EDITOR_BUTTON");
			openEditorButton.IsDisabled = () => generatedMap == null || generating;
			openEditorButton.OnClick = () =>
			{
				if (generatedMap == null) return;
				Game.LoadEditor(generatedMap);
				Ui.CloseWindow();
				onSelect?.Invoke(generatedMap.Uid);
			};

			// Cancel button
			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				cts?.Cancel();
				Ui.CloseWindow();
				onExit?.Invoke();
			};

			statusText = "Enter MGRS coordinate and click Generate.";
		}

		void RunGeneration(ModData modData, GeoMapOptions options)
		{
			generating = true;
			generatedMap = null;
			cts = new CancellationTokenSource();
			var token = cts.Token;

			Task.Run(() =>
			{
				try
				{
					using var builder = new GeoMapBuilder();
					var map = builder.Generate(modData, options, (msg, pct) =>
					{
						statusText = msg;
						progressValue = pct;
					}, token);

					Game.RunAfterTick(() =>
					{
						try
						{
							var package = new ZipFileLoader.ReadWriteZipFile();
							map.Save(package);
							generatedMap = new Map(modData, package);
							generating = false;
							statusText = "Map generated! Click 'Open in Map Editor'.";
							progressValue = 100;
						}
						catch (Exception ex)
						{
							generating = false;
							statusText = $"Save error: {ex.Message}";
							progressValue = 0;
						}
					});
				}
				catch (OperationCanceledException)
				{
					Game.RunAfterTick(() =>
					{
						generating = false;
						statusText = "Generation cancelled.";
						progressValue = 0;
					});
				}
				catch (Exception ex)
				{
					Game.RunAfterTick(() =>
					{
						generating = false;
						statusText = $"Error: {ex.Message}";
						progressValue = 0;
					});
				}
			}, token);
		}
	}
}
