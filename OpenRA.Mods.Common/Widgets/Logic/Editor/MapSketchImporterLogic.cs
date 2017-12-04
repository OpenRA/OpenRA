using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapSketchImporterLogic : ChromeLogic
	{
		Widget panel;
		DropDownButtonWidget[] terrainDropdowns = new DropDownButtonWidget[13];
		DropDownButtonWidget[] resourceDropdowns = new DropDownButtonWidget[13];
		private enum ColorIndex { White, Black, Gray, Red, Orange, Yellow, Green, Teal, LightBlue, DarkBlue, Purple, Pink, Magenta }

		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}

		static Dictionary<int, string> defaultFillerTilesd2k = new Dictionary<int, string>
			{ // Default Color Index: Terrain Selection Display Name
				{ (int)ColorIndex.White,     "Sand" },
				{ (int)ColorIndex.Black,     "Cliff" },
				{ (int)ColorIndex.Gray,      "Rock" },
				{ (int)ColorIndex.Red,       "S.Rough" },
				{ (int)ColorIndex.Orange,    "Dune" },
				{ (int)ColorIndex.LightBlue, "R.Rough" }
			};

		static Dictionary<string, Dictionary<int, string>> modDefaultFillersD2k = new Dictionary<string, Dictionary<int, string>>
			{
				{ "ARRAKIS", defaultFillerTilesd2k }
			};

		static Dictionary<int, string> defaultFillerTilesRASnow = new Dictionary<int, string>
			{ // Default Color Index: Terrain Selection Display Name
				{ (int)ColorIndex.White,     "Snow" },
				{ (int)ColorIndex.DarkBlue,  "Water" },
				{ (int)ColorIndex.LightBlue, "River" },
				{ (int)ColorIndex.Teal,      "W.Cliff" },
				{ (int)ColorIndex.Black,     "Cliff" },
				{ (int)ColorIndex.Gray,      "Road" },
				{ (int)ColorIndex.Green,     "Forest" }
			};
		static Dictionary<int, string> defaultFillerTilesRADesert = new Dictionary<int, string>
			{ // Default Color Index: Terrain Selection Display Name
				{ (int)ColorIndex.White,     "Desert" },
				{ (int)ColorIndex.DarkBlue,  "Water" },
				{ (int)ColorIndex.LightBlue, "River" },
				{ (int)ColorIndex.Teal,      "W.Cliff" },
				{ (int)ColorIndex.Black,     "Cliff" },
				{ (int)ColorIndex.Gray,      "Road" }
			};
		static Dictionary<int, string> defaultFillerTilesRATemperat = new Dictionary<int, string>
			{ // Default Color Index: Terrain Selection Display Name
				{ (int)ColorIndex.White,     "Grass" },
				{ (int)ColorIndex.DarkBlue,  "Water" },
				{ (int)ColorIndex.LightBlue, "River" },
				{ (int)ColorIndex.Teal,      "W.Cliff" },
				{ (int)ColorIndex.Black,     "Cliff" },
				{ (int)ColorIndex.Gray,      "Road" },
				{ (int)ColorIndex.Green,     "Forest" }
			};
		static Dictionary<int, string> defaultFillerTilesRAInterior = new Dictionary<int, string>
			{ // Default Color Index: Terrain Selection Display Name
				{ (int)ColorIndex.Black,     "Dark" },
				{ (int)ColorIndex.White,     "Cobble" },
				{ (int)ColorIndex.Gray,      "Plate" },
				{ (int)ColorIndex.Red,       "Guide" },
				{ (int)ColorIndex.Yellow,    "Tape" },
				{ (int)ColorIndex.LightBlue, "xWall" },
				{ (int)ColorIndex.DarkBlue,  "yWall" }
			};

		static Dictionary<string, Dictionary<int, string>> modDefaultFillersRA = new Dictionary<string, Dictionary<int, string>>
			{
				{ "TEMPERAT", defaultFillerTilesRATemperat },
				{ "SNOW", defaultFillerTilesRASnow },
				{ "INTERIOR", defaultFillerTilesRAInterior },
				{ "DESERT", defaultFillerTilesRADesert }
			};

		static Dictionary<int, string> defaultFillerTilesCncDesert = new Dictionary<int, string>
			{ // Default Color Index: Terrain Selection Display Name
				{ (int)ColorIndex.White,     "Basic" },
				{ (int)ColorIndex.DarkBlue,  "Water" },
				{ (int)ColorIndex.Teal,      "River" },
				{ (int)ColorIndex.Black,     "Cliff" },
				{ (int)ColorIndex.Gray,      "Road" },
		};

		static Dictionary<int, string> defaultFillerTilesCncOther = new Dictionary<int, string>
			{ // Default Color Index: Terrain Selection Display Name
				{ (int)ColorIndex.White,     "Basic" },
				{ (int)ColorIndex.DarkBlue,  "Water" },
				{ (int)ColorIndex.Teal,      "River" },
				{ (int)ColorIndex.Black,     "Cliff" },
				{ (int)ColorIndex.Gray,      "Road" },
				{ (int)ColorIndex.Orange,      "Forest" }
		};

		static Dictionary<string, Dictionary<int, string>> modDefaultFillersCnc = new Dictionary<string, Dictionary<int, string>>
			{
				{ "TEMPERAT", defaultFillerTilesCncOther },
				{ "SNOW", defaultFillerTilesCncOther },
				{ "WINTER", defaultFillerTilesCncOther },
				{ "JUNGLE", defaultFillerTilesCncOther },
				{ "DESERT", defaultFillerTilesCncDesert }
			};

		static Dictionary<string, Tuple<ushort, int, int>> fillerTilesArrakis = new Dictionary<string, Tuple<ushort, int, int>>
			{ // Tile Selection Name: (TileID, lowest random bit, highest random bit)
				{ "Sand", Tuple.Create((ushort)0,       0, 12) },
				{ "Cliff", Tuple.Create((ushort)197,    4, 4) },
				{ "Rock", Tuple.Create((ushort)266,     0, 16) },
				{ "S.Rough", Tuple.Create((ushort)119,   0, 3) },
				{ "Dune", Tuple.Create((ushort)224,      0, 3) },
				{ "R.Rough", Tuple.Create((ushort)101,   0, 8) }
			};

		static Dictionary<string, Dictionary<string, Tuple<ushort, int, int>>> modFillersD2k = new Dictionary<string, Dictionary<string, Tuple<ushort, int, int>>>
			{
				{ "ARRAKIS", fillerTilesArrakis }
			};

		static Dictionary<string, Tuple<ushort, int, int>> fillerTilesRASnowTemp = new Dictionary<string, Tuple<ushort, int, int>>
			{ // Tile Selection Name: (TileID, lowest random bit, highest random bit)
				{ "Snow", Tuple.Create((ushort)255,       0, 19) },
				{ "Grass", Tuple.Create((ushort)255,      0, 15) },
				{ "Cliff", Tuple.Create((ushort)137,      0, 0) },
				{ "W.Cliff", Tuple.Create((ushort)61,     2, 2) },
				{ "River", Tuple.Create((ushort)114,       4, 4) },
				{ "Road", Tuple.Create((ushort)178,        0, 0) },
				{ "Water", Tuple.Create((ushort)2,         0, 3) },
				{ "Forest", Tuple.Create((ushort)255,      0, 15) } // Special Handling, trees arent exactly terrain
			};

		static string[] treeTypesRACnc = { "t01", "t02", "t03", "t05", "t06", "t07", "t08", "t10", "t11", "t12", "t13", "t14", "t15", "t16", "t17" };

		static Dictionary<string, Tuple<ushort, int, int>> fillerTilesRADesert = new Dictionary<string, Tuple<ushort, int, int>>
			{ // Tile Selection Name: (TileID, lowest random bit, highest random bit)
				{ "Desert", Tuple.Create((ushort)255,     0, 15) },
				{ "Cliff", Tuple.Create((ushort)182,      0, 0) },
				{ "W.Cliff", Tuple.Create((ushort)302,    2, 2) },
				{ "River", Tuple.Create((ushort)63,        6, 6) },
				{ "Road", Tuple.Create((ushort)125,        0, 0) },
				{ "Water", Tuple.Create((ushort)257,       0, 3) }
			};

		static Dictionary<string, Tuple<ushort, int, int>> fillerTilesRAInterior = new Dictionary<string, Tuple<ushort, int, int>>
			{ // Tile Selection Name: (TileID, lowest random bit, highest random bit)
				{ "Dark", Tuple.Create((ushort)255,     0, 15) },
				{ "xWall", Tuple.Create((ushort)330,      0, 4) }, // Special Handling, variations saved as tiles, not bits.
				{ "yWall", Tuple.Create((ushort)340,      0, 6) }, // Special Handling, variations saved as tiles, not bits.
				{ "Plate", Tuple.Create((ushort)268,       0, 11) },
				{ "Cobble", Tuple.Create((ushort)275,      0, 12) },
				{ "Guide", Tuple.Create((ushort)253,       0, 5) },
				{ "Tape", Tuple.Create((ushort)319,        0, 11) }
			};

		static ushort[] xWallVariationIds = { 330, 332, 334, 336, 338 };
		static ushort[] yWallVariationIds = { 340, 341, 342, 343, 344, 345, 346 };

		static Dictionary<string, Dictionary<string, Tuple<ushort, int, int>>> modFillersRA =
			new Dictionary<string, Dictionary<string, Tuple<ushort, int, int>>>
			{
				{ "TEMPERAT", fillerTilesRASnowTemp },
				{ "SNOW", fillerTilesRASnowTemp },
				{ "INTERIOR", fillerTilesRAInterior },
				{ "DESERT", fillerTilesRADesert }
			};

		static Dictionary<string, Tuple<ushort, int, int>> fillerTilesCncDesert = new Dictionary<string, Tuple<ushort, int, int>>
			{ // Tile Selection Name: (TileID, lowest random bit, highest random bit)
				{ "Basic", Tuple.Create((ushort)255, 0, 15) },
				{ "Water", Tuple.Create((ushort)1, 0, 0) },
				{ "River", Tuple.Create((ushort)152, 6, 6) },
				{ "Cliff", Tuple.Create((ushort)15, 0, 0) },
				{ "Road", Tuple.Create((ushort)98, 0, 0) }
			};

		static Dictionary<string, Tuple<ushort, int, int>> fillerTilesCncOther = new Dictionary<string, Tuple<ushort, int, int>>
			{ // Tile Selection Name: (TileID, lowest random bit, highest random bit)
				{ "Basic", Tuple.Create((ushort)255, 0, 15) },
				{ "Water", Tuple.Create((ushort)2, 0, 3) },
				{ "River", Tuple.Create((ushort)138, 4, 4) },
				{ "Cliff", Tuple.Create((ushort)15, 0, 0) },
				{ "Road", Tuple.Create((ushort)98, 0, 0) },
				{ "Forest", Tuple.Create((ushort)255, 0, 15) } // Special Handling, trees arent exactly terrain
			};

		static Dictionary<string, Dictionary<string, Tuple<ushort, int, int>>> modFillersCnc =
			new Dictionary<string, Dictionary<string, Tuple<ushort, int, int>>>
			{
				{ "TEMPERAT", fillerTilesCncOther },
				{ "SNOW", fillerTilesCncOther },
				{ "WINTER", fillerTilesCncOther },
				{ "JUNGLE", fillerTilesCncOther },
				{ "DESERT", fillerTilesCncDesert }
			};

		static Dictionary<int, string> defaultFillerResourcesD2k = new Dictionary<int, string>
			{ // Default Color Index: Resource Selection Display Name
				{ 0, "-" }, // None Dummy
				{ (int)ColorIndex.Yellow, "Spice" }
			};

		static Dictionary<string, Tuple<byte, byte, byte>> fillerResourcesD2k = new Dictionary<string, Tuple<byte, byte, byte>>
			{
				{ "-", Tuple.Create((byte)0, (byte)0, (byte)0) }, // Dummy
				{ "Spice", Tuple.Create((byte)1, (byte)1, (byte)1) }
			};

		static Dictionary<int, string> defaultFillerResourcesCnc = new Dictionary<int, string>
			{ // Default Color Index: Resource Selection Display Name
				{ 0, "-" }, // None Dummy
				{ (int)ColorIndex.Green, "Green Tib." },
				{ (int)ColorIndex.LightBlue, "Blue Tib." }
			};

		static Dictionary<string, Tuple<byte, byte, byte>> fillerResourcesCnc = new Dictionary<string, Tuple<byte, byte, byte>>
			{
				{ "-", Tuple.Create((byte)0, (byte)0, (byte)0) }, // Dummy
				{ "Green Tib.", Tuple.Create((byte)1, (byte)1, (byte)1) },
				{ "Blue Tib.", Tuple.Create((byte)2, (byte)1, (byte)1) }
			};

		static Dictionary<int, string> defaultFillerResourcesRA = new Dictionary<int, string>
			{ // Default Color Index: Resource Selection Display Name
				{ 0, "-" }, // None Dummy
				{ (int)ColorIndex.Yellow, "Ore" },
				{ (int)ColorIndex.Orange, "Gems" }
			};

		static Dictionary<string, Tuple<byte, byte, byte>> fillerResourcesRA = new Dictionary<string, Tuple<byte, byte, byte>>
			{
				{ "-", Tuple.Create((byte)0, (byte)0, (byte)0) }, // Dummy
				{ "Ore", Tuple.Create((byte)1, (byte)1, (byte)1) },
				{ "Gems", Tuple.Create((byte)2, (byte)1, (byte)1) }
			};

		static int GetIndexFromHSB(int hue, int sat, int bright)
			{
				if (bright >= 90) return (int)ColorIndex.White;
				if (bright <= 10) return (int)ColorIndex.Black;
				if (sat == 0) return (int)ColorIndex.Gray; // all shades of pure gray
				 // subjective and arbitrary upper color borders:
				if (hue < 22) return (int)ColorIndex.Red; // red part 1
				if (hue < 45) return (int)ColorIndex.Orange;
				if (hue < 62) return (int)ColorIndex.Yellow;
				if (hue < 135) return (int)ColorIndex.Green;
				if (hue < 167) return (int)ColorIndex.Teal;
				if (hue < 208) return (int)ColorIndex.LightBlue;
				if (hue < 260) return (int)ColorIndex.DarkBlue;
				if (hue < 276) return (int)ColorIndex.Purple;
				if (hue < 313) return (int)ColorIndex.Pink;
				if (hue < 341) return (int)ColorIndex.Magenta;
				return (int)ColorIndex.Red; // remaining red
			}

		[ObjectCreator.UseCtor]
		public MapSketchImporterLogic(Widget widget, World world, ModData modData, Action<string> onSelect, Action onExit)
		{
			panel = widget;

			Action showPanel = () => panel.Visible = true;

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			var tilesetDropDown = panel.Get<DropDownButtonWidget>("TILESET");
			tilesetDropDown.Text = modData.DefaultTileSets.Select(kv => kv.Key).First();

			var activeTerrainFillers = new Dictionary<string, Tuple<ushort, int, int>>();
			var activeDefaultTerrainFillers = new Dictionary<int, string>();
			var activeResourceFillers = new Dictionary<string, Tuple<byte, byte, byte>>();
			var activeDefaultResourceFillers = new Dictionary<int, string>();
			var activeModFillers = new Dictionary<string, Dictionary<string, Tuple<ushort, int, int>>>();
			var activeModDefaultFillers = new Dictionary<string, Dictionary<int, string>>();

			if (modData.Manifest.Id == "d2k")
			{
				activeModDefaultFillers = modDefaultFillersD2k;
				activeModFillers = modFillersD2k;
				activeResourceFillers = fillerResourcesD2k;
				activeDefaultResourceFillers = defaultFillerResourcesD2k;
			}

			if (modData.Manifest.Id == "ra")
			{
				activeModDefaultFillers = modDefaultFillersRA;
				activeModFillers = modFillersRA;
				activeResourceFillers = fillerResourcesRA;
				activeDefaultResourceFillers = defaultFillerResourcesRA;
			}

			if (modData.Manifest.Id == "cnc")
			{
				activeModDefaultFillers = modDefaultFillersCnc;
				activeModFillers = modFillersCnc;
				activeResourceFillers = fillerResourcesCnc;
				activeDefaultResourceFillers = defaultFillerResourcesCnc;
			}

			activeTerrainFillers = activeModFillers[tilesetDropDown.Text];
			activeDefaultTerrainFillers = activeModDefaultFillers[tilesetDropDown.Text];

			tilesetDropDown.OnMouseDown = _ =>
			{
				var tilesets = modData.DefaultTileSets.Select(kv => new DropDownOption
				{
					 Title = kv.Key,
					 IsSelected = () => tilesetDropDown.Text == kv.Key,
					 OnClick = () =>
						{
							tilesetDropDown.Text = kv.Key;
							activeDefaultTerrainFillers = activeModDefaultFillers[kv.Key];
							activeTerrainFillers = activeModFillers[kv.Key];

							// refreshing Color->Terrain Defaults
							for (int i = 0; i < 13; i++)
							{
								if (activeDefaultTerrainFillers.ContainsKey(i))
									terrainDropdowns[i].Text = activeDefaultTerrainFillers[i];
								else
									terrainDropdowns[i].Text = activeDefaultTerrainFillers.Select(p => p.Value).First();
							}
						}
				});

				Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
					return item;
				};

				tilesetDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", tilesets.Count() * 30, tilesets, setupItem);
			};

			for (int i = 0; i < 13; i++)
			{
				var terrainDropDown = panel.Get<DropDownButtonWidget>("TERRAIN_SELECT_" + i);

				Func<string, ScrollItemWidget, ScrollItemWidget> setupTerrainChoice = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => terrainDropDown.Text == option,
						() => { terrainDropDown.Text = option; });
					item.Get<LabelWidget>("LABEL").GetText = () => option;
					return item;
				};
				if (activeDefaultTerrainFillers.ContainsKey(i))
				{
					terrainDropDown.Text = activeDefaultTerrainFillers[i];
				}
				else {
					terrainDropDown.Text = activeDefaultTerrainFillers.Select(p => p.Value).First();
				}

				terrainDropDown.OnClick = () =>
					terrainDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, activeDefaultTerrainFillers.Select(p => p.Value), setupTerrainChoice);
				terrainDropdowns[i] = terrainDropDown;
			}

			for (int i = 0; i < 13; i++)
			{
				var resourceDropDown = panel.Get<DropDownButtonWidget>("RESOURCE_SELECT_" + i);
				Func<string, ScrollItemWidget, ScrollItemWidget> setupResourceChoice = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => resourceDropDown.Text == option,
						() => { resourceDropDown.Text = option; });
					item.Get<LabelWidget>("LABEL").GetText = () => option;
					return item;
				};

				if (activeDefaultResourceFillers.ContainsKey(i))
					resourceDropDown.Text = activeDefaultResourceFillers[i];
				else
					resourceDropDown.Text = activeDefaultResourceFillers.Select(p => p.Value).First();

				resourceDropDown.OnClick = () =>
					resourceDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, activeDefaultResourceFillers.Select(p => p.Value), setupResourceChoice);
				resourceDropdowns[i] = resourceDropDown;
			}

			// needs file selection dialog
			var simpleFilepathTextField = panel.Get<TextFieldWidget>("MAP_SKETCH_FILE_PATH");

			panel.Get<ButtonWidget>("CREATE_BUTTON").OnClick = () =>
			{
				// checks: file exists? file of .bmp type?
				if (!File.Exists(simpleFilepathTextField.Text))
				{
					panel.Visible = false;
					ConfirmationDialogs.ButtonPrompt(
						title: "File not found",
						text: "File does not exist or could not be accessed.",
						onConfirm: showPanel);
					return;
				}

				var file = File.Open(simpleFilepathTextField.Text, FileMode.Open, FileAccess.Read);
				try
				{
					Bitmap tmp = new Bitmap(file);
				}
				catch (ArgumentException e)
				{
					panel.Visible = false;
					ConfirmationDialogs.ButtonPrompt(
						title: "Incompatible file format",
						text: "File must be of bitmap (.bmp) type!",
						onConfirm: showPanel);
					return;
				}

				Bitmap MapSketch = new Bitmap(file);
				var height = MapSketch.Height;
				var width = MapSketch.Width;

				// impose max height*width limitation to avoid map save crash due to limited memory
				if (height * width > 888 * 888)
				{
					panel.Visible = false;
					ConfirmationDialogs.ButtonPrompt(
						title: "Map size too big",
						text: "The map you tried to generate is of size " + width + "x" + height + ".\n This is too big. Reduce the size below 888x888.",
						onConfirm: showPanel);
					return;
				}

				width = Math.Max(2, width);
				height = Math.Max(2, height);

				var maxTerrainHeight = world.Map.Grid.MaximumTerrainHeight;
				var tileset = modData.DefaultTileSets[tilesetDropDown.Text];
				var map = new Map(Game.ModData, tileset, width + 2, height + maxTerrainHeight + 2);

				var tl = new PPos(1, 1 + maxTerrainHeight);
				var br = new PPos(width, height + maxTerrainHeight);
				map.SetBounds(tl, br);

				map.PlayerDefinitions = new MapPlayers(map.Rules, 0).ToMiniYaml();

				var r = new Random();
				for (int i = 1 + maxTerrainHeight; i <= height + maxTerrainHeight; i++)
				{
					for (int j = 1; j <= width; j++)
					{
						var pixel = MapSketch.GetPixel(j - 1, i - 1 - maxTerrainHeight);
						var hue = (int)pixel.GetHue();
						var sat = (int)(100.0 * pixel.GetSaturation());
						var bright = (int)(100.0 * pixel.GetBrightness());

						var index = GetIndexFromHSB(hue, sat, bright);
						var tileName = terrainDropdowns[index].GetText();
						var terrainTuple = activeTerrainFillers[tileName];
						var resourceTuple = activeResourceFillers[resourceDropdowns[index].GetText()];
						var pos = new CPos(j, i);

						TerrainTile tile;
						if (tileName == "xWall")
						{
							tile = new TerrainTile(xWallVariationIds[r.Next(terrainTuple.Item2, terrainTuple.Item3)], (byte)0);
						}
						else if (tileName == "yWall")
						{
							tile = new TerrainTile(yWallVariationIds[r.Next(terrainTuple.Item2, terrainTuple.Item3)], (byte)0);
						}
						else if (tileName == "Forest")
						{
							var actType = treeTypesRACnc[r.Next(0, 14)]; // single tree types for RA TMP/SNO, CNC TMP/SNO/WIN/JUN
							// CPos(j, i-1) needed here instead of CPos(j, i) because Trees are size 2x2
							var act = new ActorReference(actType) { new LocationInit(new CPos(j, i - 1)), new OwnerInit("Neutral") };
							map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, act.Save()));
							tile = new TerrainTile(terrainTuple.Item1, (byte)r.Next(terrainTuple.Item2, terrainTuple.Item3));
						}
						else
							tile = new TerrainTile(terrainTuple.Item1, (byte)r.Next(terrainTuple.Item2, terrainTuple.Item3));

						map.Tiles[pos] = tile;
						if (resourceTuple.Item1 != 0) {
							map.Resources[pos] = new ResourceTile(resourceTuple.Item1, resourceTuple.Item2);
						}
					}
				}

				file.Close();

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
					{ "map", map },
					{ "playerDefinitions", map.PlayerDefinitions },
					{ "actorDefinitions", map.ActorDefinitions }
				});
			};
		}
	}
}
