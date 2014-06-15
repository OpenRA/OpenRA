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
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenRA.Graphics;

namespace OpenRA.Editor
{
	public partial class MapSelect : Form
	{
		public string MapFolderPath;

		public bool DirectoryIsEmpty(string path)
		{
			return !Directory.GetFileSystemEntries(path).Any();
		}

		public MapSelect(string currentMod)
		{
			MapFolderPath = new string[] { Platform.SupportDir, "maps", currentMod }
				.Aggregate(Path.Combine);

			if (!Directory.Exists(MapFolderPath))
				Directory.CreateDirectory(MapFolderPath);

			InitializeComponent();
			MapIconsList.Images.Add(pictureBox1.Image);
		}

		void MapSelect_Load(object sender, EventArgs e)
		{
			MapList.Items.Clear();
			PathOutText.Text = MapFolderPath;

			if (DirectoryIsEmpty(MapFolderPath))
				return;

			foreach (var map in MapCache.FindMapsIn(MapFolderPath))
			{
				var map1 = new ListViewItem();
				map1.Tag = map;
				map1.Text = Path.GetFileNameWithoutExtension(map);
				map1.ImageIndex = 0;
				MapList.Items.Add(map1);
			}

			// hack
			if (NewText.Text != "unnamed")
				MapList.Items[0].Selected = true;
		}

		void MapList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (MapList.SelectedItems.Count == 1)
			{
				NewText.Text = MapList.SelectedItems[0].Text;
				NewText.Tag = MapList.SelectedItems[0].Tag;

				var map = new Map((string)NewText.Tag);
				TitleText.Text = map.Title;
				AuthorText.Text = map.Author;
				TheaterText.Text = map.Tileset;
				DescTxt.Text = map.Description;
				MiniMapBox.Image = null;

				try
				{
					var tileset = Program.Rules.TileSets[map.Tileset];
					MiniMapBox.Image = Minimap.RenderMapPreview(tileset, map, true);
				}
				catch (Exception ed)
				{
					Console.WriteLine("No map preview image found: {0}", ed);
				}
			}
		}

		void PathOutTextChanged(object sender, EventArgs e)
		{
			MapFolderPath = PathOutText.Text;
		}
	}
}
