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
		const int MapSize = 512;
		const double MetersPerCell = 8.0;

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
					Cells = MapSize,
					MetersPerCell = MetersPerCell,
					IncludeRoads = roadsEnabled,
					IncludeWater = waterEnabled,
					IncludeVegetation = vegEnabled,
					IncludeBuildings = buildingsEnabled,
					IncludeCoastline = coastlineEnabled,
					InvertCoastline = invertCoastline,
				};

				RunGeneration(modData, opts);
			};

			// Open Map Editor button
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
				cts?.Dispose();
				cts = null;
				Ui.CloseWindow();
				onExit?.Invoke();
			};

			statusText = "Enter MGRS coordinate and click Generate.";
		}

		void RunGeneration(ModData modData, GeoMapOptions options)
		{
			generating = true;
			generatedMap = null;
			cts?.Dispose();
			cts = new CancellationTokenSource();
			var token = cts.Token;

			// Phase 1-4 run on a background thread (no OpenRA engine state touched).
			Task.Run(() =>
			{
				try
				{
					using var builder = new GeoMapBuilder();
					var data = builder.ComputeData(options, (msg, pct) =>
					{
						statusText = msg;
						progressValue = pct;
					}, token);

					// Phase 5: Build the Map on the main thread (Map constructor
					// calls PostInit/Ruleset.Load which touches shared modData).
					Game.RunAfterTick(() =>
					{
						try
						{
							statusText = "Building map...";
							progressValue = 92;

							var map = GeoMapBuilder.BuildMap(modData, data);

							statusText = "Saving map package...";
							progressValue = 95;

							var package = new ZipFileLoader.ReadWriteZipFile();
							map.Save(package);

							statusText = "Reloading map...";
							progressValue = 98;

							generatedMap = new Map(modData, package);
							generating = false;
							statusText = "Map generated! Click 'Open Map Editor'.";
							progressValue = 100;
						}
						catch (Exception ex)
						{
							generating = false;
							statusText = $"Error: {ex.Message}";
							progressValue = 0;
							Log.Write("debug", $"GeoMapGenerator error: {ex}");
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
