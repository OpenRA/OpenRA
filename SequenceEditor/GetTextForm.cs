using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SequenceEditor
{
	public partial class GetTextForm : Form
	{
		public GetTextForm()
		{
			InitializeComponent();
		}

		public static string GetString(string title, string defaultValue)
		{
			using (var f = new GetTextForm())
			{
				f.textBox1.Text = defaultValue;
				f.Text = title;
				if (DialogResult.OK != f.ShowDialog())
					return null;
				return f.textBox1.Text;
			}
		}
	}
}
