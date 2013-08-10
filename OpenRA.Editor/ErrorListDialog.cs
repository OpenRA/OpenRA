﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Windows.Forms;

namespace OpenRA.Editor
{
	public partial class ErrorListDialog : Form
	{
		public ErrorListDialog(IEnumerable<string> errors)
		{
			InitializeComponent();
			foreach (var e in errors)
				listBox1.Items.Add(e);
		}
	}
}
