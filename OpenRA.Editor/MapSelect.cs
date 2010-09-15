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

namespace OpenRA.Editor
{
    public partial class MapSelect : Form
    {
        public MapSelect()
        {
            InitializeComponent();
        }

        private void MapSelect_Load(object sender, EventArgs e)
        {

            DirectoryInfo directory = new DirectoryInfo(txtPath.Text);
            DirectoryInfo[] directories = directory.GetDirectories();
            MapList.Items.Clear();
            foreach (DirectoryInfo subDirectory in directories)
            {
                ListViewItem map1 = new ListViewItem(subDirectory.Name);
                map1.ImageIndex = 0;
                MapList.Items.Add(map1);
                var map = new Map(new Folder(txtPath.Text + "\\" + subDirectory.Name));
                map1.SubItems.Add(map.Theater);
            }
            MapList.Items[0].Selected = true;

        }

        private void MapList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MapList.SelectedItems.Count > 0)
            {
                txtNew.Text = MapList.SelectedItems[0].Text;
            }
        }
    }
}
