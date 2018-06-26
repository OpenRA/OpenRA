using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapToolsLogic : ChromeLogic
	{
		Widget panel;
		DropDownButtonWidget[] resizeDropdowns = new DropDownButtonWidget[10];

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}

		// Constants.
		static Dictionary<string, Tuple<int, int>> resizeFactors = new Dictionary<string, Tuple<int, int>>
			{ // Default Resize Factors.
				{ "0.25", Tuple.Create(1, 4) },
				{ "0.50", Tuple.Create(1, 2) },
				{ "0.75", Tuple.Create(3, 4) },
				{ "0.90", Tuple.Create(9, 10) },
				{ "1.00", Tuple.Create(1, 1) },
				{ "1.10", Tuple.Create(11, 10) },
				{ "1.25", Tuple.Create(5, 4) },
				{ "1.33", Tuple.Create(4, 3) },
				{ "1.50", Tuple.Create(3, 2) },
				{ "2.00", Tuple.Create(2, 1) }
			};
		static Dictionary<string, Tuple<int, int, int>> mapElements = new Dictionary<string, Tuple<int, int, int>>
			{ // Default Resize Factors.
				{ "T, R, A", Tuple.Create(1, 1, 1) },
				{ "T, R, _", Tuple.Create(1, 1, 0) },
				{ "T, _, A", Tuple.Create(1, 0, 1) },
				{ "_, R, A", Tuple.Create(0, 1, 1) },
				{ "T, _, _", Tuple.Create(1, 0, 0) },
				{ "_, R, _", Tuple.Create(0, 1, 0) },
				{ "_, _, A", Tuple.Create(0, 0, 1) }
			};

		// Shared variables.
		Map inputBaseMap = null;
		Map inputSecondaryMap = null;
		Map outputMap = null;

		TextFieldWidget inputBaseMapFilepathTextField = null;
		TextFieldWidget inputSecondaryMapFilepathTextField = null;

		string outputMapFilepathText = "";

		string inputBaseMapTilesetText = "";
		string inputSecondaryMapTilesetText = "";

		int minMapSizeX = 0;
		int minMapSizeY = 0;

		int usingTiles = 0; int usingResources = 0; int usingActors = 0;

		Tuple<int, int> activeResizeW = Tuple.Create(1, 1);
		Tuple<int, int> activeResizeH = Tuple.Create(1, 1);
		Tuple<int, int, int> activeMapElements = Tuple.Create(1, 1, 1);

		// Common functions.
		bool GetInputBaseMap(ModData modData, Action showPanel)
		{
			// Function : gets variables, and Base map.
			usingTiles = activeMapElements.Item1; usingResources = activeMapElements.Item2; usingActors = activeMapElements.Item3;

			inputBaseMapFilepathTextField = panel.Get<TextFieldWidget>("FILE_PATH_A");

			// checks: file exists?
			if (!File.Exists(inputBaseMapFilepathTextField.Text))
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Base map not found",
					text: "Base map does not exist or could not be accessed.",
					onConfirm: showPanel);
				return false;
			}

			// checks: file of .oramap type?
			try
			{
				var inputBaseMapPackage = new Folder(".").OpenPackage(inputBaseMapFilepathTextField.Text, modData.ModFiles) as IReadWritePackage;
				inputBaseMap = new Map(modData, inputBaseMapPackage);
			}
			catch (ArgumentException)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Incompatible file format",
					text: "Base file must be of .oramap type!",
					onConfirm: showPanel);
				return false;
			}

			// checks: mod ?
			if (modData.Manifest.Id != inputBaseMap.RequiresMod)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Incompatible mod",
					text: "Base map incompatible with mod.",
					onConfirm: showPanel);
				return false;
			}

			return true;
		}

		bool GetInputSecondaryMap(ModData modData, Action showPanel)
		{
			// Function : gets variables, and Secondary map.
			inputSecondaryMapFilepathTextField = panel.Get<TextFieldWidget>("FILE_PATH_B");

			// checks: file exists?
			if (!File.Exists(inputSecondaryMapFilepathTextField.Text))
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Secondary map not found",
					text: "Sec. map does not exist or could not be accessed.",
					onConfirm: showPanel);
				return false;
			}

			// checks: file of .oramap type?
			try
			{
				var inputSecondaryMapPackage = new Folder(".").OpenPackage(inputSecondaryMapFilepathTextField.Text, modData.ModFiles) as IReadWritePackage;
				inputSecondaryMap = new Map(modData, inputSecondaryMapPackage);
			}
			catch (ArgumentException)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Incompatible file format",
					text: "Secondary file must be of .oramap type!",
					onConfirm: showPanel);
				return false;
			}

			// checks: mod ?
			if (modData.Manifest.Id != inputSecondaryMap.RequiresMod)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Incompatible mod",
					text: "Secondary map incompatible with mod.",
					onConfirm: showPanel);
				return false;
			}

			return true;
		}

		bool GetInputMaps(ModData modData, Action showPanel)
		{
			// Function : gets variables, and maps A and B.
			if (!GetInputBaseMap(modData, showPanel)) { return false; }
			if (!GetInputSecondaryMap(modData, showPanel)) { return false; }

			// checks: same tileset?
			inputBaseMapTilesetText = inputBaseMap.Tileset;
			inputSecondaryMapTilesetText = inputSecondaryMap.Tileset;

			if (inputBaseMapTilesetText != inputSecondaryMapTilesetText)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Different tilesets",
					text: "The maps have different tilesets.",
					onConfirm: showPanel);
				return false;
			}

			minMapSizeX = Math.Max(2, Math.Min(inputBaseMap.MapSize.X, inputSecondaryMap.MapSize.X));
			minMapSizeY = Math.Max(2, Math.Min(inputBaseMap.MapSize.Y, inputSecondaryMap.MapSize.Y));
			return true;
		}

		void SaveNewMap(Action<string> onSelect, Action onExit)
		{
			// saving the map
			Action<string> afterSave = uid =>
			{
				// HACK: Work around a synced-code change check.
				// It's not clear why this is needed here, but not in the other places that load maps.
				Game.RunAfterTick(() =>
				{
					ConnectionLogic.Connect(System.Net.IPAddress.Loopback.ToString(),
						Game.CreateLocalServer(uid), "",
						() => Game.LoadEditor(uid),
						() => { Game.CloseServer(); onExit(); });
				});

				Ui.CloseWindow();
				onSelect(uid);
			};

			Ui.OpenWindow("SAVE_MAP_PANEL", new WidgetArgs()
				{
					{ "onSave", afterSave },
					{ "onExit", () => { Ui.CloseWindow(); onExit(); } },
					{ "map", outputMap },
					{ "playerDefinitions", outputMap.PlayerDefinitions },
					{ "actorDefinitions", outputMap.ActorDefinitions }
				});
		}

		// Object creator.
		[ObjectCreator.UseCtor]
		public MapToolsLogic(Widget widget, World world, ModData modData, Action<string> onSelect, Action onExit)
		{
			panel = widget;
			Action showPanel = () => panel.Visible = true;

			var resizeDropdownW = panel.Get<DropDownButtonWidget>("RESIZE_W");
			var resizeDropdownH = panel.Get<DropDownButtonWidget>("RESIZE_H");
			var mapElementsDropdown = panel.Get<DropDownButtonWidget>("USING_ELEMENTS");

			resizeDropdownW.Text = "0.25";
			resizeDropdownH.Text = "0.25";
			mapElementsDropdown.Text = "T, R, A";

			activeResizeW = resizeFactors[resizeDropdownW.Text];
			activeResizeH = resizeFactors[resizeDropdownH.Text];
			activeMapElements = mapElements[mapElementsDropdown.Text];

			resizeDropdownW.OnMouseDown = _ =>
			{
				var resizeFactorsB = resizeFactors.Select(kv => new DropDownOption
				{
					Title = kv.Key,
					IsSelected = () => resizeDropdownW.Text == kv.Key,
					OnClick = () =>
					{
						resizeDropdownW.Text = kv.Key;
						activeResizeW = resizeFactors[kv.Key];
					}
				});

				Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
					return item;
				};

				resizeDropdownW.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", resizeFactorsB.Count() * 30, resizeFactorsB, setupItem);
			};

			resizeDropdownH.OnMouseDown = _ =>
			{
				var resizeFactorsB = resizeFactors.Select(kv => new DropDownOption
				{
					Title = kv.Key,
					IsSelected = () => resizeDropdownH.Text == kv.Key,
					OnClick = () =>
					{
						resizeDropdownH.Text = kv.Key;
						activeResizeH = resizeFactors[kv.Key];
					}
				});

				Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
					return item;
				};

				resizeDropdownH.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", resizeFactorsB.Count() * 30, resizeFactorsB, setupItem);
			};

			mapElementsDropdown.OnMouseDown = _ =>
			{
				var mapElementsB = mapElements.Select(kv => new DropDownOption
				{
					Title = kv.Key,
					IsSelected = () => mapElementsDropdown.Text == kv.Key,
					OnClick = () =>
					{
						mapElementsDropdown.Text = kv.Key;
						activeMapElements = mapElements[kv.Key];
					}
				});

				Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
					return item;
				};

				mapElementsDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", mapElementsB.Count() * 30, mapElementsB, setupItem);
			};

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			//// APPEND TOOL.
			panel.Get<ButtonWidget>("APPEND_BUTTON").OnClick = () =>
			{
				// Initialization. If fails, break.
				if (!GetInputMaps(modData, showPanel)) { return; };

				// Copy Base map to create Output map
				outputMapFilepathText = inputBaseMapFilepathTextField.Text.Replace(".oramap", "") + "-Append";
				outputMapFilepathText += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(inputBaseMapFilepathTextField.Text, outputMapFilepathText, true);
				var outputPackage = new Folder(".").OpenPackage(outputMapFilepathText, modData.ModFiles) as IReadWritePackage;
				outputMap = new Map(modData, outputPackage);

				outputMap.Title += "-Append";
				outputMap.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				outputMap.Resize(inputBaseMap.MapSize.X + inputSecondaryMap.MapSize.X, Math.Max(inputBaseMap.MapSize.Y, inputSecondaryMap.MapSize.Y));

				var outputLeftTopCell = new PPos(inputBaseMap.Bounds.Left, Math.Min(inputBaseMap.Bounds.Top, inputSecondaryMap.Bounds.Top));
				var outputRightBottomCell = new PPos(inputBaseMap.MapSize.X + inputSecondaryMap.Bounds.Right - 1,
					Math.Max(inputBaseMap.Bounds.Bottom, inputSecondaryMap.Bounds.Bottom) - 1);

				outputMap.SetBounds(outputLeftTopCell, outputRightBottomCell);

				// Appending Secondary map to Base map.
				for (int j = 0; j < inputSecondaryMap.MapSize.Y; j++)
				{
					for (int i = 0; i < inputSecondaryMap.MapSize.X; i++)
					{
						var posB = new MPos(i, j);
						var outPos = new MPos(i + inputBaseMap.MapSize.X, j);

						if (usingTiles == 1) { outputMap.Tiles[outPos] = inputSecondaryMap.Tiles[posB]; outputMap.Height[outPos] = inputSecondaryMap.Height[posB]; };
						if (usingResources == 1) { outputMap.Resources[outPos] = inputSecondaryMap.Resources[posB]; };
					}
				}

				if (usingActors == 1)
				{
					foreach (var kv in inputSecondaryMap.ActorDefinitions)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);

						var oldMPos = new CPos(location.X, location.Y).ToMPos(outputMap);
						var newCPos = new MPos(oldMPos.U + inputBaseMap.MapSize.X, oldMPos.V).ToCPos(outputMap);
						var newLocation = new LocationInit(newCPos);

						var actType = actor.Type;
						var newActor = new ActorReference(actType) { newLocation, new OwnerInit("Neutral") };
						outputMap.ActorDefinitions.Add(new MiniYamlNode("Actor" + outputMap.ActorDefinitions.Count, newActor.Save()));
					}
				}

				SaveNewMap(onSelect, onExit);
			};

			//// COPY TOOL.
			panel.Get<ButtonWidget>("COPY_BUTTON").OnClick = () =>
			{
				// Initialization. If fails, break.
				if (!GetInputMaps(modData, showPanel)) { return; };

				// Copy Base map to create Output map.
				outputMapFilepathText = inputBaseMapFilepathTextField.Text.Replace(".oramap", "") + "-Copy";
				outputMapFilepathText += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(inputBaseMapFilepathTextField.Text, outputMapFilepathText, true);
				var outputPackage = new Folder(".").OpenPackage(outputMapFilepathText, modData.ModFiles) as IReadWritePackage;
				outputMap = new Map(modData, outputPackage);

				outputMap.Title += "-Copy";
				outputMap.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				// Copying Secondary map over Base map.
				for (int j = 0; j < minMapSizeY; j++)
				{
					for (int i = 0; i < minMapSizeX; i++)
					{
						var pos = new MPos(i, j);

						if (usingTiles == 1) { outputMap.Tiles[pos] = inputSecondaryMap.Tiles[pos]; outputMap.Height[pos] = inputSecondaryMap.Height[pos]; };
						if (usingResources == 1) { outputMap.Resources[pos] = inputSecondaryMap.Resources[pos]; };
					}
				}

				var forRemoval = new List<MiniYamlNode>();

				foreach (var kv in outputMap.ActorDefinitions)
					forRemoval.Add(kv);

				foreach (var kv in forRemoval)
					outputMap.ActorDefinitions.Remove(kv);

				if (usingActors == 1)
				{
					foreach (var kv in inputSecondaryMap.ActorDefinitions)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);
						var loc_MPos = new CPos(location.X, location.Y).ToMPos(outputMap);

						bool outputMap_Bounds = (loc_MPos.U < outputMap.MapSize.X) && (loc_MPos.V < outputMap.MapSize.Y);

						if (outputMap_Bounds)
							outputMap.ActorDefinitions.Add(new MiniYamlNode("Actor" + outputMap.ActorDefinitions.Count, actor.Save()));
					}
				};

				foreach (var kv in inputBaseMap.ActorDefinitions)
				{
					var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
					var location = actor.InitDict.Get<LocationInit>().Value(null);
					var loc_MPos = new CPos(location.X, location.Y).ToMPos(outputMap);

					bool outputMap_Bounds = (loc_MPos.U < outputMap.MapSize.X) && (loc_MPos.V < outputMap.MapSize.Y);
					bool inputSecondaryMap_Bounds = (loc_MPos.U < inputSecondaryMap.MapSize.X) && (loc_MPos.V < inputSecondaryMap.MapSize.Y);
					bool inputSecondaryMap_reserved = (usingActors == 1) && inputSecondaryMap_Bounds;

					if (outputMap_Bounds && !inputSecondaryMap_reserved)
						outputMap.ActorDefinitions.Add(new MiniYamlNode("Actor" + outputMap.ActorDefinitions.Count, actor.Save()));
				}

				SaveNewMap(onSelect, onExit);
			};

			//// MIRROR_LEFT TOOL.
			panel.Get<ButtonWidget>("MIRROR_LEFT").OnClick = () =>
			{
				// Initialization. If fails, break.
				if (!GetInputBaseMap(modData, showPanel)) { return; };

				// Copy Base map to create Output map.
				outputMapFilepathText = inputBaseMapFilepathTextField.Text.Replace(".oramap", "") + "-MirrorLeft";
				outputMapFilepathText += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(inputBaseMapFilepathTextField.Text, outputMapFilepathText, true);
				var outputPackage = new Folder(".").OpenPackage(outputMapFilepathText, modData.ModFiles) as IReadWritePackage;
				outputMap = new Map(modData, outputPackage);

				outputMap.Title += "-MirrorLeft";
				outputMap.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				// Calculations.
				int halfWidth = inputBaseMap.Bounds.Width / 2;
				int halfWidthRest = inputBaseMap.Bounds.Width % 2;

				halfWidth = halfWidth + halfWidthRest;

				for (int j = 0; j < inputBaseMap.MapSize.Y; j++)
				{
					for (int i = 0; i < halfWidth; i++)
					{
						var i_old = i + inputBaseMap.Bounds.Left;
						var j_old = j;
						var inPos = new MPos(i_old, j_old);

						// Adding offset if Isometric map. See MPos diagram.
						var i_iso = (inputBaseMap.Grid.Type == MapGridType.RectangularIsometric) ? j % 2 : 0;
						var i_new = inputBaseMap.Bounds.Right - (i + i_iso) - 1;
						var j_new = j_old;

						var outPos = new MPos(i_new, j_new);

						if (usingTiles == 1) { outputMap.Tiles[outPos] = inputBaseMap.Tiles[inPos]; outputMap.Height[outPos] = inputBaseMap.Height[inPos]; };
						if (usingResources == 1) { outputMap.Resources[outPos] = inputBaseMap.Resources[inPos]; };
					}
				}

				if (usingActors == 1)
				{
					var forRemoval = new List<MiniYamlNode>();
					var forKeeping = new List<MiniYamlNode>();

					foreach (var kv in outputMap.ActorDefinitions)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);
						var location_MPos = new CPos(location.X, location.Y).ToMPos(outputMap);

						if ((inputBaseMap.Bounds.Left < location_MPos.U) && (location_MPos.U <= inputBaseMap.Bounds.Left + halfWidth - halfWidthRest))
							forKeeping.Add(kv);
						else
							forRemoval.Add(kv);
					}

					foreach (var kv in forRemoval)
						outputMap.ActorDefinitions.Remove(kv);

					foreach (var kv in forKeeping)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);

						var oldMPos = location.ToMPos(outputMap);

						var i = oldMPos.U - inputBaseMap.Bounds.Left;
						var j = oldMPos.V;

						// Adding offset if Isometric map. See MPos diagram.
						var i_iso = (inputBaseMap.Grid.Type == MapGridType.RectangularIsometric) ? j % 2 : 0;
						i += i_iso;

						var newCPos = new MPos(inputBaseMap.Bounds.Right - 1 - i, j).ToCPos(outputMap);
						var newLocation = new LocationInit(newCPos);

						var actType = actor.Type;
						var newActor = new ActorReference(actType) { newLocation, new OwnerInit("Neutral") };
						outputMap.ActorDefinitions.Add(new MiniYamlNode("Actor" + outputMap.ActorDefinitions.Count, newActor.Save()));
					}
				}

				SaveNewMap(onSelect, onExit);
			};

			//// MIRROR_TOP TOOL.
			panel.Get<ButtonWidget>("MIRROR_TOP").OnClick = () =>
			{
				// Initialization. If fails, break.
				if (!GetInputBaseMap(modData, showPanel)) { return; };

				// Copy Base map to create Output map.
				outputMapFilepathText = inputBaseMapFilepathTextField.Text.Replace(".oramap", "") + "-MirrorTop";
				outputMapFilepathText += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(inputBaseMapFilepathTextField.Text, outputMapFilepathText, true);
				var outputPackage = new Folder(".").OpenPackage(outputMapFilepathText, modData.ModFiles) as IReadWritePackage;
				outputMap = new Map(modData, outputPackage);

				outputMap.Title += "-MirrorTop";
				outputMap.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				// Calculations.
				int halfHeight = inputBaseMap.Bounds.Height / 2;
				int halfHeightRest = inputBaseMap.Bounds.Height % 2;
				halfHeight = halfHeight + halfHeightRest;

				for (int j = 0; j < halfHeight; j++)
				{
					for (int i = 0; i < inputBaseMap.MapSize.X; i++)
					{
						var i_old = i;
						var j_old = j + inputBaseMap.Bounds.Top;
						var inPos = new MPos(i_old, j_old);

						var i_new = i_old;
						var j_new = inputBaseMap.Bounds.Bottom - j - 1;

						// If height is odd, MPos issue. (-1) offset to correct it.
						if ((inputBaseMap.Grid.Type == MapGridType.RectangularIsometric) && ((inputBaseMap.Bounds.Height % 2) == 0))
							j_new -= 1;

						var outPos = new MPos(i_new, j_new);

						if (usingTiles == 1)
						{
							outputMap.Tiles[outPos] = inputBaseMap.Tiles[inPos];
							outputMap.Height[outPos] = inputBaseMap.Height[inPos];
						}

						if (usingResources == 1)
							outputMap.Resources[outPos] = inputBaseMap.Resources[inPos];
					}
				}

				if (usingActors == 1)
				{
					var forRemoval = new List<MiniYamlNode>();
					var forKeeping = new List<MiniYamlNode>();

					foreach (var kv in outputMap.ActorDefinitions)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);
						var location_MPos = new CPos(location.X, location.Y).ToMPos(outputMap);

						if ((inputBaseMap.Bounds.Top < location_MPos.V) && (location_MPos.V <= inputBaseMap.Bounds.Top + halfHeight - halfHeightRest))
							forKeeping.Add(kv);
						else
							forRemoval.Add(kv);
					}

					foreach (var kv in forRemoval)
						outputMap.ActorDefinitions.Remove(kv);

					foreach (var kv in forKeeping)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);

						var oldMPos = location.ToMPos(outputMap);

						var i = oldMPos.U;
						var j_local = oldMPos.V - inputBaseMap.Bounds.Top;

						// If height is odd, MPos issue. (-1) offset to correct it.
						var j_new = inputBaseMap.Bounds.Bottom - 1 - j_local;
						if ((inputBaseMap.Grid.Type == MapGridType.RectangularIsometric) && ((inputBaseMap.Bounds.Height % 2) == 0))
							j_new -= 1;

						var newCPos = new MPos(i, j_new).ToCPos(outputMap);
						var newLocation = new LocationInit(newCPos);

						var actType = actor.Type;
						var newActor = new ActorReference(actType) { newLocation, new OwnerInit("Neutral") };
						outputMap.ActorDefinitions.Add(new MiniYamlNode("Actor" + outputMap.ActorDefinitions.Count, newActor.Save()));
					}
				};
				SaveNewMap(onSelect, onExit);
			};

			// RESIZE TOOL.
			panel.Get<ButtonWidget>("RESIZE_BUTTON").OnClick = () =>
			{
				// Initialization. If fails, break.
				if (!GetInputBaseMap(modData, showPanel)) { return; };

				// Copy Base map to create Output map.
				outputMapFilepathText = inputBaseMapFilepathTextField.Text.Replace(".oramap", "") + "-Resize";
				outputMapFilepathText += "_" + resizeDropdownW.Text.Replace(".", "");
				outputMapFilepathText += "_" + resizeDropdownH.Text.Replace(".", "");
				outputMapFilepathText += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(inputBaseMapFilepathTextField.Text, outputMapFilepathText, true);
				var outputPackage = new Folder(".").OpenPackage(outputMapFilepathText, modData.ModFiles) as IReadWritePackage;
				outputMap = new Map(modData, outputPackage);
				outputMap.Title += "-Resize";
				outputMap.Title += "_" + resizeDropdownW.Text.Replace(".", "");
				outputMap.Title += "_" + resizeDropdownH.Text.Replace(".", "");
				outputMap.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				// Calculations.
				int Aw = activeResizeW.Item1; int Bw = activeResizeW.Item2;
				int Ah = activeResizeH.Item1; int Bh = activeResizeH.Item2;

				int outputMapWidth = Math.Max(1, (inputBaseMap.Bounds.Width * Aw) / Bw);
				int outputMapHeight = Math.Max(1, (inputBaseMap.Bounds.Height * Ah) / Bh);

				var outputMapSizeX = inputBaseMap.MapSize.X + outputMapWidth - inputBaseMap.Bounds.Width;
				var outputMapSizeY = inputBaseMap.MapSize.Y + outputMapHeight - inputBaseMap.Bounds.Height;

				var outputLeftTopCell = new PPos(inputBaseMap.Bounds.Left, inputBaseMap.Bounds.Top);
				var outputRightBottomCell = new PPos(inputBaseMap.Bounds.Left + outputMapWidth - 1, inputBaseMap.Bounds.Top + outputMapHeight - 1);

				// Fixing potential southern bound problem : resizeOffset.
				int[] A_Tiles_lastRow_height = new int[inputBaseMap.MapSize.X];
				for (int i = 0; i < inputBaseMap.MapSize.X; i++)
				{
					var inPos = new MPos(i, inputBaseMap.MapSize.Y - 1);
					A_Tiles_lastRow_height[i] = inputBaseMap.Height[inPos];
				}

				var A_Tiles_lastRow_MaxHeight = A_Tiles_lastRow_height.Max();
				var BottomBound = inputBaseMap.MapSize.Y - inputBaseMap.Bounds.Bottom;
				var resizeOffset = 0;

				if (BottomBound == A_Tiles_lastRow_MaxHeight)
					resizeOffset = 1;

				// Resize and set bounds.
				outputMap.Resize(outputMapSizeX, outputMapSizeY + resizeOffset);
				outputMap.SetBounds(outputLeftTopCell, outputRightBottomCell);

				// Grid B resizing bigger.
				for (int local_j_new = 1; local_j_new <= outputMapHeight; local_j_new++)
				{
					for (int local_i_new = 1; local_i_new <= outputMapWidth; local_i_new++)
					{
						var local_i_old = Math.Max(1, Math.Min(inputBaseMap.Bounds.Width, (local_i_new * Bw) / Aw));
						var local_j_old = Math.Max(1, Math.Min(inputBaseMap.Bounds.Height, (local_j_new * Bh) / Ah));

						var i_new = (local_i_new - 1) + inputBaseMap.Bounds.Left; var j_new = (local_j_new - 1) + inputBaseMap.Bounds.Top;
						var i_old = (local_i_old - 1) + inputBaseMap.Bounds.Left; var j_old = (local_j_old - 1) + inputBaseMap.Bounds.Top;

						var outPos = new MPos(i_new, j_new);
						var inPos = new MPos(i_old, j_old);

						if (usingTiles == 1) { outputMap.Tiles[outPos] = inputBaseMap.Tiles[inPos]; outputMap.Height[outPos] = inputBaseMap.Height[inPos]; };
						if (usingResources == 1) { outputMap.Resources[outPos] = inputBaseMap.Resources[inPos]; };
					}
				}

				// Cleaning bottom border, if resize Y < 1.
				if (Ah < Bh)
				{
					for (int j_old = inputBaseMap.Bounds.Bottom; j_old < inputBaseMap.MapSize.Y; j_old++)
					{
						for (int i_old = 0; i_old < Math.Min(inputBaseMap.MapSize.X, outputMapSizeX); i_old++)
						{
							var i_new = i_old;
							var j_new = j_old + outputMapHeight - inputBaseMap.Bounds.Height;

							var outPos = new MPos(i_new, j_new);
							var inPos = new MPos(i_old, j_old);

							outputMap.Tiles[outPos] = inputBaseMap.Tiles[inPos]; outputMap.Height[outPos] = inputBaseMap.Height[inPos];
							outputMap.Resources[outPos] = inputBaseMap.Resources[inPos];
						}
					}
				}

				// Cleaning right border, if resize X < 1.
				if (Aw < Bw)
				{
					for (int j_old = 0; j_old < Math.Min(inputBaseMap.MapSize.Y, outputMapSizeY); j_old++)
					{
						for (int i_old = inputBaseMap.Bounds.Right; i_old < inputBaseMap.MapSize.X; i_old++)
						{
							var i_new = i_old + outputMapWidth - inputBaseMap.Bounds.Width;
							var j_new = j_old;

							var outPos = new MPos(i_new, j_new);
							var inPos = new MPos(i_old, j_old);

							outputMap.Tiles[outPos] = inputBaseMap.Tiles[inPos]; outputMap.Height[outPos] = inputBaseMap.Height[inPos];
							outputMap.Resources[outPos] = inputBaseMap.Resources[inPos];
						}
					}
				}

				var forRemoval = new List<MiniYamlNode>();

				foreach (var kv in outputMap.ActorDefinitions)
					forRemoval.Add(kv);

				foreach (var kv in forRemoval)
					outputMap.ActorDefinitions.Remove(kv);

				foreach (var kv in inputBaseMap.ActorDefinitions)
				{
					var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
					var location = actor.InitDict.Get<LocationInit>().Value(null);
					var actType = actor.Type;

					var oldMPos = new CPos(location.X, location.Y).ToMPos(outputMap);
					var oldX = oldMPos.U; var oldY = oldMPos.V;
					var local_oldX = oldX - inputBaseMap.Bounds.Left; var local_oldY = oldY - inputBaseMap.Bounds.Top;

					var inBounds = ((inputBaseMap.Bounds.Left <= oldX) && (oldX < inputBaseMap.Bounds.Right)) &&
						((inputBaseMap.Bounds.Top <= oldY) && (oldY < inputBaseMap.Bounds.Bottom));

					if (inBounds)
					{
						if (usingActors == 1)
						{
							var local_newX = Math.Min((local_oldX * Aw) / Bw, outputMapWidth - 1);
							var local_newY = Math.Min((local_oldY * Ah) / Bh, outputMapHeight - 1);

							var newX = local_newX + inputBaseMap.Bounds.Left;
							var newY = local_newY + inputBaseMap.Bounds.Top;
							var newCPos = new MPos(newX, newY).ToCPos(outputMap);
							var newLocation = new LocationInit(newCPos);

							var newActor = new ActorReference(actType) { newLocation, new OwnerInit("Neutral") };
							outputMap.ActorDefinitions.Add(new MiniYamlNode("Actor" + outputMap.ActorDefinitions.Count, newActor.Save()));
						}

						if (usingActors == 0)
							outputMap.ActorDefinitions.Add(new MiniYamlNode("Actor" + outputMap.ActorDefinitions.Count, actor.Save()));
					}
				}

				SaveNewMap(onSelect, onExit);
			};
		}
	}
}
