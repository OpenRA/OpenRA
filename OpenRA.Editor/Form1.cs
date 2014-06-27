#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	public partial class Form1 : Form
	{
		public Form1(string[] args)
		{
			InitializeComponent();
			AppDomain.CurrentDomain.AssemblyResolve += GlobalFileSystem.ResolveAssembly;

			currentMod = args.FirstOrDefault() ?? "ra";

			toolStripComboBox1.Items.AddRange(ModMetadata.AllMods.Keys.ToArray());

			toolStripComboBox1.SelectedIndexChanged += (_, e) =>
			{
				tilePalette.SuspendLayout();
				actorPalette.SuspendLayout();
				resourcePalette.SuspendLayout();
				tilePalette.Controls.Clear();
				actorPalette.Controls.Clear();
				resourcePalette.Controls.Clear();
				tilePalette.ResumeLayout();
				actorPalette.ResumeLayout();
				resourcePalette.ResumeLayout();
				surface1.Bind(null, null, null, null, null);
				miniMapBox.Image = null;
				currentMod = toolStripComboBox1.SelectedItem as string;

				Game.modData = new ModData(currentMod);
				GlobalFileSystem.LoadFromManifest(Game.modData.Manifest);
				Program.Rules = Game.modData.RulesetCache.LoadDefaultRules();

				var mod = Game.modData.Manifest.Mod;
				Text = "{0} Mod Version: {1} - OpenRA Editor".F(mod.Title, mod.Version);

				loadedMapName = null;
			};

			toolStripComboBox1.SelectedItem = currentMod;

			surface1.AfterChange += OnMapChanged;
			surface1.MousePositionChanged += s => toolStripStatusLabelMousePosition.Text = s;
			surface1.ActorDoubleClicked += ActorDoubleClicked;

			if (args.Length >= 2)
				LoadMap(args[1]);
		}

		void OnMapChanged()
		{
			MakeDirty();
			var tileSet = Program.Rules.TileSets[surface1.Map.Tileset];
			miniMapBox.Image = Minimap.RenderMapPreview(tileSet, surface1.Map, true);
			cashToolStripStatusLabel.Text = CalculateTotalResource().ToString();
		}

		void ActorDoubleClicked(KeyValuePair<string, ActorReference> kv)
		{
			using (var apd = new ActorPropertiesDialog())
			{
				var name = kv.Key;
				apd.AddRow("(Name)", apd.MakeEditorControl(typeof(string), () => name, v => name = (string)v));
				apd.AddRow("(Type)", apd.MakeEditorControl(typeof(string), () => kv.Value.Type, v => kv.Value.Type = (string)v));

				var objSaved = kv.Value.Save();

				// TODO: make this work properly
				foreach (var init in Program.Rules.Actors[kv.Value.Type].GetInitKeys())
					apd.AddRow(init.First,
						apd.MakeEditorControl(init.Second,
							() =>
							{
								var nodesDict = objSaved.ToDictionary();
								return nodesDict.ContainsKey(init.First) ? nodesDict[init.First].Value : null;
							},
							_ => { }));

				apd.ShowDialog();

				// TODO: writeback
			}
		}

		void MakeDirty() { dirty = true; }

		string loadedMapName;
		string currentMod = "ra";
		TileSet tileset;
		TileSetRenderer tilesetRenderer;
		bool dirty = false;

		void LoadMap(string mapname)
		{
			tilePalette.Controls.Clear();
			actorPalette.Controls.Clear();
			resourcePalette.Controls.Clear();

			loadedMapName = mapname;

			// load the map
			var map = new Map(mapname);

			// upgrade maps that have no player definitions. editor doesnt care,
			// but this breaks the game pretty badly.
			if (map.Players.Count == 0)
				map.MakeDefaultPlayers();

			PrepareMapResources(Game.modData, map);

			// Calculate total net worth of resources in cash
			cashToolStripStatusLabel.Text = CalculateTotalResource().ToString();

			dirty = false;
		}

		void NewMap(Map map)
		{
			tilePalette.Controls.Clear();
			actorPalette.Controls.Clear();
			resourcePalette.Controls.Clear();

			loadedMapName = null;
			PrepareMapResources(Game.modData, map);

			MakeDirty();
		}

		// this code is insanely stupid, and mostly my fault -- chrisf
		void PrepareMapResources(ModData modData, Map map)
		{
			Program.Rules = map.Rules;

			tileset = Program.Rules.TileSets[map.Tileset];
			tilesetRenderer = new TileSetRenderer(tileset, modData.Manifest.TileSize);
			var shadowIndex = new int[] { 3, 4 };
			var palette = new Palette(GlobalFileSystem.Open(tileset.Palette), shadowIndex);

			// required for desert terrain in RA
			var playerPalette = tileset.PlayerPalette ?? tileset.Palette;
			var shadowedPalette = new Palette(GlobalFileSystem.Open(playerPalette), shadowIndex);

			surface1.Bind(map, tileset, tilesetRenderer, palette, shadowedPalette);

			// construct the palette of tiles
			var palettes = new[] { tilePalette, actorPalette, resourcePalette };
			foreach (var p in palettes) { p.Visible = false; p.SuspendLayout(); }

			var templateOrder = tileset.EditorTemplateOrder ?? new string[] { };
			foreach (var tc in tileset.Templates.GroupBy(t => t.Value.Category).OrderBy(t => Array.IndexOf(templateOrder, t.Key)))
			{
				var category = tc.Key ?? "(Uncategorized)";
				var categoryHeader = new Label
				{
					BackColor = SystemColors.Highlight,
					ForeColor = SystemColors.HighlightText,
					Text = category,
					AutoSize = false,
					Height = 24,
					TextAlign = ContentAlignment.MiddleLeft,
					Width = tilePalette.ClientSize.Width,
				};

				// hook this manually, anchoring inside FlowLayoutPanel is flaky.
				tilePalette.Resize += (_, e) => categoryHeader.Width = tilePalette.ClientSize.Width;

				if (tilePalette.Controls.Count > 0)
					tilePalette.SetFlowBreak(
						tilePalette.Controls[tilePalette.Controls.Count - 1], true);
				tilePalette.Controls.Add(categoryHeader);

				foreach (var t in tc)
				{
					try
					{
						var bitmap = tilesetRenderer.RenderTemplate((ushort)t.Key, palette);
						var ibox = new PictureBox
						{
							Image = bitmap,
							Width = bitmap.Width / 2,
							Height = bitmap.Height / 2,
							SizeMode = PictureBoxSizeMode.StretchImage
						};
	
						var brushTemplate = new BrushTemplate { Bitmap = bitmap, N = t.Key };
						ibox.Click += (_, e) => surface1.SetTool(new BrushTool(brushTemplate));
	
						var template = t.Value;
						tilePalette.Controls.Add(ibox);
						tt.SetToolTip(ibox, "{1}:{0} ({2}x{3})".F(template.Image, template.Id, template.Size.X, template.Size.Y));
					}
					catch { }
				}
			}

			var actorTemplates = new List<ActorTemplate>();

			foreach (var a in Program.Rules.Actors.Keys)
			{
				try
				{
					var info = Program.Rules.Actors[a];
					if (!info.Traits.Contains<RenderSimpleInfo>()) continue;

					var etf = info.Traits.GetOrDefault<EditorTilesetFilterInfo>();
					if (etf != null && etf.ExcludeTilesets != null
						&& etf.ExcludeTilesets.Contains(tileset.Id)) continue;
					if (etf != null && etf.RequireTilesets != null
						&& !etf.RequireTilesets.Contains(tileset.Id)) continue;

					var templatePalette = shadowedPalette;
					var rsi = info.Traits.GetOrDefault<RenderSimpleInfo>();

					// exception for desert buildings
					if (rsi != null && rsi.Palette != null && rsi.Palette.Contains("terrain"))
						templatePalette = palette;

					var template = RenderUtils.RenderActor(info, tileset, templatePalette);
					var ibox = new PictureBox
					{
						Image = template.Bitmap,
						Width = 32,
						Height = 32,
						SizeMode = PictureBoxSizeMode.Zoom,
						BorderStyle = BorderStyle.FixedSingle
					};

					ibox.Click += (_, e) => surface1.SetTool(new ActorTool(template));

					actorPalette.Controls.Add(ibox);

					tt.SetToolTip(ibox, "{0}".F(info.Name));

					actorTemplates.Add(template);
				}
				catch { }
			}

			surface1.BindActorTemplates(actorTemplates);

			var resourceTemplates = new List<ResourceTemplate>();

			foreach (var a in Program.Rules.Actors["world"].Traits.WithInterface<ResourceTypeInfo>())
			{
				try
				{
					var template = RenderUtils.RenderResourceType(a, tileset.Extensions, shadowedPalette);
					var ibox = new PictureBox
					{
						Image = template.Bitmap,
						Width = 32,
						Height = 32,
						SizeMode = PictureBoxSizeMode.Zoom,
						BorderStyle = BorderStyle.FixedSingle
					};

					ibox.Click += (_, e) => surface1.SetTool(new ResourceTool(template));

					resourcePalette.Controls.Add(ibox);

					tt.SetToolTip(ibox, "{0}:{1}cr".F(template.Info.Name, template.Info.ValuePerUnit));

					resourceTemplates.Add(template);
				}
				catch { }
			}

			surface1.BindResourceTemplates(resourceTemplates);

			foreach (var p in palettes)
			{
				p.Visible = true;
				p.ResumeLayout();
			}

			miniMapBox.Image = Minimap.RenderMapPreview(tileset, surface1.Map, true);

			propertiesToolStripMenuItem.Enabled = true;
			toolStripMenuItemProperties.Enabled = true;
			resizeToolStripMenuItem.Enabled = true;
			toolStripMenuItemResize.Enabled = true;
			saveToolStripMenuItem.Enabled = true;
			toolStripMenuItemSave.Enabled = true;
			saveAsToolStripMenuItem.Enabled = true;
			miniMapToPng.Enabled = true;

			PopulateActorOwnerChooser();
		}

		void PopulateActorOwnerChooser()
		{
			actorOwnerChooser.Items.Clear();
			actorOwnerChooser.Items.AddRange(surface1.Map.Players.Values.ToArray());
			actorOwnerChooser.SelectedIndex = 0;
			surface1.NewActorOwner = ((PlayerReference)actorOwnerChooser.SelectedItem).Name;
		}

		void ResizeClicked(object sender, EventArgs e)
		{
			using (var rd = new ResizeDialog())
			{
				rd.MapWidth.Value = surface1.Map.MapSize.X;
				rd.MapHeight.Value = surface1.Map.MapSize.Y;
				rd.CordonLeft.Value = surface1.Map.Bounds.Left;
				rd.CordonTop.Value = surface1.Map.Bounds.Top;
				rd.CordonRight.Value = surface1.Map.Bounds.Right;
				rd.CordonBottom.Value = surface1.Map.Bounds.Bottom;

				if (DialogResult.OK != rd.ShowDialog())
					return;

				surface1.Map.ResizeCordon((int)rd.CordonLeft.Value,
					(int)rd.CordonTop.Value,
					(int)rd.CordonRight.Value,
					(int)rd.CordonBottom.Value);

				if ((int)rd.MapWidth.Value != surface1.Map.MapSize.X || (int)rd.MapHeight.Value != surface1.Map.MapSize.Y)
				{
					surface1.Map.Resize((int)rd.MapWidth.Value, (int)rd.MapHeight.Value);
					surface1.Bind(surface1.Map, surface1.TileSet, surface1.TileSetRenderer, surface1.Palette, surface1.PlayerPalette);	// rebind it to invalidate all caches
				}

				surface1.Invalidate();
			}
		}

		void SaveClicked(object sender, EventArgs e)
		{
			if (loadedMapName == null)
				SaveAsClicked(sender, e);
			else
			{
				surface1.Map.RequiresMod = currentMod;
				surface1.Map.Save(loadedMapName);
				dirty = false;
			}
		}

		void SaveAsClicked(object sender, EventArgs e)
		{
			using (var nms = new MapSelect(currentMod))
			{
				nms.NewText.ReadOnly = false;
				nms.ButtonOkay.Text = "Save";
				nms.NewText.Text = "unnamed";
				nms.PathOutText.ReadOnly = false;

				if (DialogResult.OK == nms.ShowDialog())
				{
					if (nms.NewText.Text == "")
						nms.NewText.Text = "unnamed";

					// TODO: Allow the user to choose map format (directory vs oramap)
					loadedMapName = Path.Combine(nms.MapFolderPath, nms.NewText.Text + ".oramap");
					SaveClicked(sender, e);
				}
			}
		}

		void OpenClicked(object sender, EventArgs e)
		{
			using (var nms = new MapSelect(currentMod))
			{
				nms.NewText.ReadOnly = true;
				nms.PathOutText.ReadOnly = true;
				nms.ButtonOkay.Text = "Open";

				if (DialogResult.OK == nms.ShowDialog())
					LoadMap((string)nms.NewText.Tag);
			}
		}

		void NewClicked(object sender, EventArgs e)
		{
			using (var nmd = new NewMapDialog())
			{
				nmd.TheaterBox.Items.Clear();
				nmd.TheaterBox.Items.AddRange(Program.Rules.TileSets.Select(a => a.Value.Id).ToArray());
				nmd.TheaterBox.SelectedIndex = 0;

				if (DialogResult.OK == nmd.ShowDialog())
				{
					var tileset = Program.Rules.TileSets[nmd.TheaterBox.SelectedItem as string];
					var map = Map.FromTileset(tileset);

					map.Resize((int)nmd.MapWidth.Value, (int)nmd.MapHeight.Value);
					map.ResizeCordon((int)nmd.CordonLeft.Value, (int)nmd.CordonTop.Value,
						(int)nmd.CordonRight.Value, (int)nmd.CordonBottom.Value);

					map.Players.Clear();
					map.MakeDefaultPlayers();
					map.FixOpenAreas(Program.Rules);

					NewMap(map);
				}
			}
		}

		void PropertiesClicked(object sender, EventArgs e)
		{
			using (var pd = new PropertiesDialog())
			{
				pd.TitleBox.Text = surface1.Map.Title;
				pd.DescBox.Text = surface1.Map.Description;
				pd.AuthorBox.Text = surface1.Map.Author;
				pd.SelectableCheckBox.Checked = surface1.Map.Selectable;
				pd.ShellmapCheckBox.Checked = surface1.Map.UseAsShellmap;

				if (DialogResult.OK != pd.ShowDialog())
					return;

				surface1.Map.Title = pd.TitleBox.Text;
				surface1.Map.Description = pd.DescBox.Text;
				surface1.Map.Author = pd.AuthorBox.Text;
				surface1.Map.Selectable = pd.SelectableCheckBox.Checked;
				surface1.Map.UseAsShellmap = pd.ShellmapCheckBox.Checked;
			}
		}

		void Form1_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Space) surface1.IsPanning = true; }
		void Form1_KeyUp(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Space) surface1.IsPanning = false; }

		void CloseClicked(object sender, EventArgs e)
		{
			Close();
		}

		void OnFormClosing(object sender, FormClosingEventArgs e)
		{
			if (!dirty) return;

			switch (MessageBox.Show("The map has been modified since it was last saved. " + "\r\n" + "Save changes now?",
				"Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation))
			{
				case DialogResult.Yes: SaveClicked(null, EventArgs.Empty); break;
				case DialogResult.No: break;
				case DialogResult.Cancel: e.Cancel = true; break;
			}
		}

		void ExportMinimap(object sender, EventArgs e)
		{
			using (var sfd = new SaveFileDialog()
			{ 
				InitialDirectory = Path.Combine(Environment.CurrentDirectory, "maps"),
				DefaultExt = "*.png",
				Filter = "PNG Image (*.png)|*.png",
				Title = "Export Minimap to PNG",
				FileName = Path.ChangeExtension(loadedMapName, ".png"),
				RestoreDirectory = true
			})

			if (DialogResult.OK == sfd.ShowDialog())
				miniMapBox.Image.Save(sfd.FileName);
		}

		void ShowActorNamesClicked(object sender, EventArgs e)
		{
			showActorNamesToolStripMenuItem.Checked ^= true;
			toolStripMenuItemShowActorNames.Checked ^= true;
			surface1.ShowActorNames = showActorNamesToolStripMenuItem.Checked;
		}

		void ShowGridClicked(object sender, EventArgs e)
		{
			showGridToolStripMenuItem.Checked ^= true;
			toolStripMenuItemShowGrid.Checked ^= true;
			surface1.ShowGrid = showGridToolStripMenuItem.Checked;
			surface1.Chunks.Clear();
		}

		void FixOpenAreas(object sender, EventArgs e)
		{
			dirty = true;
			surface1.Map.FixOpenAreas(Program.Rules);
			surface1.Chunks.Clear();
			surface1.Invalidate();
		}

		void SetupDefaultPlayers(object sender, EventArgs e)
		{
			dirty = true;
			surface1.Map.Players.Clear();
			surface1.Map.MakeDefaultPlayers();

			surface1.Chunks.Clear();
			surface1.Invalidate();

			PopulateActorOwnerChooser();
		}

		void DrawPlayerListItem(object sender, DrawItemEventArgs e)
		{
			// color block
			var player = e.Index >= 0 ? (PlayerReference)((ComboBox)sender).Items[e.Index] : null;

			e.DrawBackground();
			e.DrawFocusRectangle();

			if (player == null)
				return;

			var color = player.Color.RGB;
			using (var brush = new SolidBrush(color))
				e.Graphics.FillRectangle(brush, e.Bounds.Left + 2, e.Bounds.Top + 2, e.Bounds.Height + 6, e.Bounds.Height - 4);
			using (var foreBrush = new SolidBrush(e.ForeColor))
				e.Graphics.DrawString(player.Name, e.Font, foreBrush, e.Bounds.Left + e.Bounds.Height + 12, e.Bounds.Top);
		}

		void OnSelectedPlayerChanged(object sender, EventArgs e)
		{
			var player = actorOwnerChooser.SelectedItem as PlayerReference;
			surface1.NewActorOwner = player.Name;
		}

		void CopySelectionToolStripMenuItemClick(object sender, EventArgs e)
		{
			surface1.CopySelection();
		}

		void OpenRAWebsiteToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.openra.net");
		}

		void OpenRAResourcesToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://resource.openra.net");
		}

		void WikiDocumentationToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://wiki.openra.net");
		}

		void DiscussionForumsToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.sleipnirstuff.com/forum/viewforum.php?f=80");
		}

		void IssueTrackerToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://bugs.openra.net");
		}

		void DeveloperBountiesToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://www.bountysource.com/trackers/36085-openra");
		}

		void SourceCodeToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://github.com/OpenRA/OpenRA");
		}

		void AboutToolStripMenuItemClick(object sender, EventArgs e)
		{
			MessageBox.Show("OpenRA and OpenRA Editor are Free/Libre Open Source Software released under the GNU General Public License version 3. See AUTHORS and COPYING for details.",
							"About",
							MessageBoxButtons.OK,
							MessageBoxIcon.Asterisk);
		}

		void HelpToolStripButton_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://wiki.openra.net/Mapping");
		}

		void ToolStripMenuItemNewClick(object sender, EventArgs e)
		{
			NewClicked(sender, e);
		}

		void ToolStripMenuItemOpenClick(object sender, EventArgs e)
		{
			OpenClicked(sender, e);
		}

		void ToolStripMenuItemSaveClick(object sender, EventArgs e)
		{
			SaveClicked(sender, e);
		}

		void ToolStripMenuItemPropertiesClick(object sender, EventArgs e)
		{
			PropertiesClicked(sender, e);
		}

		void ToolStripMenuItemResizeClick(object sender, EventArgs e)
		{
			ResizeClicked(sender, e);
		}

		void ToolStripMenuItemShowActorNamesClick(object sender, EventArgs e)
		{
			ShowActorNamesClicked(sender, e);
		}

		void ToolStripMenuItemFixOpenAreasClick(object sender, EventArgs e)
		{
			FixOpenAreas(sender, e);
		}

		void ToolStripMenuItemSetupDefaultPlayersClick(object sender, EventArgs e)
		{
			SetupDefaultPlayers(sender, e);
		}

		void ToolStripMenuItemCopySelectionClick(object sender, EventArgs e)
		{
			CopySelectionToolStripMenuItemClick(sender, e);
		}

		void ToolStripMenuItemShowGridClick(object sender, EventArgs e)
		{
			ShowGridClicked(sender, e);
		}
		
		public int CalculateTotalResource()
		{
			var totalResource = 0;
			for (var i = 0; i < surface1.Map.MapSize.X; i++)
				for (var j = 0; j < surface1.Map.MapSize.Y; j++)
				{
					var cell = new CPos(i, j);
					if (surface1.Map.MapResources.Value[cell].Type != 0)
						totalResource += GetResourceValue(i, j);
				}

			return totalResource;
		}
		
		int GetAdjecentCellsWith(int resourceType, int x, int y)
		{
			var sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
				{
					var cell = new CPos(x + u, y + v);

					if (!surface1.Map.Contains(cell))
						continue;

					if (surface1.Map.MapResources.Value[cell].Type == resourceType)
						++sum;
				}

			return sum;
		}

		int GetResourceValue(int x, int y)
		{
			var imageLength = 0;
			var type = surface1.Map.MapResources.Value[new CPos(x, y)].Type;
			var template = surface1.ResourceTemplates.FirstOrDefault(a => a.Value.Info.ResourceType == type).Value;
			if (type == 1)
				imageLength = 12;
			else if (type == 2)
				imageLength = 3;
			var density = (GetAdjecentCellsWith(type, x, y) * imageLength - 1) / 9;
			var value = template.Info.ValuePerUnit;
			return density * value;
		}

		void ZoomInToolStripButtonClick(object sender, System.EventArgs e)
		{
			if (surface1.Map == null) return;

			surface1.Zoom /= .75f;

			surface1.Invalidate();
		}

		void ZoomOutToolStripButtonClick(object sender, System.EventArgs e)
		{
			if (surface1.Map == null) return;

			surface1.Zoom *= .75f;

			surface1.Invalidate();
		}

		void PanToolStripButtonClick(object sender, System.EventArgs e)
		{
			panToolStripButton.Checked ^= true;
			surface1.IsPanning = panToolStripButton.Checked;
		}

		void ShowRulerToolStripMenuItemClick(object sender, EventArgs e)
		{
			showRulerToolStripMenuItem.Checked ^= true;
			showRulerToolStripItem.Checked ^= true;
			surface1.ShowRuler = showRulerToolStripMenuItem.Checked;
			surface1.Chunks.Clear();
		}

		void ShowRulerToolStripItemClick(object sender, System.EventArgs e)
		{
			ShowRulerToolStripMenuItemClick(sender, e);
		}

		void EraserToolStripButtonClick(object sender, System.EventArgs e)
		{
			eraserToolStripButton.Checked ^= true;
			surface1.IsErasing = eraserToolStripButton.Checked;
		}
	}
}
