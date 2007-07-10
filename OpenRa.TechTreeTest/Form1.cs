using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OpenRa.TechTreeTest
{
	public partial class Form1 : Form
	{
		TechTree techTree = new TechTree(Race.Allies);

		public Form1()
		{
			InitializeComponent();
			RefreshList();
		}

		void RefreshList()
		{
			buildableItems.Controls.Clear();

			foreach (Item b in techTree.BuildableItems)
			{
				PictureBox box = new PictureBox();
				box.SizeMode = PictureBoxSizeMode.AutoSize;
				box.Image = b.Icon;

				toolTip1.SetToolTip(box, b.Tooltip);

				buildableItems.Controls.Add(box);

				Item k = b;

				box.Click += delegate { Build(k); };
			}
		}

		void Build(Item b)
		{
			techTree.Build(b.tag);
			RefreshList();
		}
	}
}