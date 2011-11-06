using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenRA.FileFormats;

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
			r.LostFocus += (e,_) => setter(FieldLoader.GetValue("<editor internals>", t, r.Text));
			r.Enabled = false;
			return r;
		}
	}
}
