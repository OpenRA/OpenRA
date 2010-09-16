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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Graphics;

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

			Game.modData = new ModData( currentMod );

			Rules.LoadRules(Game.modData.Manifest, new Map());

			folderBrowser.SelectedPath = new string[] { Environment.CurrentDirectory, "mods", currentMod, "maps" }
				.Aggregate(Path.Combine);

			surface1.AfterChange += MakeDirty;
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

			Game.modData = new ModData( currentMod );

			// load the map
			var map = new Map(new Folder(mapname));


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

			Game.modData = new ModData( currentMod );

			PrepareMapResources(Game.modData.Manifest, map);

			MakeDirty();
		}

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
					var bitmap = RenderTemplate(tileset, (ushort)t.Key, palette);
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
					if( !info.Traits.Contains<RenderSimpleInfo>() ) continue;
					var template = RenderActor(info, tileset, palette);
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
					var template = RenderResourceType(a, tileset.Extensions, palette);
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

            foreach (var p in palettes) { p.Visible = true; p.ResumeLayout();
            }
		}



		static Bitmap RenderTemplate(TileSet ts, ushort n, Palette p)
		{
			var template = ts.Templates[n];
			var tile = ts.Tiles[n];

			var bitmap = new Bitmap(ts.TileSize * template.Size.X, ts.TileSize * template.Size.Y);
			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* q = (int*)data.Scan0.ToPointer();
				var stride = data.Stride >> 2;

				for (var u = 0; u < template.Size.X; u++)
					for (var v = 0; v < template.Size.Y; v++)
						if (tile.TileBitmapBytes[u + v * template.Size.X] != null)
						{
							var rawImage = tile.TileBitmapBytes[u + v * template.Size.X];
							for (var i = 0; i < ts.TileSize; i++)
								for (var j = 0; j < ts.TileSize; j++)
									q[(v * ts.TileSize + j) * stride + u * ts.TileSize + i] = p.GetColor(rawImage[i + ts.TileSize * j]).ToArgb();
						}
						else
						{
							for (var i = 0; i < ts.TileSize; i++)
								for (var j = 0; j < ts.TileSize; j++)
									q[(v * ts.TileSize + j) * stride + u * ts.TileSize + i] = Color.Transparent.ToArgb();
						}
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}

		static ActorTemplate RenderActor(ActorInfo info, TileSet tileset, Palette p)
		{
			var ri = info.Traits.Get<RenderSimpleInfo>();
			string image = null;
			if (ri.OverrideTheater != null)
				for (int i = 0; i < ri.OverrideTheater.Length; i++)
					if (ri.OverrideTheater[i] == tileset.Id)
						image = ri.OverrideImage[i];
			
			image = image ?? ri.Image ?? info.Name;
			using (var s = FileSystem.OpenWithExts(image, tileset.Extensions))
			{
				var shp = new ShpReader(s);
				var frame = shp[0];

				var bitmap = new Bitmap(shp.Width, shp.Height);
				var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				unsafe
				{
					int* q = (int*)data.Scan0.ToPointer();
					var stride = data.Stride >> 2;

					for (var i = 0; i < shp.Width; i++)
						for (var j = 0; j < shp.Height; j++)
							q[j * stride + i] = p.GetColor(frame.Image[i + shp.Width * j]).ToArgb();
				}

				bitmap.UnlockBits(data);
				return new ActorTemplate { Bitmap = bitmap, Info = info, Centered = !info.Traits.Contains<BuildingInfo>() };
			}
		}

		static ResourceTemplate RenderResourceType(ResourceTypeInfo info, string[] exts, Palette p)
		{
			var image = info.SpriteNames[0];
			using (var s = FileSystem.OpenWithExts(image, exts))
			{
				var shp = new ShpReader(s);
				var frame = shp[shp.ImageCount - 1];

				var bitmap = new Bitmap(shp.Width, shp.Height);
				var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				unsafe
				{
					int* q = (int*)data.Scan0.ToPointer();
					var stride = data.Stride >> 2;

					for (var i = 0; i < shp.Width; i++)
						for (var j = 0; j < shp.Height; j++)
							q[j * stride + i] = p.GetColor(frame.Image[i + shp.Width * j]).ToArgb();
				}

				bitmap.UnlockBits(data);
				return new ResourceTemplate { Bitmap = bitmap, Info = info, Value = shp.ImageCount - 1 };
			}
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
                    surface1.Map.Package = new Folder(loadedMapName);
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
           

                if (DialogResult.OK == nms.ShowDialog())
                {

                    if (nms.txtNew.Text == "")
                    {
                        nms.txtNew.Text = "unnamed";
                    }

                    string mapfoldername = Path.Combine(nms.MapFolderPath, nms.txtNew.Text);
                    DirectoryInfo directory = new DirectoryInfo(mapfoldername);
                    loadedMapName = mapfoldername;
                    try
                    {

                        if (directory.Exists)
                        {
                            return;
                        }
                        directory.Create();
                    }
                    catch (Exception ed)
                    {
                        Console.WriteLine("Directory creation failed: {0}", ed.ToString());
                    }
                    finally { }

                    SaveClicked(sender, e);
                }
            }


			//if (DialogResult.OK == folderBrowser.ShowDialog())
			//{
				//loadedMapName = folderBrowser.SelectedPath;
			//	SaveClicked(sender, e);
			//}
		}

		void OpenClicked(object sender, EventArgs e)
		{
			//folderBrowser.ShowNewFolderButton = true;


            using (var nms = new MapSelect())
            {
                nms.MapFolderPath = new string[] { Environment.CurrentDirectory, "mods", currentMod, "maps" }
                .Aggregate(Path.Combine);

                nms.txtNew.ReadOnly = true;
                nms.btnOk.Text = "Open";

                if (DialogResult.OK == nms.ShowDialog())
                {
                    string mapfoldername = Path.Combine(nms.MapFolderPath, nms.txtNew.Text);
                    LoadMap(mapfoldername);
                }
            }


			//if (DialogResult.OK == folderBrowser.ShowDialog())
				//LoadMap(folderBrowser.SelectedPath);
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
			using (var ofd = new OpenFileDialog { Filter = "Legacy maps (*.ini;*.mpr)|*.ini;*.mpr" })
				if (DialogResult.OK == ofd.ShowDialog())
				{
					/* massive hack: we should be able to call NewMap() with the imported Map object,
					 * but something's not right internally in it, unless loaded via the real maploader */

					var savePath = Path.Combine(Path.GetTempPath(), "OpenRA.Import");
					Directory.CreateDirectory(savePath);

					var map = LegacyMapImporter.Import(ofd.FileName);
					map.Package = new Folder(savePath);
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

			switch (MessageBox.Show("The map has been modified since it was last saved. Save changes now?",
				"Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation))
			{
				case DialogResult.Yes: SaveClicked(null, EventArgs.Empty); break;
				case DialogResult.No: break;
				case DialogResult.Cancel: e.Cancel = true; break;
			}
		}

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void surface1_Click(object sender, EventArgs e)
        {

        }

        private void layersFloaterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pb = new PaletteBox(); 
            pb.Show();
        }


        private void surface1_Click_1(object sender, EventArgs e)
        {
            var terrainBitmap = Minimap.TerrainBitmap(surface1.Map);
            pmMiniMap.Image = terrainBitmap;
        }

	}
}