#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	public partial class Form1 : Form
	{
		public Form1(string[] args)
		{
			InitializeComponent();
			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;

			currentMod = args.FirstOrDefault() ?? "ra";

			toolStripComboBox1.Items.AddRange(Mod.AllMods.Keys.ToArray());

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
				surface1.Bind(null, null, null, null);
				pmMiniMap.Image = null;
				currentMod = toolStripComboBox1.SelectedItem as string;

				Game.modData = new ModData(currentMod);
				FileSystem.LoadFromManifest(Game.modData.Manifest);
				Rules.LoadRules(Game.modData.Manifest, new Map());

				var mod = Game.modData.Manifest.Mods[0];
				Text = "{0} Mod Version: {1} - OpenRA Editor".F(Mod.AllMods[mod].Title, Mod.AllMods[mod].Version);			

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
			pmMiniMap.Image = Minimap.AddStaticResources(surface1.Map, Minimap.TerrainBitmap(surface1.Map, true));
			cashToolStripStatusLabel.Text = CalculateTotalResource().ToString();
		}

		void ActorDoubleClicked(KeyValuePair<string,ActorReference> kv)
		{
			using( var apd = new ActorPropertiesDialog() )
			{
				var name = kv.Key;
				apd.AddRow("(Name)", apd.MakeEditorControl(typeof(string), () => name, v => name = (string)v));
				apd.AddRow("(Type)", apd.MakeEditorControl(typeof(string), () => kv.Value.Type, v => kv.Value.Type = (string)v));

				var objSaved = kv.Value.Save();

				// TODO: make this work properly
				foreach( var init in Rules.Info[kv.Value.Type].GetInitKeys() )
					apd.AddRow(init.First,
						apd.MakeEditorControl(init.Second,
							() => objSaved.NodesDict.ContainsKey( init.First ) ? objSaved.NodesDict[init.First].Value : null,
							_ => {}));

				apd.ShowDialog();

				// TODO: writeback
			}
		}

		void MakeDirty() { dirty = true; }

		string loadedMapName;
		string currentMod = "ra";
		TileSet tileset;
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

			PrepareMapResources(Game.modData.Manifest, map);
			//Calculate total net worth of resources in cash
			cashToolStripStatusLabel.Text = CalculateTotalResource().ToString();

			dirty = false;
		}

		void NewMap(Map map)
		{
			tilePalette.Controls.Clear();
			actorPalette.Controls.Clear();
			resourcePalette.Controls.Clear();

			loadedMapName = null;
			PrepareMapResources(Game.modData.Manifest, map);

			MakeDirty();
		}

		// this code is insanely stupid, and mostly my fault -- chrisf
		void PrepareMapResources(Manifest manifest, Map map)
		{
			Rules.LoadRules(manifest, map);
			tileset = Rules.TileSets[map.Tileset];
			tileset.LoadTiles();
			int[] ShadowIndex = { 3, 4 };
			var palette = new Palette(FileSystem.Open(tileset.Palette), ShadowIndex);

			// required for desert terrain in RA
			var playerPalette = tileset.PlayerPalette ?? tileset.Palette;
			var PlayerPalette = new Palette(FileSystem.Open(playerPalette), ShadowIndex);

			surface1.Bind(map, tileset, palette, PlayerPalette);
			// construct the palette of tiles
			var palettes = new[] { tilePalette, actorPalette, resourcePalette };
			foreach (var p in palettes) { p.Visible = false; p.SuspendLayout(); }

			string[] templateOrder = tileset.EditorTemplateOrder ?? new string[]{};
			foreach (var tc in tileset.Templates.GroupBy(t => t.Value.Category).OrderBy(t => templateOrder.ToList().IndexOf(t.Key)))
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
				tilePalette.Resize += (_,e) => categoryHeader.Width = tilePalette.ClientSize.Width;

				if (tilePalette.Controls.Count > 0)
					tilePalette.SetFlowBreak(
						tilePalette.Controls[tilePalette.Controls.Count - 1], true);
				tilePalette.Controls.Add(categoryHeader);

				foreach( var t in tc )
				{
					try
					{
						var bitmap = tileset.RenderTemplate((ushort)t.Key, palette);
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
						tt.SetToolTip(ibox,
							"{1}:{0} ({2}x{3})".F(
							template.Image,
							template.Id,
							template.Size.X,
							template.Size.Y));
					}
					catch { }
				}
			}

			var actorTemplates = new List<ActorTemplate>();

			foreach (var a in Rules.Info.Keys)
			{
				try
				{
					var info = Rules.Info[a];
					if (!info.Traits.Contains<RenderSimpleInfo>()) continue;

					var etf = info.Traits.GetOrDefault<EditorTilesetFilterInfo>();
					if (etf != null && etf.ExcludeTilesets != null
						&& etf.ExcludeTilesets.Contains(tileset.Id)) continue;
					if (etf != null && etf.RequireTilesets != null
						&& !etf.RequireTilesets.Contains(tileset.Id)) continue;

					var TemplatePalette = PlayerPalette;
					var rsi = info.Traits.GetOrDefault<RenderSimpleInfo>();
					// exception for desert buildings
					if (rsi != null && rsi.Palette != null && rsi.Palette.Contains("terrain"))
						TemplatePalette = palette;

					var template = RenderUtils.RenderActor(info, tileset, TemplatePalette);
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

					tt.SetToolTip(ibox,
						"{0}".F(
						info.Name));

					actorTemplates.Add(template);
				}
				catch { }
			}

			surface1.BindActorTemplates(actorTemplates);

			var resourceTemplates = new List<ResourceTemplate>();

			foreach (var a in Rules.Info["world"].Traits.WithInterface<ResourceTypeInfo>())
			{
				try
				{
					var template = RenderUtils.RenderResourceType(a, tileset.Extensions, PlayerPalette);
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

					tt.SetToolTip(ibox,
						"{0}:{1}cr".F(
						template.Info.Name,
						template.Info.ValuePerUnit));

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

			pmMiniMap.Image = Minimap.AddStaticResources(surface1.Map, Minimap.TerrainBitmap(surface1.Map, true));

			propertiesToolStripMenuItem.Enabled = true;
			toolStripMenuItemProperties.Enabled = true;
			resizeToolStripMenuItem.Enabled = true;
			toolStripMenuItemResize.Enabled = true;
			saveToolStripMenuItem.Enabled = true;
			toolStripMenuItemSave.Enabled = true;
			saveAsToolStripMenuItem.Enabled = true;
			mnuMinimapToPNG.Enabled = true;	// TODO: what is this VB naming bullshit doing here?

			PopulateActorOwnerChooser();
		}

		void PopulateActorOwnerChooser()
		{
			actorOwnerChooser.Items.Clear();
			actorOwnerChooser.Items.AddRange(surface1.Map.Players.Values.ToArray());
			actorOwnerChooser.SelectedIndex = 0;
			surface1.NewActorOwner = (actorOwnerChooser.SelectedItem as PlayerReference).Name;
		}

		void ResizeClicked(object sender, EventArgs e)
		{
			using (var rd = new ResizeDialog())
			{
				rd.width.Value = surface1.Map.MapSize.X;
				rd.height.Value = surface1.Map.MapSize.Y;
				rd.cordonLeft.Value = surface1.Map.Bounds.Left;
				rd.cordonTop.Value = surface1.Map.Bounds.Top;
				rd.cordonRight.Value = surface1.Map.Bounds.Right;
				rd.cordonBottom.Value = surface1.Map.Bounds.Bottom;

				if (DialogResult.OK != rd.ShowDialog())
					return;

				surface1.Map.ResizeCordon((int)rd.cordonLeft.Value,
					(int)rd.cordonTop.Value,
					(int)rd.cordonRight.Value,
					(int)rd.cordonBottom.Value);

				if ((int)rd.width.Value != surface1.Map.MapSize.X || (int)rd.height.Value != surface1.Map.MapSize.Y)
				{
					surface1.Map.Resize((int)rd.width.Value, (int)rd.height.Value);
					surface1.Bind(surface1.Map, surface1.TileSet, surface1.Palette, surface1.PlayerPalette);	// rebind it to invalidate all caches
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
				surface1.Map.Save(loadedMapName);
				dirty = false;
			}

		}

		void SaveAsClicked(object sender, EventArgs e)
		{
			using (var nms = new MapSelect(currentMod))
			{
				nms.txtNew.ReadOnly = false;
				nms.btnOk.Text = "Save";
				nms.txtNew.Text = "unnamed";
				nms.txtPathOut.ReadOnly = false;

				if (DialogResult.OK == nms.ShowDialog())
				{
					if (nms.txtNew.Text == "")
						nms.txtNew.Text = "unnamed";

					// TODO: Allow the user to choose map format (directory vs oramap)
					loadedMapName = Path.Combine(nms.MapFolderPath, nms.txtNew.Text + ".oramap");
					SaveClicked(sender, e);
				}
			}
		}

		void OpenClicked(object sender, EventArgs e)
		{
			using (var nms = new MapSelect(currentMod))
			{
				nms.txtNew.ReadOnly = true;
				nms.txtPathOut.ReadOnly = true;
				nms.btnOk.Text = "Open";

				if (DialogResult.OK == nms.ShowDialog())
					LoadMap(nms.txtNew.Tag as string);
			}
		}

		void NewClicked(object sender, EventArgs e)
		{
			using (var nmd = new NewMapDialog())
			{
				nmd.theater.Items.Clear();
				nmd.theater.Items.AddRange(Rules.TileSets.Select(a => a.Value.Id).ToArray());
				nmd.theater.SelectedIndex = 0;

				if (DialogResult.OK == nmd.ShowDialog())
				{
					var map = Map.FromTileset(nmd.theater.SelectedItem as string);

					map.Resize((int)nmd.width.Value, (int)nmd.height.Value);
					map.ResizeCordon((int)nmd.cordonLeft.Value, (int)nmd.cordonTop.Value,
						(int)nmd.cordonRight.Value, (int)nmd.cordonBottom.Value);

					map.Players.Clear();
					map.MakeDefaultPlayers();

					NewMap(map);
				}
			}
		}

		void PropertiesClicked(object sender, EventArgs e)
		{
			using (var pd = new PropertiesDialog())
			{
				pd.title.Text = surface1.Map.Title;
				pd.desc.Text = surface1.Map.Description;
				pd.author.Text = surface1.Map.Author;
				pd.selectable.Checked = surface1.Map.Selectable;
				pd.useAsShellmap.Checked = surface1.Map.UseAsShellmap;

				if (DialogResult.OK != pd.ShowDialog())
					return;

				surface1.Map.Title = pd.title.Text;
				surface1.Map.Description = pd.desc.Text;
				surface1.Map.Author = pd.author.Text;
				surface1.Map.Selectable = pd.selectable.Checked;
				surface1.Map.UseAsShellmap = pd.useAsShellmap.Checked;
			}
		}

		void Form1_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Space) surface1.IsPanning = true; }
		void Form1_KeyUp(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Space) surface1.IsPanning = false; }

		void CloseClicked(object sender, EventArgs e)
		{
			Close();
		}

		void ImportLegacyMapClicked(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog { RestoreDirectory = true,
				Filter = "Legacy maps (*.ini;*.mpr)|*.ini;*.mpr" })
				if (DialogResult.OK == ofd.ShowDialog())
				{
					/* massive hack: we should be able to call NewMap() with the imported Map object,
					 * but something's not right internally in it, unless loaded via the real maploader */

					var savePath = Path.Combine(Path.GetTempPath(), "OpenRA.Import");
					Directory.CreateDirectory(savePath);

					var errors = new List<string>();

					var map = LegacyMapImporter.Import(ofd.FileName, a => errors.Add(a));

					if (errors.Count > 0)
						using (var eld = new ErrorListDialog(errors))
							eld.ShowDialog();

					map.MakeDefaultPlayers();

					map.Save(savePath);
					LoadMap(savePath);
					loadedMapName = null;	/* editor needs to think this hasnt been saved */

					Directory.Delete(savePath, true);
					MakeDirty();
				}
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
			using( var sfd = new SaveFileDialog() { 
				InitialDirectory = Path.Combine(Environment.CurrentDirectory, "maps"),
				DefaultExt = "*.png",
				Filter = "PNG Image (*.png)|*.png",
				Title = "Export Minimap to PNG",
				FileName = Path.ChangeExtension(loadedMapName, ".png"),
				RestoreDirectory = true } )
				if (DialogResult.OK == sfd.ShowDialog())
					pmMiniMap.Image.Save(sfd.FileName);
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
			var r = new Random();

			for (var j = surface1.Map.Bounds.Top; j < surface1.Map.Bounds.Bottom; j++)
				for (var i = surface1.Map.Bounds.Left; i < surface1.Map.Bounds.Right; i++)
				{
					var tr = surface1.Map.MapTiles.Value[i, j];
					if (tr.type == 0xff || tr.type == 0xffff || tr.type == 1 || tr.type == 2)
						tr.index = (byte)r.Next(0,
							Rules.TileSets[surface1.Map.Tileset].Templates[tr.type].Data.TileBitmapBytes.Count);

					surface1.Map.MapTiles.Value[i, j] = tr;
				}

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
			var player = e.Index >= 0 ? (PlayerReference)(sender as ComboBox).Items[e.Index] : null;

			e.DrawBackground();
			e.DrawFocusRectangle();

			if (player == null)
				return;

			var color = player.ColorRamp.GetColor(0);
			using( var brush = new SolidBrush(color) )
				e.Graphics.FillRectangle( brush, e.Bounds.Left + 2, e.Bounds.Top + 2, e.Bounds.Height + 6, e.Bounds.Height - 4 );
			using( var foreBrush = new SolidBrush(e.ForeColor) )
				e.Graphics.DrawString( player.Name, e.Font, foreBrush, e.Bounds.Left + e.Bounds.Height + 12, e.Bounds.Top );
		}

		void OnSelectedPlayerChanged(object sender, EventArgs e)
		{
			var player = actorOwnerChooser.SelectedItem as PlayerReference;
			surface1.NewActorOwner = player.Name;
		}

		private void copySelectionToolStripMenuItemClick(object sender, EventArgs e)
		{
			surface1.CopySelection();
		}

		private void openRAWebsiteToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.open-ra.org");
		}

		private void openRAResourcesToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://content.open-ra.org");
		}

		private void wikiDocumentationToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://github.com/OpenRA/OpenRA/wiki");
		}

		private void discussionForumsToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.sleipnirstuff.com/forum/viewforum.php?f=80");
		}

		private void issueTrackerToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://github.com/OpenRA/OpenRA/issues");
		}

		private void developerBountiesToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://www.bountysource.com/#repos/OpenRA/OpenRA");
		}

		private void sourceCodeToolStripMenuItemClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://github.com/OpenRA/OpenRA");
		}

		private void aboutToolStripMenuItemClick(object sender, EventArgs e)
		{
			MessageBox.Show("OpenRA and OpenRA Editor are Free/Libre Open Source Software released under the GNU General Public License version 3. See AUTHORS and COPYING for details.",
							"About",
							MessageBoxButtons.OK,
							MessageBoxIcon.Asterisk);
		}

		private void helpToolStripButton_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://github.com/OpenRA/OpenRA/wiki/Mapping");
		}

		private void toolStripMenuItemNewClick(object sender, EventArgs e)
		{
			NewClicked(sender, e);
		}

		private void toolStripMenuItemOpenClick(object sender, EventArgs e)
		{
			OpenClicked(sender, e);
		}

		private void toolStripMenuItemSaveClick(object sender, EventArgs e)
		{
			SaveClicked(sender, e);
		}

		private void toolStripMenuItemPropertiesClick(object sender, EventArgs e)
		{
			PropertiesClicked(sender, e);
		}

		private void toolStripMenuItemResizeClick(object sender, EventArgs e)
		{
			ResizeClicked(sender, e);
		}

		private void toolStripMenuItemShowActorNamesClick(object sender, EventArgs e)
		{
			ShowActorNamesClicked(sender, e);
		}

		private void toolStripMenuItemFixOpenAreasClick(object sender, EventArgs e)
		{
			FixOpenAreas(sender, e);
		}

		private void toolStripMenuItemSetupDefaultPlayersClick(object sender, EventArgs e)
		{
			SetupDefaultPlayers(sender, e);
		}

		private void toolStripMenuItemCopySelectionClick(object sender, EventArgs e)
		{
			copySelectionToolStripMenuItemClick(sender, e);
		}

		private void toolStripMenuItemShowGridClick(object sender, EventArgs e)
		{
			ShowGridClicked(sender, e);
		}
		
		public int CalculateTotalResource()
		{
			int TotalResource = 0;
			for(int i = 0; i < surface1.Map.MapSize.X; i++)
				for (int j = 0; j < surface1.Map.MapSize.Y; j++)
				{
					if (surface1.Map.MapResources.Value[i, j].type != 0)
						TotalResource += GetResourceValue(i, j);
				}
			return TotalResource;
		}
		
		int GetAdjecentCellsWith(int ResourceType, int x, int y)
		{
			int sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
				{
					if (!surface1.Map.IsInMap(new CPos(x + u, y + v)))
						continue;
					if (surface1.Map.MapResources.Value[x + u, y + v].type == ResourceType)
						++sum;
				}
			return sum;
		}

		int GetResourceValue(int x, int y)
		{
			int ImageLength = 0;
			int type = surface1.Map.MapResources.Value[x, y].type;
			var template = surface1.ResourceTemplates.Where(a => a.Value.Info.ResourceType == type).FirstOrDefault().Value;
			if (type == 1)
				ImageLength = 12;
			else if (type == 2)
				ImageLength = 3;
			int density = (GetAdjecentCellsWith(type ,x , y) * ImageLength - 1) / 9;
			int value = template.Info.ValuePerUnit;
			return (density) * value;
		}

		void zoomInToolStripButtonClick(object sender, System.EventArgs e)
		{
			if (surface1.Map == null) return;

			surface1.Zoom *= 4.0f / 3.0f;

			surface1.Invalidate();
		}

		void zoomOutToolStripButtonClick(object sender, System.EventArgs e)
		{
			if (surface1.Map == null) return;

			surface1.Zoom *= .75f;

			surface1.Invalidate();
		}

		void panToolStripButtonClick(object sender, System.EventArgs e)
		{
			panToolStripButton.Checked ^= true;
			surface1.IsPanning = panToolStripButton.Checked;
		}
	}
}
