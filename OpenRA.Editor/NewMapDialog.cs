#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Windows.Forms;

namespace OpenRA.Editor
{
	public partial class NewMapDialog : Form
	{
		public NewMapDialog()
		{
			InitializeComponent();
		}

		void SelectText(object sender, System.EventArgs e)
		{
			var ud = sender as NumericUpDown;
			ud.Select(0, ud.ToString().Length);
		}
	}
}
