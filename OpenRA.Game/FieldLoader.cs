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
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
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
				else if (!TryGetValueFromYaml(kv.Key, my, out val))
					continue;

				kv.Key.SetValue(self, val);
			}
		}

		static bool TryGetValueFromYaml(FieldInfo field, MiniYaml yaml, out object ret)
		{
			ret = null;
			var n = yaml.Nodes.Where(x => x.Key == field.Name).ToList();
			if (n.Count == 0)
				return false;
			if (n.Count == 1 && n[0].Value.Nodes.Count == 0)
			{
				ret = GetValue(field.Name, field.FieldType, n[0].Value.Value, field);
				return true;
			}
			else if (n.Count > 1)
			{
				throw new InvalidOperationException("The field {0} has multiple definitions:\n{1}"
					.F(field.Name, n.Select(m => "\t- " + m.Location).JoinWith("\n")));
			}

			throw new InvalidOperationException("TryGetValueFromYaml: unable to load field {0} (of type {1})".F(field.Name, field.FieldType));
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
					field.SetValue(self, GetValue(field.Name, field.FieldType, value, field));
				return;
			}

			var prop = self.GetType().GetProperty(key.Trim());

			if (prop != null)
			{
				if (!prop.HasAttribute<FieldFromYamlKeyAttribute>())
					prop.SetValue(self, GetValue(prop.Name, prop.PropertyType, value, prop), NoIndexes);
				return;
			}

			UnknownFieldAction(key.Trim(), self.GetType());
		}

		public static T GetValue<T>(string field, string value)
		{
			return (T)GetValue(field, typeof(T), value, null);
		}

		public static object GetValue(string fieldName, Type fieldType, string value)
		{
			return GetValue(fieldName, fieldType, value, null);
		}

		public static object GetValue(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null) value = value.Trim();

			if (fieldType == typeof(int))
			{
				int res;
				if (Exts.TryParseIntegerInvariant(value, out res))
					return res;
				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(ushort))
			{
				ushort res;
				if (ushort.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out res))
					return res;
				return InvalidValueAction(value, fieldType, fieldName);
			}

			if (fieldType == typeof(long))
			{
				long res;
				if (long.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out res))
					return res;
				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(float))
			{
				float res;
				if (float.TryParse(value.Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out res))
					return res * (value.Contains('%') ? 0.01f : 1f);
				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(decimal))
			{
				decimal res;
				if (decimal.TryParse(value.Replace("%", ""),  NumberStyles.Float, NumberFormatInfo.InvariantInfo, out res))
					return res * (value.Contains('%') ? 0.01m : 1m);
				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(string))
			{
				if (field != null && field.HasAttribute<TranslateAttribute>())
					return Regex.Replace(value, "@[^@]+@", m => Translate(m.Value.Substring(1, m.Value.Length - 2)), RegexOptions.Compiled);
				return value;
			}

			else if (fieldType == typeof(Color))
			{
				var parts = value.Split(',');
				if (parts.Length == 3)
					return Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255));
				if (parts.Length == 4)
					return Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[3]).Clamp(0, 255));
				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(HSLColor))
			{
				var parts = value.Split(',');

				// Allow old ColorRamp format to be parsed as HSLColor
				if (parts.Length == 3 || parts.Length == 4)
					return new HSLColor(
						(byte)Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						(byte)Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						(byte)Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255));

				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(Hotkey))
			{
				Hotkey res;
				if (Hotkey.TryParse(value, out res))
					return res;

				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(WRange))
			{
				WRange res;
				if (WRange.TryParse(value, out res))
					return res;

				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(WVec))
			{
				var parts = value.Split(',');
				if (parts.Length == 3)
				{
					WRange rx, ry, rz;
					if (WRange.TryParse(parts[0], out rx) && WRange.TryParse(parts[1], out ry) && WRange.TryParse(parts[2], out rz))
						return new WVec(rx, ry, rz);
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(WPos))
			{
				var parts = value.Split(',');
				if (parts.Length == 3)
				{
					WRange rx, ry, rz;
					if (WRange.TryParse(parts[0], out rx) && WRange.TryParse(parts[1], out ry) && WRange.TryParse(parts[2], out rz))
						return new WPos(rx, ry, rz);
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(WAngle))
			{
				int res;
				if (Exts.TryParseIntegerInvariant(value, out res))
					return new WAngle(res);
				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(WRot))
			{
				var parts = value.Split(',');
				if (parts.Length == 3)
				{
					int rr, rp, ry;
					if (Exts.TryParseIntegerInvariant(value, out rr)
						&& Exts.TryParseIntegerInvariant(value, out rp)
						&& Exts.TryParseIntegerInvariant(value, out ry))
							return new WRot(new WAngle(rr), new WAngle(rp), new WAngle(ry));
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}

			else if (fieldType == typeof(CPos))
			{
				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new CPos(
					Exts.ParseIntegerInvariant(parts[0]),
					Exts.ParseIntegerInvariant(parts[1]));
			}

			else if (fieldType == typeof(CVec))
			{
				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new CVec(
					Exts.ParseIntegerInvariant(parts[0]),
					Exts.ParseIntegerInvariant(parts[1]));
			}

			else if (fieldType.IsEnum)
			{
				try
				{
					return Enum.Parse(fieldType, value, true);
				}
				catch (ArgumentException)
				{
					return InvalidValueAction(value, fieldType, fieldName);
				}
			}

			else if (fieldType == typeof(bool))
				return ParseYesNo(value, fieldType, fieldName);

			else if (fieldType.IsArray)
			{
				if (value == null)
					return Array.CreateInstance(fieldType.GetElementType(), 0);

				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

				var ret = Array.CreateInstance(fieldType.GetElementType(), parts.Length);
				for (int i = 0; i < parts.Length; i++)
					ret.SetValue(GetValue(fieldName, fieldType.GetElementType(), parts[i].Trim(), field), i);
				return ret;
			}

			else if (fieldType == typeof(Size))
			{
				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new Size(
					Exts.ParseIntegerInvariant(parts[0]),
					Exts.ParseIntegerInvariant(parts[1]));
			}

			else if (fieldType == typeof(int2))
			{
				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new int2(
					Exts.ParseIntegerInvariant(parts[0]),
					Exts.ParseIntegerInvariant(parts[1]));
			}

			else if (fieldType == typeof(float2))
			{
				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				float xx = 0;
				float yy = 0;
				float res;
				if (float.TryParse(parts[0].Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out res))
					xx = res * (parts[0].Contains('%') ? 0.01f : 1f);
				if (float.TryParse(parts[1].Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out res))
					yy = res * (parts[1].Contains('%') ? 0.01f : 1f);
				return new float2(xx, yy);
			}

			else if (fieldType == typeof(Rectangle))
			{
				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				return new Rectangle(
					Exts.ParseIntegerInvariant(parts[0]),
					Exts.ParseIntegerInvariant(parts[1]),
					Exts.ParseIntegerInvariant(parts[2]),
					Exts.ParseIntegerInvariant(parts[3]));
			}

			else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Bits<>))
			{
				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				var argTypes = new Type[] { typeof(string[]) };
				var argValues = new object[] { parts };
				return fieldType.GetConstructor(argTypes).Invoke(argValues);
			}

			else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				var innerType = fieldType.GetGenericArguments().First();
				var innerValue = GetValue("Nullable<T>", innerType, value, field);
				return fieldType.GetConstructor(new[] { innerType }).Invoke(new[] { innerValue });
			}

			UnknownFieldAction("[Type] {0}".F(value), fieldType);
			return null;
		}

		static object ParseYesNo(string p, Type fieldType, string field)
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
					ret[field] = (f, ft, yaml) => GetValue(f, ft, yaml.Value, field);
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

		public static string Translate(string key)
		{
			if (Translations == null || string.IsNullOrEmpty(key))
				return key;

			string value;
			if (!Translations.TryGetValue(key, out value))
				return key;

			return value;
		}

		public static Dictionary<string, string> Translations = new Dictionary<string, string>();
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class TranslateAttribute : Attribute { }

	public class FieldFromYamlKeyAttribute : Attribute { }

	// mirrors DescriptionAttribute from System.ComponentModel but we dont want to have to use that everywhere.
	public class DescAttribute : Attribute
	{
		public readonly string[] Lines;
		public DescAttribute(params string[] lines) { Lines = lines; }
	}
}
