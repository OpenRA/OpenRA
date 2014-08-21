#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenRA.Editor
{
	public partial class ActorPropertiesDialog : Form
	{
		public ActorPropertiesDialog()
		{
			InitializeComponent();
		}

		public void AddRow(string name, Control c)
		{
			flowLayoutPanel1.Controls.Add(new Label
			{
				Text = name,
				Width = flowLayoutPanel1.Width * 3 / 10,
				Height = 25,
				TextAlign = ContentAlignment.MiddleLeft,
			});

			c.Width = flowLayoutPanel1.Width * 6 / 10 - 25;
			c.Height = 25;
			flowLayoutPanel1.Controls.Add(c);
		}

		public Control MakeEditorControl(Type t, Func<object> getter, Action<object> setter)
		{
			var r = new TextBox();
			r.Text = FieldSaver.FormatValue(getter(), t);
			r.LostFocus += (e, _) => setter(FieldLoader.GetValue("<editor internals>", t, r.Text));
			r.Enabled = false;
			return r;
		}
	}
}
