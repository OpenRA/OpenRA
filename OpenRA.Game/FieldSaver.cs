#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OpenRA
{
	public static class FieldSaver
	{
		public static MiniYaml Save(object o, bool includePrivateByDefault = false)
		{
			var nodes = new List<MiniYamlNode>();
			string root = null;

			foreach (var info in FieldLoader.GetTypeLoadInfo(o.GetType(), includePrivateByDefault))
			{
				if (info.Attribute.FromYamlKey)
					root = FormatValue(o, info.Field);
				else
					nodes.Add(new MiniYamlNode(info.YamlName, FormatValue(o, info.Field)));
			}

			return new MiniYaml(root, nodes);
		}

		public static MiniYaml SaveDifferences(object o, object from, bool includePrivateByDefault = false)
		{
			if (o.GetType() != from.GetType())
				throw new InvalidOperationException("FieldLoader: can't diff objects of different types");

			var fields = FieldLoader.GetTypeLoadInfo(o.GetType(), includePrivateByDefault)
				.Where(info => FormatValue(o, info.Field) != FormatValue(from, info.Field));

			return new MiniYaml(
				null,
				fields.Select(info => new MiniYamlNode(info.YamlName, FormatValue(o, info.Field))).ToList()
			);
		}

		public static MiniYamlNode SaveField(object o, string field)
		{
			return new MiniYamlNode(field, FormatValue(o, o.GetType().GetField(field)));
		}

		public static string FormatValue(object v, Type t)
		{
			if (v == null)
				return "";

			// Color.ToString() does the wrong thing; force it to format as an array
			if (t == typeof(Color))
			{
				var c = (Color)v;
				return "{0},{1},{2},{3}".F(((int)c.A).Clamp(0, 255),
					((int)c.R).Clamp(0, 255),
					((int)c.G).Clamp(0, 255),
					((int)c.B).Clamp(0, 255));
			}

			// Don't save floats in settings.yaml using country-specific decimal separators which can be misunderstood as group seperators.
			if (t == typeof(float))
				return ((float)v).ToString(CultureInfo.InvariantCulture);

			if (t == typeof(Rectangle))
			{
				var r = (Rectangle)v;
				return "{0},{1},{2},{3}".F(r.X, r.Y, r.Width, r.Height);
			}

			if (t.IsArray)
			{
				var elems = ((Array)v).OfType<object>();
				return elems.JoinWith(",");
			}

			if (t == typeof(DateTime))
				return ((DateTime)v).ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);

			return v.ToString();
		}

		public static string FormatValue(object o, FieldInfo f)
		{
			return FormatValue(f.GetValue(o), f.FieldType);
		}
	}
}
