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
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenRA.Graphics;
using OpenRA.Input;
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
			var loadInfo = typeLoadInfo[self.GetType()];

			Dictionary<string, MiniYaml> md = null;

			foreach (var fli in loadInfo)
			{
				object val;

				if (fli.Loader != null)
					val = fli.Loader(my);
				else
				{
					if (md == null)
						md = my.ToDictionary();

					if (!TryGetValueFromYaml(fli.YamlName, fli.Field, md, out val))
						continue;
				}

				fli.Field.SetValue(self, val);
			}
		}

		static bool TryGetValueFromYaml(string yamlName, FieldInfo field, Dictionary<string, MiniYaml> md, out object ret)
		{
			ret = null;

			MiniYaml yaml;
			if (!md.TryGetValue(yamlName, out yaml))
				return false;

			if (yaml.Nodes.Count == 0)
			{
				ret = GetValue(field.Name, field.FieldType, yaml.Value, field);
				return true;
			}

			throw new InvalidOperationException("TryGetValueFromYaml: unable to load field {0} (of type {1})".F(yamlName, field.FieldType));
		}

		public static T Load<T>(MiniYaml y) where T : new()
		{
			var t = new T();
			Load(t, y);
			return t;
		}

		static readonly object[] NoIndexes = { };
		public static void LoadField(object target, string key, string value)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			key = key.Trim();

			var field = target.GetType().GetField(key, flags);
			if (field != null)
			{
				var sa = field.GetCustomAttributes<SerializeAttribute>(false).DefaultIfEmpty(SerializeAttribute.Default).First();
				if (!sa.FromYamlKey)
					field.SetValue(target, GetValue(field.Name, field.FieldType, value, field));
				return;
			}

			var prop = target.GetType().GetProperty(key, flags);
			if (prop != null)
			{
				var sa = prop.GetCustomAttributes<SerializeAttribute>(false).DefaultIfEmpty(SerializeAttribute.Default).First();
				if (!sa.FromYamlKey)
					prop.SetValue(target, GetValue(prop.Name, prop.PropertyType, value, prop), NoIndexes);
				return;
			}

			UnknownFieldAction(key, target.GetType());
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

			else if (fieldType == typeof(Color[]))
			{
				var parts = value.Split(',');

				if (parts.Length % 4 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var colors = new Color[parts.Length / 4];

				for (var i = 0; i < colors.Length; i++)
				{
					colors[i] = Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[4 * i]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 2]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 3]).Clamp(0, 255));
				}

				return colors;
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

			else if (fieldType == typeof(WVec[]))
			{
				var parts = value.Split(',');

				if (parts.Length % 3 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var vecs = new WVec[parts.Length / 3];

				for (var i = 0; i < vecs.Length; ++i)
				{
					WRange rx, ry, rz;
					if (WRange.TryParse(parts[3 * i], out rx)
							&& WRange.TryParse(parts[3 * i + 1], out ry)
							&& WRange.TryParse(parts[3 * i + 2], out rz))
						vecs[i] = new WVec(rx, ry, rz);
				}

				return vecs;
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
				for (var i = 0; i < parts.Length; i++)
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

			else if (fieldType == typeof(DateTime))
			{
				DateTime dt;
				if (DateTime.TryParseExact(value, "yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt))
					return dt;
				return InvalidValueAction(value, fieldType, fieldName);
			}

			else
			{
				var conv = TypeDescriptor.GetConverter(fieldType);
				if (conv.CanConvertFrom(typeof(string)))
				{
					try
					{
						return conv.ConvertFromInvariantString(value);
					}
					catch
					{
						return InvalidValueAction(value, fieldType, fieldName);
					}
				}
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

		public sealed class FieldLoadInfo
		{
			public readonly FieldInfo Field;
			public readonly SerializeAttribute Attribute;
			public readonly string YamlName;
			public readonly Func<MiniYaml, object> Loader;

			internal FieldLoadInfo(FieldInfo field, SerializeAttribute attr, string yamlName, Func<MiniYaml, object> loader = null)
			{
				Field = field;
				Attribute = attr;
				YamlName = yamlName;
				Loader = loader;
			}
		}

		public static IEnumerable<FieldLoadInfo> GetTypeLoadInfo(Type type, bool includePrivateByDefault = false)
		{
			return typeLoadInfo[type].Where(fli => includePrivateByDefault || fli.Field.IsPublic || (fli.Attribute.Serialize && !fli.Attribute.IsDefault));
		}

		static Cache<Type, List<FieldLoadInfo>> typeLoadInfo = new Cache<Type, List<FieldLoadInfo>>(BuildTypeLoadInfo);

		static List<FieldLoadInfo> BuildTypeLoadInfo(Type type)
		{
			var ret = new List<FieldLoadInfo>();

			foreach (var ff in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var field = ff;

				var sa = field.GetCustomAttributes<SerializeAttribute>(false).DefaultIfEmpty(SerializeAttribute.Default).First();
				if (!sa.Serialize)
					continue;

				var yamlName = string.IsNullOrEmpty(sa.YamlName) ? field.Name : sa.YamlName;

				var loader = sa.GetLoader(type);
				if (loader == null && sa.FromYamlKey)
					loader = (yaml) => GetValue(yamlName, field.FieldType, yaml.Value, field);

				var fli = new FieldLoadInfo(field, sa, yamlName, loader);
				ret.Add(fli);
			}

			return ret;
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class IgnoreAttribute : SerializeAttribute
		{
			public IgnoreAttribute()
				: base(false) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class LoadUsingAttribute : SerializeAttribute
		{
			public LoadUsingAttribute(string loader)
			{
				Loader = loader;
			}
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class SerializeAttribute : Attribute
		{
			public static readonly SerializeAttribute Default = new SerializeAttribute(true);

			public bool IsDefault { get { return this == Default; } }

			public readonly bool Serialize;
			public string YamlName;
			public string Loader;
			public bool FromYamlKey;

			public SerializeAttribute(bool serialize = true)
			{
				Serialize = serialize;
			}

			internal Func<MiniYaml, object> GetLoader(Type type)
			{
				const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

				if (!string.IsNullOrEmpty(Loader))
				{
					var method = type.GetMethod(Loader, flags);
					if (method == null)
						throw new InvalidOperationException("{0} does not specify a loader function '{1}'".F(type.Name, Loader));

					return (Func<MiniYaml, object>)Delegate.CreateDelegate(typeof(Func<MiniYaml, object>), method);
				}

				return null;
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
	public sealed class TranslateAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class FieldFromYamlKeyAttribute : FieldLoader.SerializeAttribute
	{
		public FieldFromYamlKeyAttribute()
		{
			FromYamlKey = true;
		}
	}

	// mirrors DescriptionAttribute from System.ComponentModel but we dont want to have to use that everywhere.
	public sealed class DescAttribute : Attribute
	{
		public readonly string[] Lines;
		public DescAttribute(params string[] lines) { Lines = lines; }
	}
}
