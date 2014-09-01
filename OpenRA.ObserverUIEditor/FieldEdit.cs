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
	public partial class frmFieldEdit : Form {
		MiniYamlNode currentNode = null;

		public frmFieldEdit()
		{
			InitializeComponent();
		}

		private void cbValue_SelectedIndexChanged(object sender, EventArgs e)
		{
			grpImageSettings.Visible = false;
			grpLabelSettings.Visible = false;

			if (cbValue.Text.StartsWith("Label"))
			{
				grpLabelSettings.Visible = true;

				edCustomText.Enabled = (cbValue.Text == "Label");
				lblCustomText.Visible = edCustomText.Enabled;
			}
			else if (cbValue.Text == "Image")
			{
				grpImageSettings.Visible = true;
			}
		}

		private void reselectSprite(string name)
		{
			var q = cmbImage.Items.Cast<YamlSprite>();
			var obj = from YamlSprite c in q
					  where c.ToString() == name
					  select c;
			var item = obj.First<YamlSprite>();
			cmbImage.SelectedItem = item;
		}

		public void LoadFromNode(MiniYamlNode node)
		{
			currentNode = node;

			cbValue.SelectedItem = currentNode.Key;

			edX.Value = NodeUtils.GetIntValue(currentNode, "X");
			edY.Value = NodeUtils.GetIntValue(currentNode, "Y");
			edWidth.Value = NodeUtils.GetIntValue(currentNode, "Width");
			edHeight.Value = NodeUtils.GetIntValue(currentNode, "Height");
			
			var align = NodeUtils.GetTextValue(currentNode, "Align");
			if (align != "")
			{
				cbAlign.SelectedItem = align;
			}
			else
			{
				cbAlign.SelectedItem = "Left";
			}

			edCustomText.Text = NodeUtils.GetTextValue(currentNode, "Text");

			if (currentNode.Key == "Image")
			{
				var collection = NodeUtils.GetTextValue(currentNode, "ImageCollection");
				var imagename = NodeUtils.GetTextValue(currentNode, "ImageName");

				reselectSprite(collection + "." + imagename);
			}


			/*
			   Label@PLAYER:
							X:50
							Y:0
							Width:135
							Height:PARENT_BOTTOM
							Font:Bold
						Image@IMAGE_CASH:
							X:189
							Y:6
							Width:11
							Height:14
							ImageCollection:observericons
							ImageName:cash
			 */
		}

		public void SaveToNodes()
		{
			currentNode.Key = "" + cbValue.SelectedItem;
			
			NodeUtils.SetTextValue(currentNode, "X", "" + edX.Value);
			NodeUtils.SetTextValue(currentNode, "Y", "" + edY.Value);
			NodeUtils.SetTextValue(currentNode, "Width", "" + edWidth.Value);

			if (edHeight.Value != 0)
			{
				NodeUtils.SetTextValue(currentNode, "Height", "" + edHeight.Value);
			}
			else
			{
				NodeUtils.SetTextValue(currentNode, "Height", "PARENT_BOTTOM");
			}

			var alignnode = NodeUtils.GetValueNode(currentNode, "Align");
			var textnode = NodeUtils.GetValueNode(currentNode, "Text");
			if (currentNode.Key.StartsWith("Label"))
			{
				if (alignnode != null)
				{
					NodeUtils.SetTextValue(currentNode, "Align", "" + cbAlign.SelectedItem);
				}
				else
				{
					alignnode = new MiniYamlNode("Align", "" + cbAlign.SelectedItem);
					currentNode.Value.Nodes.Add(alignnode);
				}

				if (!currentNode.Key.StartsWith("Label@") && (textnode != null))
				{
					textnode.Value.Value = "" + edCustomText.Text;
				}
				else if (!currentNode.Key.StartsWith("Label@"))
				{
					textnode = new MiniYamlNode("Text", "" + edCustomText.Text);
					currentNode.Value.Nodes.Add(textnode);
				}
			}
			else
			{
				// not a Label

				if (alignnode != null)
				{
					currentNode.Value.Nodes.Remove(alignnode);
				}

				if (textnode != null)
				{
					currentNode.Value.Nodes.Remove(textnode);
				}
			}

			if (currentNode.Key == "Image")
			{
				YamlSprite sprite = (YamlSprite)cmbImage.SelectedItem;
				if (sprite != null)
				{
					var imagecollection = NodeUtils.GetValueNode(currentNode, "ImageCollection");
					var imagenamenode = NodeUtils.GetValueNode(currentNode, "ImageName");

					if (imagenamenode != null)
					{
						NodeUtils.SetTextValue(currentNode, "ImageName", sprite.Name);
					}
					else
					{
						imagenamenode = new MiniYamlNode("ImageName", sprite.Name);
						currentNode.Value.Nodes.Add(imagenamenode);
					}

					if (imagecollection != null)
					{
						NodeUtils.SetTextValue(currentNode, "ImageCollection", sprite.Collection);
					}
					else
					{
						imagecollection = new MiniYamlNode("ImageCollection", sprite.Collection);
						currentNode.Value.Nodes.Add(imagecollection);
					}
				}
			}
			else
			{
				var imagecollection = NodeUtils.GetValueNode(currentNode, "ImageCollection");
				var imagenamenode = NodeUtils.GetValueNode(currentNode, "ImageName");

				currentNode.Value.Nodes.Remove(imagecollection);
				currentNode.Value.Nodes.Remove(imagenamenode);
			}
		}

		public void InitYamlImageOptions(string currentMod)
		{
			List<YamlSprite> options = new List<YamlSprite>();

			var chromeyaml = MiniYaml.FromFile(".\\mods\\" + currentMod + "\\chrome.yaml");
			foreach (var collectionnode in chromeyaml)
			{
				foreach (var spritenode in collectionnode.Value.Nodes)
				{
					if (spritenode.Value.Value.EndsWith(",16,16") || (collectionnode.Key == "observericons"))
					{
						var option = new YamlSprite();
						option.Collection = collectionnode.Key;
						option.Bitmap = collectionnode.Value.Value;
						option.Name = spritenode.Key;
						option.RectStr = spritenode.Value.Value;
						options.Add(option);
					}
				}
			}

			cmbImage.Items.Clear();
			cmbImage.Items.AddRange(options.ToArray<YamlSprite>());
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
		}

		private void cbAlign_SelectedIndexChanged(object sender, EventArgs e)
		{

		}
	}
}
