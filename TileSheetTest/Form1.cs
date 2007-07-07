using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenRa.FileFormats;
using System.IO;

namespace TileSheetTest
{
	public partial class Form1 : Form
	{
		static readonly Size pageSize = new Size(256,256);
		const int sheetBorder = 4;

		public Form1()
		{
			InitializeComponent();

			Package package = new Package("../../../snow.mix");
			Palette palette = new Palette(File.OpenRead("../../../snow.pal"));
			TileSet tileSet = new TileSet(package, ".sno", palette);

			List<Bitmap> sheets = new List<Bitmap>();

			Provider<Bitmap> sheetProvider = delegate
			{
				Bitmap b = new Bitmap(pageSize.Width, pageSize.Height);

				using (Graphics g = Graphics.FromImage(b))
					g.FillRectangle(Brushes.Violet, 0, 0, pageSize.Width, pageSize.Height);

				sheets.Add(b);
				return b;
			};

			TileSheetBuilder<Bitmap> builder = 
				new TileSheetBuilder<Bitmap>(pageSize, sheetProvider);

			foreach (Terrain t in tileSet.tiles.Values)
				for (int i = 0; i < t.NumTiles; i++)
				{
					Bitmap tileImage = t.GetTile(i);

					if (tileImage == null)
						continue;

					SheetRectangle<Bitmap> item = builder.AddImage(tileImage.Size);

					using (Graphics g = Graphics.FromImage(item.sheet))
					{
						g.DrawImage(tileImage, item.origin);
					}

					if (!sheets.Contains(item.sheet))
						sheets.Add(item.sheet);
				}

			foreach (Bitmap b in sheets)
			{
				PictureBox box = new PictureBox();
				box.Image = b;
				box.SizeMode = PictureBoxSizeMode.CenterImage;
				box.Size = new Size(2 * sheetBorder + b.Width, 2 * sheetBorder + b.Height);

				flowLayoutPanel1.Controls.Add(box);
			}
		}
	}
}