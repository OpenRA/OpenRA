using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using System.Windows.Forms;
using System.IO;
using OpenRA.Graphics;

namespace OpenRA.Editor
{
    public partial class MapSelect : Form
    {
        public string MapFolderPath;

        public MapSelect()
        {
            InitializeComponent();
        }

        private void MapSelect_Load(object sender, EventArgs e)
        {

            DirectoryInfo directory = new DirectoryInfo(MapFolderPath);
            DirectoryInfo[] directories = directory.GetDirectories();
            MapList.Items.Clear();
            txtPathOut.Text = MapFolderPath;
            foreach (DirectoryInfo subDirectory in directories)
            {
                ListViewItem map1 = new ListViewItem(subDirectory.Name);
                map1.ImageIndex = 0;
                MapList.Items.Add(map1);

            }
            if (txtNew.Text == "unnamed")
            {
                //dumb indian code
            }
            else
            {
                MapList.Items[0].Selected = true;
            }

        }

        private void MapList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MapList.SelectedItems.Count == 1)
            {
                txtNew.Text = MapList.SelectedItems[0].Text;
                var map = new Map(new Folder(Path.Combine(MapFolderPath, MapList.SelectedItems[0].Text)));
                txtTitle.Text = map.Title;
                txtAuthor.Text = map.Author;
                txtTheater.Text = map.Theater;
                txtDesc.Text = map.Description;
				pbMinimap.Image = null;
                try
                {
					pbMinimap.Image = Minimap.AddStaticResources(map, Minimap.TerrainBitmap(map, true));
                }
                catch (Exception ed)
                {
                    Console.WriteLine("No map preview image found: {0}", ed.ToString());
                }
                finally { }
            }
        }

        private void txtPathOut_TextChanged(object sender, EventArgs e)
        {
            MapFolderPath = txtPathOut.Text;
        }

    }
}
