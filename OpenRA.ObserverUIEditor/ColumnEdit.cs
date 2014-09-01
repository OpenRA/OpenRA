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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenRA.FileFormats;

namespace OpenRA.ObserverUIEditor {
	public partial class ColumnEdit : Form {
		protected MiniYamlNode headerNode;
		protected MiniYamlNode rowNode;

		public ColumnEdit()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		public void LoadFromNode(MiniYamlNode headerNode, MiniYamlNode rowNode)
		{
			this.headerNode = headerNode;
			this.rowNode = rowNode;

			LoadValues();
		}

		protected void LoadValues()
		{
			edTitle.Text = NodeUtils.GetTextValue(this.headerNode, "Text");
			edWidth.Value = NodeUtils.GetIntValue(this.headerNode, "Width");

			edCustomText.Text = NodeUtils.GetTextValue(this.rowNode, "Text");

			cbValue.SelectedItem = this.rowNode.Key;
			cbAlign.SelectedItem = NodeUtils.GetTextValue(this.headerNode, "Align");

			chkNoBackground.Checked = (NodeUtils.GetTextValue(this.rowNode, "BackgroundColor") == "");
		}

		public void SaveToNodes()
		{
			if (cbValue.SelectedText.StartsWith("Image"))
			{
				this.rowNode.Key = "" + cbValue.SelectedItem;


				return;
			}

			// labels

			this.rowNode.Key = "" + cbValue.SelectedItem;

			var node = NodeUtils.GetValueNode(this.headerNode, "Text");
			if (node != null)
			{
				node.Value.Value = "" + edTitle.Text;
			}
			else
			{
				node = new MiniYamlNode("Text", "" + edTitle.Text);
				this.headerNode.Value.Nodes.Add(node);
			}


			node = NodeUtils.GetValueNode(this.headerNode, "Width");
			if (node != null)
			{
				node.Value.Value = "" + edWidth.Value;
			}
			else
			{
				node = new MiniYamlNode("Width", "" + edWidth.Value);
				this.headerNode.Value.Nodes.Add(node);
			}

			node = NodeUtils.GetValueNode(this.rowNode, "Text");
			if (node != null)
			{
				node.Value.Value = "" + edCustomText.Text;
			}
			else if (edCustomText.Text != "")
			{
				node = new MiniYamlNode("Text", edCustomText.Text);
				this.rowNode.Value.Nodes.Add(node);
			}
			
			node = NodeUtils.GetValueNode(this.headerNode, "Align");
			if (node != null)
			{
				node.Value.Value = "" + cbAlign.SelectedItem;
			}
			else
			{
				node = new MiniYamlNode("Align", "" + cbAlign.SelectedItem);
				this.headerNode.Value.Nodes.Add(node);
			}

			node = NodeUtils.GetValueNode(this.rowNode, "Align");
			if (node != null)
			{
				node.Value.Value = "" + cbAlign.SelectedItem;
			}
			else
			{
				node = new MiniYamlNode("Align", "" + cbAlign.SelectedItem);
				this.rowNode.Value.Nodes.Add(node);
			}

			node = NodeUtils.GetValueNode(this.rowNode, "BackgroundColor");
			if (chkNoBackground.Checked && (node != null))
			{
				this.rowNode.Value.Nodes.Remove(node);
			}
			else if (!chkNoBackground.Checked && (node ==  null))
			{
				node = new MiniYamlNode("BackgroundColor", "255,0,0,0");
				this.rowNode.Value.Nodes.Add(node);
			}
		}
	}
}
