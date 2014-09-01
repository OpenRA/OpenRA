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
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

namespace OpenRA.ObserverUIEditor
{
	public static class NodeUtils
	{
		public static MiniYamlNode GetValueNode(MiniYamlNode searchnode, string keyname)
		{
			foreach (var node in searchnode.Value.Nodes)
			{
				if (node.Key == keyname)
				{
					return node;
				}
			}

			return null;
		}

		public static void DeleteValueNode(MiniYamlNode searchnode, string keyname)
		{
			foreach (var node in searchnode.Value.Nodes)
			{
				if (node.Key == keyname)
				{
					searchnode.Value.Nodes.Remove(node);
					return;
				}
			}
		}

		public static void SetTextValue(MiniYamlNode searchnode, string keyname, string textvalue)
		{
			var node = NodeUtils.GetValueNode(searchnode, keyname);
			if (node == null)
			{
				node = new MiniYamlNode(keyname, textvalue);
				searchnode.Value.Nodes.Add(node);
			}

			node.Value.Value = textvalue;
		}

		public static string GetTextValue(MiniYamlNode searchnode, string keyname)
		{
			var node = GetValueNode(searchnode, keyname);
			if (node != null)
			{
				return node.Value.Value;
			}

			return "";
		}

		public static int GetIntValue(MiniYamlNode searchnode, string keyname)
		{
			int r = 0;

			var node = GetValueNode(searchnode, keyname);
			if (node != null)
			{
				Int32.TryParse(node.Value.Value, out r);
			}

			return r;
		}
	}
}
