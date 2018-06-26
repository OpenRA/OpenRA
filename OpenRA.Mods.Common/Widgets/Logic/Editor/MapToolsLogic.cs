using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Widgets;
using OpenRA.FileSystem;


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
		// Constants
		static Dictionary<string, Tuple<int, int>> resizeFactors = new Dictionary<string, Tuple<int, int>>
			{ // Default Resize Factors
				{ "0.25" , Tuple.Create( 1  , 4  ) },
				{ "0.50" , Tuple.Create( 1  , 2  ) },
				{ "0.75" , Tuple.Create( 3  , 4  ) },
				{ "0.90" , Tuple.Create( 9  , 10 ) },
				{ "1.00" , Tuple.Create( 1  , 1  ) },
				{ "1.10" , Tuple.Create( 11 , 10 ) },
				{ "1.25" , Tuple.Create( 5  , 4  ) },
				{ "1.33" , Tuple.Create( 4  , 3  ) },
				{ "1.50" , Tuple.Create( 3  , 2  ) },
				{ "2.00" , Tuple.Create( 2  , 1  ) }
			};
		static Dictionary<string, Tuple<int, int, int>> mapElements = new Dictionary<string, Tuple<int, int, int>>
			{ // Default Resize Factors
				{ "T, R, A" , Tuple.Create( 1, 1, 1 ) },
				{ "T, R, _" , Tuple.Create( 1, 1, 0 ) },
				{ "T, _, A" , Tuple.Create( 1, 0, 1 ) },
				{ "_, R, A" , Tuple.Create( 0, 1, 1 ) },
				{ "T, _, _" , Tuple.Create( 1, 0, 0 ) },
				{ "_, R, _" , Tuple.Create( 0, 1, 0 ) },
				{ "_, _, A" , Tuple.Create( 0, 0, 1 ) }
			};

		// Variables
		Map Map_A = null;
		Map Map_B = null;
		Map Map_C = null;

		TextFieldWidget filepathTextField_A = null;
		TextFieldWidget filepathTextField_B = null;

		string filepathText_A = "";
		string filepathText_B = "";
		string filepathText_C = "";

		string tileset_A_txt = "";
		string tileset_B_txt = "";

		int A_left    =	0; int B_left    =	0;
		int A_right   =	0; int B_right   =	0;
		int A_top     =	0; int B_top     =	0;
		int A_bottom  =	0; int B_bottom  =	0;

		int A_X       =	0; int B_X       =	0;
		int A_Y       =	0; int B_Y       =	0;

		int A_W       =	0; int B_W       =	0;
		int A_H       =	0; int B_H       =	0;

		int Wmin = 0;
		int Hmin = 0;
		int Wmax = 0;
		int Hmax = 0;

		int Xmin = 0;
		int Ymin = 0;
		int Xmax = 0;
		int Ymax = 0;

		int usingTiles = 0; int usingResources = 0; int usingActors = 0;

		Tuple<int, int> activeResizeW = Tuple.Create(1, 1);
		Tuple<int, int> activeResizeH = Tuple.Create(1, 1);
		Tuple<int, int, int> activeMapElements = Tuple.Create(1, 1, 1);

		// Common functions
		bool getVariablesAndMaps_A(ModData modData, Action showPanel)
		{		
			// Function : gets variables, and map A.
			usingTiles = activeMapElements.Item1; usingResources = activeMapElements.Item2; usingActors = activeMapElements.Item3;

			filepathTextField_A = panel.Get<TextFieldWidget>("FILE_PATH_A");
			filepathText_A = filepathTextField_A.Text;

			
			// checks: file exists? 
			if (!File.Exists(filepathTextField_A.Text))
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Map A not found",
					text: "Map A does not exist or could not be accessed.",
					onConfirm: showPanel);
				return false;
			}

			// checks: file of .oramap type?
			try
			{
				var package_A = new Folder(".").OpenPackage(filepathTextField_A.Text, modData.ModFiles) as IReadWritePackage; 
				Map_A = new Map(modData, package_A);
			}
			catch (ArgumentException)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Incompatible file format",
					text: "File A must be of .oramap type!",
					onConfirm: showPanel);
				return false;
			}

			// checks: mod ?
			if (modData.Manifest.Id != Map_A.RequiresMod)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Incompatible mod",
					text: "Map A incompatible with mod.",
					onConfirm: showPanel);
				return false;
			}

			// variables
			A_left   = Map_A.Bounds.Left; 
			A_right  = Map_A.Bounds.Right;
			A_X      = Map_A.MapSize.X;
			A_W      = Map_A.Bounds.Width;
			A_top    = Map_A.Bounds.Top; 
			A_bottom = Map_A.Bounds.Bottom;
			A_Y      = Map_A.MapSize.Y; 
			A_H      = Map_A.Bounds.Height;

			return true;
		}

		bool getVariablesAndMaps_B(ModData modData, Action showPanel)
		{
			// Function : gets variables, and map B.
			filepathTextField_B = panel.Get<TextFieldWidget>("FILE_PATH_B");
			filepathText_B = filepathTextField_B.Text;

			// checks: file exists? 
			if (!File.Exists(filepathTextField_B.Text))
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Map B not found",
					text: "Map B does not exist or could not be accessed.",
					onConfirm: showPanel);
				return false;
			}

			// checks: file of .oramap type?
			try
			{
				var package_B = new Folder(".").OpenPackage(filepathTextField_B.Text, modData.ModFiles) as IReadWritePackage;
				Map_B = new Map(modData, package_B);
			}
			catch (ArgumentException)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Incompatible file format",
					text: "File B must be of .oramap type!",
					onConfirm: showPanel);
				return false;
			}

			// checks: mod ?
			if (modData.Manifest.Id != Map_B.RequiresMod)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Incompatible mod",
					text: "Map B incompatible with mod.",
					onConfirm: showPanel);
				return false;
			}

			// variables
			B_left   = Map_B.Bounds.Left; 
			B_right  = Map_B.Bounds.Right; 
			B_top    = Map_B.Bounds.Top; 
			B_bottom = Map_B.Bounds.Bottom;
			
			B_X      = Map_B.MapSize.X; 
			B_Y      = Map_B.MapSize.Y; 
				     
			B_W      = Map_B.Bounds.Width;
			B_H      = Map_B.Bounds.Height;
			return true;
		}

		bool getVariablesAndMaps_AB(ModData modData, Action showPanel)
		{
			// Function : gets variables, and maps A and B.

			if (!getVariablesAndMaps_A(modData, showPanel)) { return false; };
			if (!getVariablesAndMaps_B(modData, showPanel)) { return false; };

			// checks: same tileset?
			tileset_A_txt = Map_A.Tileset;
			tileset_B_txt = Map_B.Tileset;

			if (tileset_A_txt != tileset_B_txt)
			{
				panel.Visible = false;
				ConfirmationDialogs.ButtonPrompt(
					title: "Different tilesets",
					text: "The maps have different tilesets.",
					onConfirm: showPanel);
				return false;
			}
			Wmin = Math.Max(2, Math.Min(A_W, B_W)); // unused
			Hmin = Math.Max(2, Math.Min(A_H, B_H)); // unused

			Wmax = Math.Max(A_W, B_W); // unused
			Hmax = Math.Max(A_H, B_H); // unused

			Xmin = Math.Max(2, Math.Min(A_X, B_X));
			Ymin = Math.Max(2, Math.Min(A_Y, B_Y));

			Xmax = Math.Max(A_X, B_X);
			Ymax = Math.Max(A_Y, B_Y);
			return true;
		}

		void saveNewMap(Action<string> onSelect, Action onExit)
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
					{ "map", Map_C },
					{ "playerDefinitions", Map_C.PlayerDefinitions },
					{ "actorDefinitions", Map_C.ActorDefinitions }
				});
		}
		
		// Object creator
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

			//// Map Tools
			//// APPEND TOOL
			panel.Get<ButtonWidget>("APPEND_BUTTON").OnClick = () =>
			{
				// initialization. if fails, break.
				if (!getVariablesAndMaps_AB(modData, showPanel)) { return; };

				// copy map A to create map C
				filepathText_C = filepathTextField_A.Text.Replace(".oramap", "") + "-Append";
				filepathText_C += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(filepathText_A, filepathText_C, true);
				var package_C = new Folder(".").OpenPackage(filepathText_C, modData.ModFiles) as IReadWritePackage;
				Map_C = new Map(modData, package_C);

				Map_C.Title += "-Append";
				Map_C.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				Map_C.Resize((A_X + B_X), Math.Max(A_Y, B_Y));

				var LT_new = new PPos(A_left, Math.Min(A_top, B_top));
				var RB_new = new PPos(A_X + B_right - 1, Math.Max(A_bottom, B_bottom) - 1);
				Map_C.SetBounds(LT_new, RB_new);


				// APPEND TOOL
				// appending B map to A : 
				for (int j = 0; j < B_Y; j++)
				{
					for (int i = 0; i < B_X; i++)
					{
						var posB = new MPos(i, j);
						var posC = new MPos(i + A_X, j);

						if (usingTiles == 1) { Map_C.Tiles[posC] = Map_B.Tiles[posB]; Map_C.Height[posC] = Map_B.Height[posB]; };
						if (usingResources == 1) { Map_C.Resources[posC] = Map_B.Resources[posB]; };

					}
				}

				if (usingActors == 1)
				{
					foreach (var kv in Map_B.ActorDefinitions)
					{

						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);

						var loc_MPos_old = new CPos(location.X, location.Y).ToMPos(Map_C);
						var loc_CPos_new = new MPos(loc_MPos_old.U + A_X, loc_MPos_old.V).ToCPos(Map_C);
						var location_new = new LocationInit(loc_CPos_new);

						var actType = actor.Type;
						var actor_new = new ActorReference(actType) { location_new, new OwnerInit("Neutral") };
						Map_C.ActorDefinitions.Add(new MiniYamlNode("Actor" + Map_C.ActorDefinitions.Count, actor_new.Save()));

					}
				}

				saveNewMap(onSelect, onExit);
			};

			//// COPY TOOL
			panel.Get<ButtonWidget>("COPY_BUTTON").OnClick = () =>
			{
				// initialization. if fails, break.
				if (!getVariablesAndMaps_AB(modData, showPanel)) { return; };

				// copy map A to create map C

				filepathText_C = filepathTextField_A.Text.Replace(".oramap", "") + "-Copy";
				filepathText_C += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(filepathText_A, filepathText_C, true);
				var package_C = new Folder(".").OpenPackage(filepathText_C, modData.ModFiles) as IReadWritePackage;
				Map_C = new Map(modData, package_C);


				Map_C.Title += "-Copy";
				Map_C.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				// COPY TOOL
				// --------------------------------------------------------------
				// copying map B over A
				for (int j = 0; j < Ymin; j++)
				{
					for (int i = 0; i < Xmin; i++)
					{
						var pos = new MPos(i, j);

						if (usingTiles == 1) { Map_C.Tiles[pos] = Map_B.Tiles[pos]; Map_C.Height[pos] = Map_B.Height[pos]; };
						if (usingResources == 1) { Map_C.Resources[pos] = Map_B.Resources[pos]; };
					}
				}

				var forRemoval = new List<MiniYamlNode>();

				foreach (var kv in Map_C.ActorDefinitions)
				{
					forRemoval.Add(kv);
				}

				foreach (var kv in forRemoval)
				{
					Map_C.ActorDefinitions.Remove(kv);
				}

				if (usingActors == 1)
				{
					foreach (var kv in Map_B.ActorDefinitions)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);
						var loc_MPos = new CPos(location.X, location.Y).ToMPos(Map_C);

						bool inMapC_Bounds = (loc_MPos.U < Map_C.MapSize.X) && (loc_MPos.V < Map_C.MapSize.Y);

						if (inMapC_Bounds)
						{
							Map_C.ActorDefinitions.Add(new MiniYamlNode("Actor" + Map_C.ActorDefinitions.Count, actor.Save()));

						}
					}
				};

				foreach (var kv in Map_A.ActorDefinitions)
				{
					var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
					var location = actor.InitDict.Get<LocationInit>().Value(null);
					var loc_MPos = new CPos(location.X, location.Y).ToMPos(Map_C);

					bool inMapC_Bounds = (loc_MPos.U < Map_C.MapSize.X) && (loc_MPos.V < Map_C.MapSize.Y);
					bool inMapB_Bounds = (loc_MPos.U < B_X) && (loc_MPos.V < B_Y);
					bool inMapB_reserved = ((usingActors == 1) && inMapB_Bounds);

					if (inMapC_Bounds && !inMapB_reserved)
					{
						Map_C.ActorDefinitions.Add(new MiniYamlNode("Actor" + Map_C.ActorDefinitions.Count, actor.Save()));
					}
				}

				saveNewMap(onSelect, onExit);
			};

			//// MIRROR_LEFT TOOL
			panel.Get<ButtonWidget>("MIRROR_LEFT").OnClick = () =>
			{
				// initialization. if fails, break.
				if (!getVariablesAndMaps_A(modData, showPanel)) { return; };

				// copy map A to create map C
				filepathText_C = filepathTextField_A.Text.Replace(".oramap", "") + "-MirrorLeft";
				filepathText_C += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(filepathText_A, filepathText_C, true);
				var package_C = new Folder(".").OpenPackage(filepathText_C, modData.ModFiles) as IReadWritePackage;
				Map_C = new Map(modData, package_C);

				Map_C.Title += "-MirrorLeft";
				Map_C.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");


				// MIRROR_LEFT TOOL
				int A_W_half = A_W / 2;
				int A_W_rest = A_W % 2;

				A_W_half = A_W_half + A_W_rest;

				for (int j = 0; j < A_Y; j++)
				{
					for (int i = 0 ; i < A_W_half; i++)
					{
						var i_old = i + A_left;
						var j_old = j;
						var posA = new MPos(i_old, j_old);

						var i_iso = (Map_A.Grid.Type == MapGridType.RectangularIsometric) ? j % 2 : 0;  // adding offset if Isometric map. See MPos diagram.
						var i_new = A_right - (i + i_iso) - 1;
						var j_new = j_old;

						var posC = new MPos(i_new, j_new);

						if (usingTiles == 1) { Map_C.Tiles[posC] = Map_A.Tiles[posA]; Map_C.Height[posC] = Map_A.Height[posA]; };
						if (usingResources == 1) { Map_C.Resources[posC] = Map_A.Resources[posA]; };
					}
				}

				if (usingActors == 1)
				{
					var forRemoval = new List<MiniYamlNode>();
					var forKeeping = new List<MiniYamlNode>();

					foreach (var kv in Map_C.ActorDefinitions)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);
						var location_MPos = new CPos(location.X, location.Y).ToMPos(Map_C);

						if ((A_left < location_MPos.U) && (location_MPos.U <= A_left + A_W_half - A_W_rest))
						{
							forKeeping.Add(kv);
						}
						else
						{
							forRemoval.Add(kv);
						}
					}
					foreach (var kv in forRemoval)
					{
						Map_C.ActorDefinitions.Remove(kv);
					}

					foreach (var kv in forKeeping)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);

						var loc_MPos_old = location.ToMPos(Map_C);

						var i = loc_MPos_old.U - A_left;
						var j = loc_MPos_old.V;
						var i_iso = (Map_A.Grid.Type == MapGridType.RectangularIsometric) ? j % 2 : 0;
						i += i_iso; // adding offset if Isometric map. See MPos diagram.

						var loc_CPos_new = new MPos(A_right - 1 - i, j).ToCPos(Map_C);
						var location_new = new LocationInit(loc_CPos_new);

						var actType = actor.Type;
						var actor_new = new ActorReference(actType) { location_new, new OwnerInit("Neutral") };
						Map_C.ActorDefinitions.Add(new MiniYamlNode("Actor" + Map_C.ActorDefinitions.Count, actor_new.Save()));
					}
				}

				saveNewMap(onSelect, onExit);
			};

			//// MIRROR_TOP TOOL
			panel.Get<ButtonWidget>("MIRROR_TOP").OnClick = () =>
			{
				// initialization. if fails, break.
				if (!getVariablesAndMaps_A(modData, showPanel)) { return; };

				// copy map A to create map C
				filepathText_C = filepathTextField_A.Text.Replace(".oramap", "") + "-MirrorTop";
				filepathText_C += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";

				System.IO.File.Copy(filepathText_A, filepathText_C, true);
				var package_C = new Folder(".").OpenPackage(filepathText_C, modData.ModFiles) as IReadWritePackage;
				Map_C = new Map(modData, package_C);

				Map_C.Title += "-MirrorTop";
				Map_C.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				// MIRROR_TOP TOOL

				int A_H_half = A_H / 2;
				int A_H_rest = A_H % 2;
				A_H_half = A_H_half + A_H_rest;

				for (int j = 0; j < A_H_half; j++)
				{
					for (int i = 0; i < A_X; i++)
					{
						var i_old = i;
						var j_old = j + A_top;
						var posA = new MPos(i_old, j_old);

						var i_new = i_old;
						var j_new = A_bottom - (j) - 1;
						if ((Map_A.Grid.Type == MapGridType.RectangularIsometric) && ((A_H % 2) == 0)) // if height is odd, MPos issue. (-1) offset to correct it.
						{
							j_new -= 1;
						}
						
						var posC = new MPos(i_new, j_new);

						if (usingTiles == 1) { Map_C.Tiles[posC] = Map_A.Tiles[posA]; Map_C.Height[posC] = Map_A.Height[posA]; };
						if (usingResources == 1) { Map_C.Resources[posC] = Map_A.Resources[posA]; };
					}
				}

				if (usingActors == 1)
				{
					var forRemoval = new List<MiniYamlNode>();
					var forKeeping = new List<MiniYamlNode>();

					foreach (var kv in Map_C.ActorDefinitions)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);
						var location_MPos = new CPos(location.X, location.Y).ToMPos(Map_C);

						if ((A_top < location_MPos.V) && (location_MPos.V <= A_top + A_H_half - A_H_rest))
						{
							forKeeping.Add(kv);
						}
						else
						{
							forRemoval.Add(kv);
						}
						
					}

					foreach (var kv in forRemoval)
					{
						Map_C.ActorDefinitions.Remove(kv);
					}

					foreach (var kv in forKeeping)
					{
						var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
						var location = actor.InitDict.Get<LocationInit>().Value(null);

						var loc_MPos_old = location.ToMPos(Map_C);


						var i = loc_MPos_old.U;
						var j_local = loc_MPos_old.V - A_top;
						
						var j_new = A_bottom - 1 - j_local;
						if ((Map_A.Grid.Type == MapGridType.RectangularIsometric) && ((A_H % 2) == 0)) // if height is odd, MPos issue. (-1) offset to correct it.
						{
							j_new -= 1;
						}
						var loc_CPos_new = new MPos(i, j_new).ToCPos(Map_C);
						var location_new = new LocationInit(loc_CPos_new);

						var actType = actor.Type;
						var actor_new = new ActorReference(actType) { location_new, new OwnerInit("Neutral") };
						Map_C.ActorDefinitions.Add(new MiniYamlNode("Actor" + Map_C.ActorDefinitions.Count, actor_new.Save()));
					}
				};

				saveNewMap(onSelect, onExit);
			};

			// RESIZE TOOL
			panel.Get<ButtonWidget>("RESIZE_BUTTON").OnClick = () =>
			{

				// initialization. if fails, break.
				if (!getVariablesAndMaps_A(modData, showPanel)) { return; };

				// copy map A to create map C
				filepathText_C = filepathTextField_A.Text.Replace(".oramap", "") + "-Resize";
				filepathText_C += "_" + resizeDropdownW.Text.Replace(".", "");
				filepathText_C += "_" + resizeDropdownH.Text.Replace(".", "");
				filepathText_C += "-" + mapElementsDropdown.Text.Replace(", ", "") + ".oramap";
				

				System.IO.File.Copy(filepathText_A, filepathText_C, true);
				var package_C = new Folder(".").OpenPackage(filepathText_C, modData.ModFiles) as IReadWritePackage;
				Map_C = new Map(modData, package_C);
				Map_C.Title += "-Resize";
				Map_C.Title += "_" + resizeDropdownW.Text.Replace(".", "");
				Map_C.Title += "_" + resizeDropdownH.Text.Replace(".", "");
				Map_C.Title += "-" + mapElementsDropdown.Text.Replace(", ", "");

				///////////


				// RESIZE TOOL
				int Aw = activeResizeW.Item1; int Bw = activeResizeW.Item2;
				int Ah = activeResizeH.Item1; int Bh = activeResizeH.Item2;

				int W = A_W;
				int H = A_H;

				int W_new = Math.Max(1, (A_W * Aw) / Bw);
				int H_new = Math.Max(1, (A_H * Ah) / Bh);

				var A_X_new = A_X + W_new - W;
				var A_Y_new = A_Y + H_new - H;

				var LT_new = new PPos(A_left            , A_top            );
				var RB_new = new PPos(A_left + W_new - 1, A_top + H_new - 1);

				// fixing potential southern bound problem : resizeOffset
				int[] A_Tiles_lastRow_height = new int[A_X];
				for (int i = 0; i < A_X; i++)
				{
					var posA = new MPos(i, A_Y - 1);
					A_Tiles_lastRow_height[i] = Map_A.Height[posA];
				}

				var A_Tiles_lastRow_MaxHeight = A_Tiles_lastRow_height.Max();
				var A_bottomBound = A_Y - A_bottom;
				var resizeOffset = 0;

				if (A_bottomBound == A_Tiles_lastRow_MaxHeight) { resizeOffset = 1; }

				// resize and set bounds
				Map_C.Resize(A_X_new, A_Y_new + resizeOffset);
				Map_C.SetBounds(LT_new, RB_new); // radar widget bug ???

				// grid B resizing bigger
				for (int local_j_new = 1; local_j_new <= H_new; local_j_new++)
				{
					for (int local_i_new = 1; local_i_new <= W_new; local_i_new++)
					{
						var local_i_old = Math.Max(1, Math.Min(W, (local_i_new * Bw) / Aw));
						var local_j_old = Math.Max(1, Math.Min(H, (local_j_new * Bh) / Ah));

						var i_new = (local_i_new - 1) + A_left; var j_new = (local_j_new - 1) + A_top;
						var i_old = (local_i_old - 1) + A_left; var j_old = (local_j_old - 1) + A_top;

						var posC = new MPos(i_new, j_new);
						var posA = new MPos(i_old, j_old);

						if (usingTiles == 1) { Map_C.Tiles[posC] = Map_A.Tiles[posA]; Map_C.Height[posC] = Map_A.Height[posA]; };
						if (usingResources == 1) { Map_C.Resources[posC] = Map_A.Resources[posA]; };
					}
				}

				// cleaning bottom border, if resize Y < 1;
				if (Ah < Bh)
				{
					for (int j_old = A_bottom; j_old < A_Y; j_old++)
					{
						for (int i_old = 0; i_old < Math.Min(A_X, A_X_new); i_old++)
						{

							var i_new = i_old;
							var j_new = j_old + H_new - H;

							var posC = new MPos(i_new, j_new);
							var posA = new MPos(i_old, j_old);

							Map_C.Tiles[posC] = Map_A.Tiles[posA]; Map_C.Height[posC] = Map_A.Height[posA];
							Map_C.Resources[posC] = Map_A.Resources[posA];
						}
					}
				}



				// cleaning right border, if resize X < 1;
				if (Aw < Bw)
				{
					for (int j_old = 0; j_old < Math.Min(A_Y, A_Y_new); j_old++)
					{
						for (int i_old = A_right; i_old < A_X; i_old++)
						{
							var i_new = i_old + W_new - W;
							var j_new = j_old;

							var posC = new MPos(i_new, j_new);
							var posA = new MPos(i_old, j_old);

							Map_C.Tiles[posC] = Map_A.Tiles[posA]; Map_C.Height[posC] = Map_A.Height[posA];
							Map_C.Resources[posC] = Map_A.Resources[posA];
						}
					}
				}

				var forRemoval = new List<MiniYamlNode>();

				foreach (var kv in Map_C.ActorDefinitions)
				{
					forRemoval.Add(kv);
				}
				foreach (var kv in forRemoval)
				{
					Map_C.ActorDefinitions.Remove(kv);
				}

				foreach (var kv in Map_A.ActorDefinitions)
				{

					var actor = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
					var location = actor.InitDict.Get<LocationInit>().Value(null);
					var actType = actor.Type;

					var loc_MPos_old = new CPos(location.X, location.Y).ToMPos(Map_C);
					var X_old = loc_MPos_old.U; var Y_old = loc_MPos_old.V;
					var local_X_old = X_old - A_left; var local_Y_old = Y_old - A_top;

					var inBounds = ((A_left <= X_old) && (X_old < A_right)) && ((A_top <= Y_old) && (Y_old < A_bottom));
					if (inBounds)
					{
						if (usingActors == 1)
						{
							var local_X_new = Math.Min((local_X_old * Aw) / Bw, W_new - 1);
							var local_Y_new = Math.Min((local_Y_old * Ah) / Bh, H_new - 1);

							var X_new = local_X_new + A_left;
							var Y_new = local_Y_new + A_top;
							var loc_CPos_new = new MPos(X_new, Y_new).ToCPos(Map_C);
							var location_new = new LocationInit(loc_CPos_new);

							var actor_new = new ActorReference(actType) { location_new, new OwnerInit("Neutral") };
							Map_C.ActorDefinitions.Add(new MiniYamlNode("Actor" + Map_C.ActorDefinitions.Count, actor_new.Save()));

						};

						if (usingActors == 0)
						{
							Map_C.ActorDefinitions.Add(new MiniYamlNode("Actor" + Map_C.ActorDefinitions.Count, actor.Save()));
						};

					}

				}
				saveNewMap(onSelect, onExit);

			};

		}
	}
}
