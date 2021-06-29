#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA
{
	public static class FieldLoader
	{
		const char SplitComma = ',';

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

			public MissingFieldsException(string[] missing, string header = null, string headerSingle = null)
				: base(null)
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
			throw new YamlException($"FieldLoader: Cannot parse `{s}` into `{f}.{t}` ");
		};

		public static Action<string, Type> UnknownFieldAction = (s, f) =>
		{
			throw new NotImplementedException($"FieldLoader: Missing field `{s}` on `{f.Name}`");
		};

		static readonly ConcurrentCache<Type, FieldLoadInfo[]> TypeLoadInfo =
			new ConcurrentCache<Type, FieldLoadInfo[]>(BuildTypeLoadInfo);
		static readonly ConcurrentCache<string, BooleanExpression> BooleanExpressionCache =
			new ConcurrentCache<string, BooleanExpression>(expression => new BooleanExpression(expression));
		static readonly ConcurrentCache<string, IntegerExpression> IntegerExpressionCache =
			new ConcurrentCache<string, IntegerExpression>(expression => new IntegerExpression(expression));

		static readonly Dictionary<Type, Func<string, Type, string, MemberInfo, object>> TypeParsers =
			new Dictionary<Type, Func<string, Type, string, MemberInfo, object>>()
			{
				{ typeof(int), ParseInt },
				{ typeof(ushort), ParseUShort },
				{ typeof(long), ParseLong },
				{ typeof(float), ParseFloat },
				{ typeof(decimal), ParseDecimal },
				{ typeof(string), ParseString },
				{ typeof(Color), ParseColor },
				{ typeof(Hotkey), ParseHotkey },
				{ typeof(HotkeyReference), ParseHotkeyReference },
				{ typeof(WDist), ParseWDist },
				{ typeof(WVec), ParseWVec },
				{ typeof(WVec[]), ParseWVecArray },
				{ typeof(WPos), ParseWPos },
				{ typeof(WAngle), ParseWAngle },
				{ typeof(WRot), ParseWRot },
				{ typeof(CPos), ParseCPos },
				{ typeof(CVec), ParseCVec },
				{ typeof(CVec[]), ParseCVecArray },
				{ typeof(BooleanExpression), ParseBooleanExpression },
				{ typeof(IntegerExpression), ParseIntegerExpression },
				{ typeof(Enum), ParseEnum },
				{ typeof(bool), ParseBool },
				{ typeof(int2[]), ParseInt2Array },
				{ typeof(Size), ParseSize },
				{ typeof(int2), ParseInt2 },
				{ typeof(float2), ParseFloat2 },
				{ typeof(float3), ParseFloat3 },
				{ typeof(Rectangle), ParseRectangle },
				{ typeof(DateTime), ParseDateTime }
			};

		static readonly Dictionary<Type, Func<string, Type, string, MiniYaml, MemberInfo, object>> GenericTypeParsers =
			new Dictionary<Type, Func<string, Type, string, MiniYaml, MemberInfo, object>>()
			{
				{ typeof(HashSet<>), ParseHashSetOrList },
				{ typeof(List<>), ParseHashSetOrList },
				{ typeof(Dictionary<,>), ParseDictionary },
				{ typeof(BitSet<>), ParseBitSet },
				{ typeof(Nullable<>), ParseNullable },
			};

		static object ParseInt(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (Exts.TryParseIntegerInvariant(value, out var res))
				return res;
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseUShort(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (ushort.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var res))
				return res;
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseLong(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (long.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var res))
				return res;
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseFloat(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null && float.TryParse(value.Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var res))
				return res * (value.Contains('%') ? 0.01f : 1f);
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseDecimal(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null && decimal.TryParse(value.Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var res))
				return res * (value.Contains('%') ? 0.01m : 1m);
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseString(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			return value;
		}

		static object ParseColor(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null && Color.TryParse(value, out var color))
					return color;

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseHotkey(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (Hotkey.TryParse(value, out var res))
				return res;

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseHotkeyReference(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			return Game.ModData.Hotkeys[value];
		}

		static object ParseWDist(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (WDist.TryParse(value, out var res))
				return res;

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWVec(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma);
				if (parts.Length == 3)
				{
					if (WDist.TryParse(parts[0], out var rx) && WDist.TryParse(parts[1], out var ry) && WDist.TryParse(parts[2], out var rz))
						return new WVec(rx, ry, rz);
				}
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWVecArray(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma);

				if (parts.Length % 3 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var vecs = new WVec[parts.Length / 3];

				for (var i = 0; i < vecs.Length; ++i)
				{
					if (WDist.TryParse(parts[3 * i], out var rx)
							&& WDist.TryParse(parts[3 * i + 1], out var ry)
							&& WDist.TryParse(parts[3 * i + 2], out var rz))
						vecs[i] = new WVec(rx, ry, rz);
				}

				return vecs;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWPos(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma);
				if (parts.Length == 3)
				{
					if (WDist.TryParse(parts[0], out var rx)
						&& WDist.TryParse(parts[1], out var ry)
						&& WDist.TryParse(parts[2], out var rz))
						return new WPos(rx, ry, rz);
				}
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWAngle(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (Exts.TryParseIntegerInvariant(value, out var res))
				return new WAngle(res);
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWRot(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma);
				if (parts.Length == 3)
				{
					if (Exts.TryParseIntegerInvariant(parts[0], out var rr)
							&& Exts.TryParseIntegerInvariant(parts[1], out var rp)
							&& Exts.TryParseIntegerInvariant(parts[2], out var ry))
						return new WRot(new WAngle(rr), new WAngle(rp), new WAngle(ry));
				}
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseCPos(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				return new CPos(Exts.ParseIntegerInvariant(parts[0]), Exts.ParseIntegerInvariant(parts[1]));
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseCVec(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				return new CVec(Exts.ParseIntegerInvariant(parts[0]), Exts.ParseIntegerInvariant(parts[1]));
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseCVecArray(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma);

				if (parts.Length % 2 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var vecs = new CVec[parts.Length / 2];
				for (var i = 0; i < vecs.Length; i++)
				{
					if (int.TryParse(parts[2 * i], out var rx)
							&& int.TryParse(parts[2 * i + 1], out var ry))
						vecs[i] = new CVec(rx, ry);
				}

				return vecs;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseBooleanExpression(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				try
				{
					return BooleanExpressionCache[value];
				}
				catch (InvalidDataException e)
				{
					throw new YamlException(e.Message);
				}
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseIntegerExpression(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				try
				{
					return IntegerExpressionCache[value];
				}
				catch (InvalidDataException e)
				{
					throw new YamlException(e.Message);
				}
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseEnum(string fieldName, Type fieldType, string value, MemberInfo field)
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

		static object ParseBool(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (bool.TryParse(value.ToLowerInvariant(), out var result))
				return result;

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseInt2Array(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length % 2 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var ints = new int2[parts.Length / 2];
				for (var i = 0; i < ints.Length; i++)
					ints[i] = new int2(Exts.ParseIntegerInvariant(parts[2 * i]), Exts.ParseIntegerInvariant(parts[2 * i + 1]));

				return ints;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseSize(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				return new Size(Exts.ParseIntegerInvariant(parts[0]), Exts.ParseIntegerInvariant(parts[1]));
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseInt2(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2)
					return InvalidValueAction(value, fieldType, fieldName);

				return new int2(Exts.ParseIntegerInvariant(parts[0]), Exts.ParseIntegerInvariant(parts[1]));
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseFloat2(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				float xx = 0;
				float yy = 0;
				if (float.TryParse(parts[0].Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var res))
					xx = res * (parts[0].Contains('%') ? 0.01f : 1f);
				if (float.TryParse(parts[1].Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out res))
					yy = res * (parts[1].Contains('%') ? 0.01f : 1f);
				return new float2(xx, yy);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseFloat3(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				float.TryParse(parts[0], NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var x);
				float.TryParse(parts[1], NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var y);

				// z component is optional for compatibility with older float2 definitions
				float z = 0;
				if (parts.Length > 2)
					float.TryParse(parts[2], NumberStyles.Float, NumberFormatInfo.InvariantInfo, out z);

				return new float3(x, y, z);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseRectangle(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				return new Rectangle(
					Exts.ParseIntegerInvariant(parts[0]),
					Exts.ParseIntegerInvariant(parts[1]),
					Exts.ParseIntegerInvariant(parts[2]),
					Exts.ParseIntegerInvariant(parts[3]));
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseDateTime(string fieldName, Type fieldType, string value, MemberInfo field)
		{
			if (DateTime.TryParseExact(value, "yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
				return dt;
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseHashSetOrList(string fieldName, Type fieldType, string value, MiniYaml yaml, MemberInfo field)
		{
			var set = Activator.CreateInstance(fieldType);
			if (value == null)
				return set;

			var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
			var addMethod = fieldType.GetMethod("Add", fieldType.GetGenericArguments());
			for (var i = 0; i < parts.Length; i++)
				addMethod.Invoke(set, new[] { GetValue(fieldName, fieldType.GetGenericArguments()[0], parts[i].Trim(), field) });
			return set;
		}

		static object ParseDictionary(string fieldName, Type fieldType, string value, MiniYaml yaml, MemberInfo field)
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

		static object ParseBitSet(string fieldName, Type fieldType, string value, MiniYaml yaml, MemberInfo field)
		{
			if (value != null)
			{
				var parts = value.Split(SplitComma, StringSplitOptions.RemoveEmptyEntries);
				var ctor = fieldType.GetConstructor(new[] { typeof(string[]) });
				return ctor.Invoke(new object[] { parts.Select(p => p.Trim()).ToArray() });
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseNullable(string fieldName, Type fieldType, string value, MiniYaml yaml, MemberInfo field)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			var innerType = fieldType.GetGenericArguments().First();
			var innerValue = GetValue("Nullable<T>", innerType, value, field);
			return fieldType.GetConstructor(new[] { innerType }).Invoke(new[] { innerValue });
		}

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

			if (!md.TryGetValue(yamlName, out var yaml))
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
			var value = yaml.Value?.Trim();
			if (fieldType.IsGenericType)
			{
				if (GenericTypeParsers.TryGetValue(fieldType.GetGenericTypeDefinition(), out var parseFuncGeneric))
					return parseFuncGeneric(fieldName, fieldType, value, yaml, field);
			}
			else
			{
				if (TypeParsers.TryGetValue(fieldType, out var parseFunc))
					return parseFunc(fieldName, fieldType, value, field);

				if (fieldType.IsArray && fieldType.GetArrayRank() == 1)
				{
					if (value == null)
						return Array.CreateInstance(fieldType.GetElementType(), 0);

					var options = field != null && field.HasAttribute<AllowEmptyEntriesAttribute>() ?
						StringSplitOptions.None : StringSplitOptions.RemoveEmptyEntries;
					var parts = value.Split(SplitComma, options);

					var ret = Array.CreateInstance(fieldType.GetElementType(), parts.Length);
					for (var i = 0; i < parts.Length; i++)
						ret.SetValue(GetValue(fieldName, fieldType.GetElementType(), parts[i].Trim(), field), i);
					return ret;
				}
			}

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

			UnknownFieldAction($"[Type] {value}", fieldType);
			return null;
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
		public sealed class AllowEmptyEntriesAttribute : SerializeAttribute
		{
			public AllowEmptyEntriesAttribute()
				: base(allowEmptyEntries: true) { }
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

			public bool IsDefault => this == Default;

			public readonly bool Serialize;
			public string YamlName;
			public string Loader;
			public bool FromYamlKey;
			public bool DictionaryFromYamlKey;
			public bool Required;
			public bool AllowEmptyEntries;

			public SerializeAttribute(bool serialize = true, bool required = false, bool allowEmptyEntries = false)
			{
				Serialize = serialize;
				Required = required;
				AllowEmptyEntries = allowEmptyEntries;
			}

			internal Func<MiniYaml, object> GetLoader(Type type)
			{
				const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

				if (!string.IsNullOrEmpty(Loader))
				{
					var method = type.GetMethod(Loader, Flags);
					if (method == null)
						throw new InvalidOperationException($"{type.Name} does not specify a loader function '{Loader}'");

					return (Func<MiniYaml, object>)Delegate.CreateDelegate(typeof(Func<MiniYaml, object>), method);
				}

				return null;
			}
		}
	}

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
