#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
		public Form1(string[] mods)
		{
			InitializeComponent();
			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;

			currentMod = mods.FirstOrDefault() ?? "ra";

			Text = "OpenRA Editor (mod:{0})".F(currentMod);

			Game.modData = new ModData(currentMod);

			Rules.LoadRules(Game.modData.Manifest, new Map());

			surface1.AfterChange += OnMapChanged;
			surface1.SetMousePositionLabel(toolStripStatusLabelMousePosition);
		}

		void OnMapChanged()
		{
			MakeDirty();
			pmMiniMap.Image = Minimap.AddStaticResources(surface1.Map, Minimap.TerrainBitmap(surface1.Map, true));
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

			Game.modData = new ModData(currentMod);

			// load the map
			var map = new Map(new Folder(mapname, 0));

			// upgrade maps that have no player definitions. editor doesnt care,
			// but this breaks the game pretty badly.
			if (map.Players.Count == 0)
				map.Players.Add("Neutral", new PlayerReference("Neutral",
						Rules.Info["world"].Traits.WithInterface<CountryInfo>().First().Race, true, true));

			PrepareMapResources(Game.modData.Manifest, map);

			dirty = false;
		}

		void NewMap(Map map)
		{
			tilePalette.Controls.Clear();
			actorPalette.Controls.Clear();
			resourcePalette.Controls.Clear();

			loadedMapName = null;

			Game.modData = new ModData(currentMod);

			PrepareMapResources(Game.modData.Manifest, map);

			MakeDirty();
		}

		// this code is insanely stupid, and mostly my fault -- chrisf
		void PrepareMapResources(Manifest manifest, Map map)
		{
			Rules.LoadRules(manifest, map);
			tileset = Rules.TileSets[map.Theater];
			tileset.LoadTiles();
			var palette = new Palette(FileSystem.Open(tileset.Palette), true);


			surface1.Bind(map, tileset, palette);
			// construct the palette of tiles
			var palettes = new[] { tilePalette, actorPalette, resourcePalette };
			foreach (var p in palettes) { p.Visible = false; p.SuspendLayout(); }
			foreach (var t in tileset.Templates)
			{
				try
				{
					var bitmap = RenderUtils.RenderTemplate(tileset, (ushort)t.Key, palette);
					var ibox = new PictureBox
					{
						Image = bitmap,
						Width = bitmap.Width / 2,
						Height = bitmap.Height / 2,
						SizeMode = PictureBoxSizeMode.StretchImage
					};

					var brushTemplate = new BrushTemplate { Bitmap = bitmap, N = t.Key };
					ibox.Click += (_, e) => surface1.SetBrush(brushTemplate);

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

			var actorTemplates = new List<ActorTemplate>();

			foreach (var a in Rules.Info.Keys)
			{
				try
				{
					var info = Rules.Info[a];
					if (!info.Traits.Contains<RenderSimpleInfo>()) continue;
					var template = RenderUtils.RenderActor(info, tileset, palette);
					var ibox = new PictureBox
					{
						Image = template.Bitmap,
						Width = 32,
						Height = 32,
						SizeMode = PictureBoxSizeMode.Zoom,
						BorderStyle = BorderStyle.FixedSingle
					};


					ibox.Click += (_, e) => surface1.SetActor(template);

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
					var template = RenderUtils.RenderResourceType(a, tileset.Extensions, palette);
					var ibox = new PictureBox
					{
						Image = template.Bitmap,
						Width = 32,
						Height = 32,
						SizeMode = PictureBoxSizeMode.Zoom,
						BorderStyle = BorderStyle.FixedSingle
					};



					ibox.Click += (_, e) => surface1.SetResource(template);

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
		}

		void ResizeClicked(object sender, EventArgs e)
		{
			using (var rd = new ResizeDialog())
			{
				rd.width.Value = surface1.Map.MapSize.X;
				rd.height.Value = surface1.Map.MapSize.Y;
				rd.cordonLeft.Value = surface1.Map.TopLeft.X;
				rd.cordonTop.Value = surface1.Map.TopLeft.Y;
				rd.cordonRight.Value = surface1.Map.BottomRight.X;
				rd.cordonBottom.Value = surface1.Map.BottomRight.Y;

				if (DialogResult.OK != rd.ShowDialog())
					return;

				surface1.Map.TopLeft = new int2((int)rd.cordonLeft.Value, (int)rd.cordonTop.Value);
				surface1.Map.BottomRight = new int2((int)rd.cordonRight.Value, (int)rd.cordonBottom.Value);

				if ((int)rd.width.Value != surface1.Map.MapSize.X || (int)rd.height.Value != surface1.Map.MapSize.Y)
				{
					surface1.Map.Resize((int)rd.width.Value, (int)rd.height.Value);
					surface1.Bind(surface1.Map, surface1.TileSet, surface1.Palette);	// rebind it to invalidate all caches
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
				surface1.Map.PlayerCount = surface1.Map.Waypoints.Count;
				surface1.Map.Save(loadedMapName);
				dirty = false;
			}

		}

		void SaveAsClicked(object sender, EventArgs e)
		{
			using (var nms = new MapSelect())
			{
				nms.MapFolderPath = new string[] { Environment.CurrentDirectory, "mods", currentMod, "maps" }
				.Aggregate(Path.Combine);

				nms.txtNew.ReadOnly = false;
				nms.btnOk.Text = "Save";
				nms.txtNew.Text = "unnamed";
				nms.txtPathOut.ReadOnly = false;

				if (DialogResult.OK == nms.ShowDialog())
				{
					if (nms.txtNew.Text == "")
						nms.txtNew.Text = "unnamed";

					string mapfoldername = Path.Combine(nms.MapFolderPath, nms.txtNew.Text);
					loadedMapName = mapfoldername;

					try
					{
						Directory.CreateDirectory(mapfoldername);
					}
					catch (Exception ed)
					{
						MessageBox.Show("Directory creation failed: {0}", ed.ToString());
					}

					SaveClicked(sender, e);
				}
			}
		}

		void OpenClicked(object sender, EventArgs e)
		{
			using (var nms = new MapSelect())
			{
				nms.MapFolderPath = new string[] { Environment.CurrentDirectory, "mods", currentMod, "maps" }
				.Aggregate(Path.Combine);

				nms.txtNew.ReadOnly = true;
				nms.txtPathOut.ReadOnly = true;
				nms.btnOk.Text = "Open";

				if (DialogResult.OK == nms.ShowDialog())
				{
					string mapfoldername = Path.Combine(nms.MapFolderPath, nms.txtNew.Text);
					LoadMap(mapfoldername);
				}
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
					var map = new Map(nmd.theater.SelectedItem as string);

					map.Resize((int)nmd.width.Value, (int)nmd.height.Value);

					map.TopLeft = new int2((int)nmd.cordonLeft.Value, (int)nmd.cordonTop.Value);
					map.BottomRight = new int2((int)nmd.cordonRight.Value, (int)nmd.cordonBottom.Value);
					map.Players.Add("Neutral", new PlayerReference("Neutral", Rules.Info["world"].Traits.WithInterface<CountryInfo>().First().Race, true, true));
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

				if (DialogResult.OK != pd.ShowDialog())
					return;

				surface1.Map.Title = pd.title.Text;
				surface1.Map.Description = pd.desc.Text;
				surface1.Map.Author = pd.author.Text;
				surface1.Map.Selectable = pd.selectable.Checked;
			}
		}

		void SpawnPointsClicked(object sender, EventArgs e) { surface1.SetWaypoint(new WaypointTemplate()); }
		void Form1_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Space) surface1.IsPanning = true; }
		void Form1_KeyUp(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Space) surface1.IsPanning = false; }

		void CloseClicked(object sender, EventArgs e)
		{
			Close();
		}

		void ImportLegacyMapClicked(object sender, EventArgs e)
		{
			var currentDirectory = Directory.GetCurrentDirectory();
			using (var ofd = new OpenFileDialog { Filter = "Legacy maps (*.ini;*.mpr)|*.ini;*.mpr" })
				if (DialogResult.OK == ofd.ShowDialog())
				{
					Directory.SetCurrentDirectory( currentDirectory );
					/* massive hack: we should be able to call NewMap() with the imported Map object,
					 * but something's not right internally in it, unless loaded via the real maploader */

					var savePath = Path.Combine(Path.GetTempPath(), "OpenRA.Import");
					Directory.CreateDirectory(savePath);

					var map = LegacyMapImporter.Import(ofd.FileName);
					map.Players.Add("Neutral", new PlayerReference("Neutral",
						Rules.Info["world"].Traits.WithInterface<CountryInfo>().First().Race, true, true));

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

		private void layersFloaterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var pb = new PaletteBox();
			pb.Show();
		}

        private void mnuMinimapToPNG_Click(object sender, EventArgs e)
        {
            try
            {
                saveFileDialog.InitialDirectory = new string[] { Environment.CurrentDirectory, "maps" }
.Aggregate(Path.Combine);
            
            FileInfo file = new FileInfo(loadedMapName + ".png");
            string name = file.Name;
            saveFileDialog.FileName = name;

            saveFileDialog.ShowDialog();
            if (saveFileDialog.FileName == "")
            {
                saveFileDialog.FileName = name;
            }
            else
            {
                Bitmap png = new Bitmap(pmMiniMap.Image);

                png.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png); 
            }
            
            }
            catch { }
        }
	}
}
