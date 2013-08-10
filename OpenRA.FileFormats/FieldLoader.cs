#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.FileFormats
{
	public static class FieldLoader
	{
		public static Func<string, Type, string, object> InvalidValueAction = (s, t, f) =>
		{
			throw new InvalidOperationException("FieldLoader: Cannot parse `{0}` into `{1}.{2}` ".F(s, f, t));
		};

		public static Action<string, Type> UnknownFieldAction = (s, f) =>
		{
			throw new NotImplementedException("FieldLoader: Missing field `{0}` on `{1}`".F(s, f.Name));
		};

		public static void Load(object self, MiniYaml my)
		{
			var loadDict = typeLoadInfo[self.GetType()];

			foreach (var kv in loadDict)
			{
				object val;
				if (kv.Value != null)
					val = kv.Value(kv.Key.Name, kv.Key.FieldType, my);
				else if (!TryGetValueFromYaml(kv.Key.Name, kv.Key.FieldType, my, out val))
					continue;

				kv.Key.SetValue(self, val);
			}
		}

		static bool TryGetValueFromYaml(string fieldName, Type fieldType, MiniYaml yaml, out object ret)
		{
			ret = null;
			var n = yaml.Nodes.Where(x => x.Key == fieldName).ToList();
			if (n.Count == 0)
				return false;
			if (n.Count == 1 && n[0].Value.Nodes.Count == 0)
			{
				ret = GetValue(fieldName, fieldType, n[0].Value.Value);
				return true;
			}
			else if (n.Count > 1)
			{
				throw new InvalidOperationException("The field {0} has multiple definitions:\n{1}"
					.F(fieldName, n.Select(m => "\t- " + m.Location).JoinWith("\n")));
			}

			throw new InvalidOperationException("TryGetValueFromYaml: unable to load field {0} (of type {1})".F(fieldName, fieldType));
		}

		public static T Load<T>(MiniYaml y) where T : new()
		{
			var t = new T();
			Load(t, y);
			return t;
		}

		static readonly object[] NoIndexes = { };
		public static void LoadField(object self, string key, string value)
		{
			var field = self.GetType().GetField(key.Trim());

			if (field != null)
			{
				if (!field.HasAttribute<FieldFromYamlKeyAttribute>())
					field.SetValue(self, GetValue(field.Name, field.FieldType, value));
				return;
			}

			var prop = self.GetType().GetProperty(key.Trim());

			if (prop != null)
			{
				if (!prop.HasAttribute<FieldFromYamlKeyAttribute>())
					prop.SetValue(self, GetValue(prop.Name, prop.PropertyType, value), NoIndexes);
				return;
			}

			UnknownFieldAction(key.Trim(), self.GetType());
		}

		public static T GetValue<T>(string field, string value)
		{
			return (T)GetValue(field, typeof(T), value);
		}

		public static object GetValue(string field, Type fieldType, string x)
		{
			if (x != null) x = x.Trim();

			if (fieldType == typeof(int))
			{
				int res;
				if (int.TryParse(x, out res))
					return res;
				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(ushort))
			{
				ushort res;
				if (ushort.TryParse(x, out res))
					return res;
				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(float))
			{
				float res;
				if (float.TryParse(x.Replace("%", ""),  NumberStyles.Any, NumberFormatInfo.InvariantInfo, out res))
					return res * (x.Contains('%') ? 0.01f : 1f);
				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(decimal))
			{
				decimal res;
				if (decimal.TryParse(x.Replace("%", ""),  NumberStyles.Any, NumberFormatInfo.InvariantInfo, out res))
					return res * (x.Contains('%') ? 0.01m : 1m);
				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(string))
				return x;

			else if (fieldType == typeof(Color))
			{
				var parts = x.Split(',');
				if (parts.Length == 3)
					return Color.FromArgb(int.Parse(parts[0]).Clamp(0, 255), int.Parse(parts[1]).Clamp(0, 255), int.Parse(parts[2]).Clamp(0, 255));
				if (parts.Length == 4)
					return Color.FromArgb(int.Parse(parts[0]).Clamp(0, 255), int.Parse(parts[1]).Clamp(0, 255), int.Parse(parts[2]).Clamp(0, 255), int.Parse(parts[3]).Clamp(0, 255));
				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(HSLColor))
			{
				var parts = x.Split(',');

				// Allow old ColorRamp format to be parsed as HSLColor
				if (parts.Length == 3 || parts.Length == 4)
					return new HSLColor(
						(byte)int.Parse(parts[0]).Clamp(0, 255),
						(byte)int.Parse(parts[1]).Clamp(0, 255),
						(byte)int.Parse(parts[2]).Clamp(0, 255));

				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(WRange))
			{
				WRange res;
				if (WRange.TryParse(x, out res))
					return res;

				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(WVec))
			{
				var parts = x.Split(',');
				if (parts.Length == 3)
				{
					WRange rx, ry, rz;
					if (WRange.TryParse(parts[0], out rx) && WRange.TryParse(parts[1], out ry) && WRange.TryParse(parts[2], out rz))
						return new WVec(rx, ry, rz);
				}

				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(WPos))
			{
				var parts = x.Split(',');
				if (parts.Length == 3)
				{
					WRange rx, ry, rz;
					if (WRange.TryParse(parts[0], out rx) && WRange.TryParse(parts[1], out ry) && WRange.TryParse(parts[2], out rz))
						return new WPos(rx, ry, rz);
				}

				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(WAngle))
			{
				int res;
				if (int.TryParse(x, out res))
					return new WAngle(res);
				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType == typeof(WRot))
			{
				var parts = x.Split(',');
				if (parts.Length == 3)
				{
					int rr, rp, ry;
					if (int.TryParse(x, out rr) && int.TryParse(x, out rp) && int.TryParse(x, out ry))
						return new WRot(new WAngle(rr), new WAngle(rp), new WAngle(ry));
				}

				return InvalidValueAction(x, fieldType, field);
			}

			else if (fieldType.IsEnum)
			{
				if (!Enum.GetNames(fieldType).Select(a => a.ToLower()).Contains(x.ToLower()))
					return InvalidValueAction(x, fieldType, field);
				return Enum.Parse(fieldType, x, true);
			}

			else if (fieldType == typeof(bool))
				return ParseYesNo(x, fieldType, field);

			else if (fieldType.IsArray)
			{
				if (x == null)
					return Array.CreateInstance(fieldType.GetElementType(), 0);

				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

				var ret = Array.CreateInstance(fieldType.GetElementType(), parts.Length);
				for (int i = 0; i < parts.Length; i++)
					ret.SetValue(GetValue(field, fieldType.GetElementType(), parts[i].Trim()), i);
				return ret;
			}

			else if (fieldType == typeof(int2))
			{
				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new int2(int.Parse(parts[0]), int.Parse(parts[1]));
			}

			else if (fieldType == typeof(float2))
			{
				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				float xx = 0;
				float yy = 0;
				float res;
				if (float.TryParse(parts[0].Replace("%", ""), out res))
					xx = res * (parts[0].Contains('%') ? 0.01f : 1f);
				if (float.TryParse(parts[1].Replace("%", ""), out res))
					yy = res * (parts[1].Contains('%') ? 0.01f : 1f);
				return new float2(xx, yy);
			}

			else if (fieldType == typeof(Rectangle))
			{
				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new Rectangle(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
			}

			else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Bits<>))
			{
				var parts = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				var argTypes = new Type[] { typeof(string[]) };
				var argValues = new object[] { parts };
				return fieldType.GetConstructor(argTypes).Invoke(argValues);
			}

			UnknownFieldAction("[Type] {0}".F(x), fieldType);
			return null;
		}

		static object ParseYesNo(string p, System.Type fieldType, string field)
		{
			p = p.ToLowerInvariant();
			if (p == "yes") return true;
			if (p == "true") return true;
			if (p == "no") return false;
			if (p == "false") return false;
			return InvalidValueAction(p, fieldType, field);
		}

		static Cache<Type, Dictionary<FieldInfo, Func<string, Type, MiniYaml, object>>> typeLoadInfo = new Cache<Type, Dictionary<FieldInfo, Func<string, Type, MiniYaml, object>>>(GetTypeLoadInfo);

		static Dictionary<FieldInfo, Func<string, Type, MiniYaml, object>> GetTypeLoadInfo(Type type)
		{
			var ret = new Dictionary<FieldInfo, Func<string, Type, MiniYaml, object>>();

			foreach (var ff in type.GetFields())
			{
				var field = ff;
				var ignore = field.GetCustomAttributes<IgnoreAttribute>(false);
				var loadUsing = field.GetCustomAttributes<LoadUsingAttribute>(false);
				var fromYamlKey = field.GetCustomAttributes<FieldFromYamlKeyAttribute>(false);
				if (loadUsing.Length != 0)
					ret[field] = (_1, fieldType, yaml) => loadUsing[0].LoaderFunc(field)(yaml);
				else if (fromYamlKey.Length != 0)
					ret[field] = (f, ft, yaml) => GetValue(f, ft, yaml.Value);
				else if (ignore.Length == 0)
					ret[field] = null;
			}

			return ret;
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class IgnoreAttribute : Attribute { }

		[AttributeUsage(AttributeTargets.Field)]
		public class LoadUsingAttribute : Attribute
		{
			Func<MiniYaml, object> loaderFuncCache;
			public readonly string Loader;

			public LoadUsingAttribute(string loader)
			{
				Loader = loader;
			}

			internal Func<MiniYaml, object> LoaderFunc(FieldInfo field)
			{
				const BindingFlags BindingFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
				if (loaderFuncCache == null)
					loaderFuncCache = (Func<MiniYaml, object>)Delegate.CreateDelegate(typeof(Func<MiniYaml, object>), field.DeclaringType.GetMethod(Loader, BindingFlag));
				return loaderFuncCache;
			}
		}
	}

	public static class FieldSaver
	{
		public static MiniYaml Save(object o)
		{
			var nodes = new List<MiniYamlNode>();
			string root = null;

			foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				if (f.HasAttribute<FieldFromYamlKeyAttribute>())
					root = FormatValue(o, f);
				else
					nodes.Add(new MiniYamlNode(f.Name, FormatValue(o, f)));
			}

			return new MiniYaml(root, nodes);
		}

		public static MiniYaml SaveDifferences(object o, object from)
		{
			if (o.GetType() != from.GetType())
				throw new InvalidOperationException("FieldLoader: can't diff objects of different types");

			var fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Where(f => FormatValue(o, f) != FormatValue(from, f));

			return new MiniYaml(null, fields.Select(f => new MiniYamlNode(f.Name, FormatValue(o, f))).ToList());
		}

		public static MiniYamlNode SaveField(object o, string field)
		{
			return new MiniYamlNode(field, FieldSaver.FormatValue(o, o.GetType().GetField(field)));
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

			return v.ToString();
		}

		public static string FormatValue(object o, FieldInfo f)
		{
			return FormatValue(f.GetValue(o), f.FieldType);
		}
	}

	public class FieldFromYamlKeyAttribute : Attribute { }

	// mirrors DescriptionAttribute from System.ComponentModel but we dont want to have to use that everywhere.
	public class DescAttribute : Attribute
	{
		public readonly string[] Lines;
		public DescAttribute(params string[] lines) { Lines = lines; }
	}
}
