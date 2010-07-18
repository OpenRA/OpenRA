#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

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
