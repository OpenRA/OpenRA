using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpenRA.TilesetBuilder
{
	public partial class frmNew : Form
	{
		public bool PaletteFromImage = true;
		public string PaletteFile = "";
		public string ImageFile = "";
		public int TileSize = 24;

		public frmNew()
		{
			InitializeComponent();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (!PaletteFromImage)
			{
				if (PaletteFile.Length < 5)
				{
					MessageBox.Show("No palette specified", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
			if (ImageFile.Length < 5)
			{
				MessageBox.Show("No image selected", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void chkUsePalFromImage_CheckedChanged(object sender, EventArgs e)
		{
			if (chkUsePalFromImage.Checked)
			{
				btnPalBrowse.Enabled = false;
				PaletteFromImage = true;
			}
			else
			{
				btnPalBrowse.Enabled = true;
				PaletteFromImage = false;
			}
		}

		private void btnPalBrowse_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog { Filter = "C&C Palette (*.pal)|*.pal" })
				if (DialogResult.OK == ofd.ShowDialog())
				{
					PaletteFile = ofd.FileName;
					txtPal.Text = PaletteFile;
				}
		}

		private void btnImgBrowse_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog { Filter = "8bit Image (*.png,*.pcx,*.bmp)|*.png;*.pcx;*.bmp" })
				if (DialogResult.OK == ofd.ShowDialog())
				{
					ImageFile = ofd.FileName;
					imgImage.Image = Image.FromFile(ImageFile);
					txtImage.Text = ImageFile;
				}
		}

		private void numSize_ValueChanged(object sender, EventArgs e)
		{
			TileSize = (int)numSize.Value;
		}
	}
}
