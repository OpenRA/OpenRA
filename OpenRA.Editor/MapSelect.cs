using System;
using System.IO;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Editor
{
    public partial class MapSelect : Form
    {
        public string MapFolderPath;

        public MapSelect()
        {
            InitializeComponent();
			MapIconsList.Images.Add(pictureBox1.Image);
        }

        void MapSelect_Load(object sender, EventArgs e)
        {
            MapList.Items.Clear();
            txtPathOut.Text = MapFolderPath;
			
            foreach (var map in ModData.FindMapsIn(MapFolderPath))
            {
                ListViewItem map1 = new ListViewItem();
				map1.Tag = map;
				map1.Text = Path.GetFileNameWithoutExtension(map);
                map1.ImageIndex = 0;
                MapList.Items.Add(map1);
            }
            
			// hack
			if (txtNew.Text != "unnamed")
                MapList.Items[0].Selected = true;
        }

        void MapList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MapList.SelectedItems.Count == 1)
            {
                txtNew.Text = MapList.SelectedItems[0].Text;
				txtNew.Tag = MapList.SelectedItems[0].Tag;
				System.Console.WriteLine(MapList.SelectedItems[0]);
                var map = new Map(txtNew.Tag as string);
                txtTitle.Text = map.Title;
                txtAuthor.Text = map.Author;
                txtTheater.Text = map.Tileset;
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

        void txtPathOut_TextChanged(object sender, EventArgs e)
        {
            MapFolderPath = txtPathOut.Text;
        }

    }
}
