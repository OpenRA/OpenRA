#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
{
	public static class FieldLoader
	{
		[Serializable]
		public class MissingFieldsException : YamlException
		{
			public readonly string[] Missing;
			public readonly string Header;
			public override string Message
			{
				get
				{
					return (string.IsNullOrEmpty(Header) ? "" : Header + ": ") + Missing[0]
						+ string.Concat(Missing.Skip(1).Select(m => ", " + m));
				}
			}

			public MissingFieldsException(string[] missing, string header = null, string headerSingle = null) : base(null)
			{
				Header = missing.Length > 1 ? header : headerSingle ?? header;
				Missing = missing;
			}

			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);
				info.AddValue("Missing", Missing);
				info.AddValue("Header", Header);
			}
		}

		public static Func<string, Type, string, object> InvalidValueAction = (s, t, f) =>
		{
			throw new YamlException("FieldLoader: Cannot parse `{0}` into `{1}.{2}` ".F(s, f, t));
		};

		public static Action<string, Type> UnknownFieldAction = (s, f) =>
		{
			throw new NotImplementedException("FieldLoader: Missing field `{0}` on `{1}`".F(s, f.Name));
		};

		static readonly ConcurrentCache<Type, FieldLoadInfo[]> TypeLoadInfo =
			new ConcurrentCache<Type, FieldLoadInfo[]>(BuildTypeLoadInfo);
		static readonly ConcurrentCache<MemberInfo, bool> MemberHasTranslateAttribute =
			new ConcurrentCache<MemberInfo, bool>(member => member.HasAttribute<TranslateAttribute>());

		static readonly object TranslationsLock = new object();
		static Dictionary<string, string> translations;

		public static void Load(object self, MiniYaml my)
		{
			var loadInfo = TypeLoadInfo[self.GetType()];
			var missing = new List<string>();

			Dictionary<string, MiniYaml> md = null;

			foreach (var fli in loadInfo)
			{
				object val;

				if (md == null)
					md = my.ToDictionary();
				if (fli.Loader != null)
				{
					if (!fli.Attribute.Required || md.ContainsKey(fli.YamlName))
						val = fli.Loader(my);
					else
					{
						missing.Add(fli.YamlName);
						continue;
					}
				}
				else
				{
					if (!TryGetValueFromYaml(fli.YamlName, fli.Field, md, out val))
					{
						if (fli.Attribute.Required)
							missing.Add(fli.YamlName);
						continue;
					}
				}

				fli.Field.SetValue(self, val);
			}

			if (missing.Any())
				throw new MissingFieldsException(missing.ToArray());
		}

		static bool TryGetValueFromYaml(string yamlName, FieldInfo field, Dictionary<string, MiniYaml> md, out object ret)
		{
			ret = null;

			MiniYaml yaml;
			if (!md.TryGetValue(yamlName, out yaml))
				return false;

			ret = GetValue(field.Name, field.FieldType, yaml, field);
			return true;
		}

		public static T Load<T>(MiniYaml y) where T : new()
		{
			var t = new T();
			Load(t, y);
			return t;
		}

		public static void LoadField(object target, string key, string value)
		{
			const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			key = key.Trim();

			var field = target.GetType().GetField(key, Flags);
			if (field != null)
			{
				var sa = field.GetCustomAttributes<SerializeAttribute>(false).DefaultIfEmpty(SerializeAttribute.Default).First();
				if (!sa.FromYamlKey)
					field.SetValue(target, GetValue(field.Name, field.FieldType, value, field));
				return;
			}

			var prop = target.GetType().GetProperty(key, Flags);
			if (prop != null)
			{
				var sa = prop.GetCustomAttributes<SerializeAttribute>(false).DefaultIfEmpty(SerializeAttribute.Default).First();
				if (!sa.FromYamlKey)
					prop.SetValue(target, GetValue(prop.Name, prop.PropertyType, value, prop), null);
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
			return GetValue(fieldName, fieldType, new MiniYaml(value), field);
		}

		public static object GetValue(string fieldName, Type fieldType, MiniYaml yaml, MemberInfo field)
		{
			var value = yaml.Value;
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
				if (value != null && float.TryParse(value.Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out res))
					return res * (value.Contains('%') ? 0.01f : 1f);
				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(decimal))
			{
				decimal res;
				if (value != null && decimal.TryParse(value.Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out res))
					return res * (value.Contains('%') ? 0.01m : 1m);
				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(string))
			{
				if (field != null && MemberHasTranslateAttribute[field] && value != null)
					return Regex.Replace(value, "@[^@]+@", m => Translate(m.Value.Substring(1, m.Value.Length - 2)), RegexOptions.Compiled);
				return value;
			}
			else if (fieldType == typeof(Color))
			{
				Color color;
				if (value != null && HSLColor.TryParseRGB(value, out color))
					return color;

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(Color[]))
			{
				if (value != null)
				{
					var parts = value.Split(',');
					var colors = new Color[parts.Length];

					for (var i = 0; i < colors.Length; i++)
						if (!HSLColor.TryParseRGB(parts[i], out colors[i]))
							return InvalidValueAction(value, fieldType, fieldName);

					return colors;
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(HSLColor))
			{
				if (value != null)
				{
					Color rgb;
					if (HSLColor.TryParseRGB(value, out rgb))
						return new HSLColor(rgb);

					// Allow old HSLColor/ColorRamp formats to be parsed as HSLColor
					var parts = value.Split(',');
					if (parts.Length == 3 || parts.Length == 4)
						return new HSLColor(
							(byte)Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
							(byte)Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
							(byte)Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255));
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(Hotkey))
			{
				Hotkey res;
				if (Hotkey.TryParse(value, out res))
					return res;

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(WDist))
			{
				WDist res;
				if (WDist.TryParse(value, out res))
					return res;

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(WVec))
			{
				if (value != null)
				{
					var parts = value.Split(',');
					if (parts.Length == 3)
					{
						WDist rx, ry, rz;
						if (WDist.TryParse(parts[0], out rx) && WDist.TryParse(parts[1], out ry) && WDist.TryParse(parts[2], out rz))
							return new WVec(rx, ry, rz);
					}
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(WVec[]))
			{
				if (value != null)
				{
					var parts = value.Split(',');

					if (parts.Length % 3 != 0)
						return InvalidValueAction(value, fieldType, fieldName);

					var vecs = new WVec[parts.Length / 3];

					for (var i = 0; i < vecs.Length; ++i)
					{
						WDist rx, ry, rz;
						if (WDist.TryParse(parts[3 * i], out rx) && WDist.TryParse(parts[3 * i + 1], out ry) && WDist.TryParse(parts[3 * i + 2], out rz))
							vecs[i] = new WVec(rx, ry, rz);
					}

					return vecs;
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(WPos))
			{
				if (value != null)
				{
					var parts = value.Split(',');
					if (parts.Length == 3)
					{
						WDist rx, ry, rz;
						if (WDist.TryParse(parts[0], out rx) && WDist.TryParse(parts[1], out ry) && WDist.TryParse(parts[2], out rz))
							return new WPos(rx, ry, rz);
					}
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
				if (value != null)
				{
					var parts = value.Split(',');
					if (parts.Length == 3)
					{
						int rr, rp, ry;
						if (Exts.TryParseIntegerInvariant(value, out rr) && Exts.TryParseIntegerInvariant(value, out rp) && Exts.TryParseIntegerInvariant(value, out ry))
							return new WRot(new WAngle(rr), new WAngle(rp), new WAngle(ry));
					}
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(CPos))
			{
				if (value != null)
				{
					var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					return new CPos(Exts.ParseIntegerInvariant(parts[0]), Exts.ParseIntegerInvariant(parts[1]));
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(CVec))
			{
				if (value != null)
				{
					var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					return new CVec(Exts.ParseIntegerInvariant(parts[0]), Exts.ParseIntegerInvariant(parts[1]));
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(CVec[]))
			{
				if (value != null)
				{
					var parts = value.Split(',');

					if (parts.Length % 2 != 0)
						return InvalidValueAction(value, fieldType, fieldName);

					var vecs = new CVec[parts.Length / 2];
					for (var i = 0; i < vecs.Length; i++)
					{
						int rx, ry;
						if (int.TryParse(parts[2 * i], out rx) && int.TryParse(parts[2 * i + 1], out ry))
							vecs[i] = new CVec(rx, ry);
					}

					return vecs;
				}

				return InvalidValueAction(value, fieldType, fieldName);
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
			else if (fieldType == typeof(ImageFormat))
			{
				if (value != null)
				{
					switch (value.ToLowerInvariant())
					{
					case "bmp":
						return ImageFormat.Bmp;
					case "gif":
						return ImageFormat.Gif;
					case "jpg":
					case "jpeg":
						return ImageFormat.Jpeg;
					case "tif":
					case "tiff":
						return ImageFormat.Tiff;
					default:
						return ImageFormat.Png;
					}
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(bool))
				return ParseYesNo(value, fieldType, fieldName);
			else if (fieldType.IsArray && fieldType.GetArrayRank() == 1)
			{
				if (value == null)
					return Array.CreateInstance(fieldType.GetElementType(), 0);

				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

				var ret = Array.CreateInstance(fieldType.GetElementType(), parts.Length);
				for (var i = 0; i < parts.Length; i++)
					ret.SetValue(GetValue(fieldName, fieldType.GetElementType(), parts[i].Trim(), field), i);
				return ret;
			}
			else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(HashSet<>))
			{
				var set = Activator.CreateInstance(fieldType);
				if (value == null)
					return set;

				var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				var addMethod = fieldType.GetMethod("Add", fieldType.GetGenericArguments());
				for (var i = 0; i < parts.Length; i++)
					addMethod.Invoke(set, new[] { GetValue(fieldName, fieldType.GetGenericArguments()[0], parts[i].Trim(), field) });
				return set;
			}
			else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				var dict = Activator.CreateInstance(fieldType);
				var arguments = fieldType.GetGenericArguments();
				var addMethod = fieldType.GetMethod("Add", arguments);

				foreach (var node in yaml.Nodes)
				{
					var key = GetValue(fieldName, arguments[0], node.Key, field);
					var val = GetValue(fieldName, arguments[1], node.Value, field);
					addMethod.Invoke(dict, new[] { key, val });
				}

				return dict;
			}
			else if (fieldType == typeof(Size))
			{
				if (value != null)
				{
					var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					return new Size(Exts.ParseIntegerInvariant(parts[0]), Exts.ParseIntegerInvariant(parts[1]));
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(int2))
			{
				if (value != null)
				{
					var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					return new int2(Exts.ParseIntegerInvariant(parts[0]), Exts.ParseIntegerInvariant(parts[1]));
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(float2))
			{
				if (value != null)
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

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(float3))
			{
				if (value != null)
				{
					var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					float x = 0;
					float y = 0;
					float z = 0;
					float.TryParse(parts[0], NumberStyles.Float, NumberFormatInfo.InvariantInfo, out x);
					float.TryParse(parts[1], NumberStyles.Float, NumberFormatInfo.InvariantInfo, out y);

					// z component is optional for compatibility with older float2 definitions
					if (parts.Length > 2)
						float.TryParse(parts[2], NumberStyles.Float, NumberFormatInfo.InvariantInfo, out z);

					return new float3(x, y, z);
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType == typeof(Rectangle))
			{
				if (value != null)
				{
					var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					return new Rectangle(
						Exts.ParseIntegerInvariant(parts[0]),
						Exts.ParseIntegerInvariant(parts[1]),
						Exts.ParseIntegerInvariant(parts[2]),
						Exts.ParseIntegerInvariant(parts[3]));
				}

				return InvalidValueAction(value, fieldType, fieldName);
			}
			else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Bits<>))
			{
				if (value != null)
				{
					var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					var argTypes = new Type[] { typeof(string[]) };
					var argValues = new object[] { parts };
					return fieldType.GetConstructor(argTypes).Invoke(argValues);
				}

				return InvalidValueAction(value, fieldType, fieldName);
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
			if (string.IsNullOrEmpty(p))
				return InvalidValueAction(p, fieldType, field);

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
			return TypeLoadInfo[type].Where(fli => includePrivateByDefault || fli.Field.IsPublic || (fli.Attribute.Serialize && !fli.Attribute.IsDefault));
		}

		static FieldLoadInfo[] BuildTypeLoadInfo(Type type)
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
					loader = yaml => GetValue(yamlName, field.FieldType, yaml, field);

				var fli = new FieldLoadInfo(field, sa, yamlName, loader);
				ret.Add(fli);
			}

			return ret.ToArray();
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class IgnoreAttribute : SerializeAttribute
		{
			public IgnoreAttribute()
				: base(false) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class RequireAttribute : SerializeAttribute
		{
			public RequireAttribute()
				: base(true, true) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class LoadUsingAttribute : SerializeAttribute
		{
			public LoadUsingAttribute(string loader, bool required = false)
			{
				Loader = loader;
				Required = required;
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
			public bool DictionaryFromYamlKey;
			public bool Required;

			public SerializeAttribute(bool serialize = true, bool required = false)
			{
				Serialize = serialize;
				Required = required;
			}

			internal Func<MiniYaml, object> GetLoader(Type type)
			{
				const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

				if (!string.IsNullOrEmpty(Loader))
				{
					var method = type.GetMethod(Loader, Flags);
					if (method == null)
						throw new InvalidOperationException("{0} does not specify a loader function '{1}'".F(type.Name, Loader));

					return (Func<MiniYaml, object>)Delegate.CreateDelegate(typeof(Func<MiniYaml, object>), method);
				}

				return null;
			}
		}

		public static string Translate(string key)
		{
			if (string.IsNullOrEmpty(key))
				return key;

			lock (TranslationsLock)
			{
				if (translations == null)
					return key;

				string value;
				if (!translations.TryGetValue(key, out value))
					return key;

				return value;
			}
		}

		public static void SetTranslations(IDictionary<string, string> translations)
		{
			lock (TranslationsLock)
				FieldLoader.translations = new Dictionary<string, string>(translations);
		}
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

	// Special-cases FieldFromYamlKeyAttribute for use with Dictionary<K,V>.
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class DictionaryFromYamlKeyAttribute : FieldLoader.SerializeAttribute
	{
		public DictionaryFromYamlKeyAttribute()
		{
			FromYamlKey = true;
			DictionaryFromYamlKey = true;
		}
	}

	// Mirrors DescriptionAttribute from System.ComponentModel but we don't want to have to use that everywhere.
	[AttributeUsage(AttributeTargets.All)]
	public sealed class DescAttribute : Attribute
	{
		public readonly string[] Lines;
		public DescAttribute(params string[] lines) { Lines = lines; }
	}
}
