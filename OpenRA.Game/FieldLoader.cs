#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA
{
	public static class FieldLoader
	{
		const char Comma = ',';

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
		}

		public static Func<string, Type, string, object> InvalidValueAction = (s, t, f) =>
			throw new YamlException($"FieldLoader: Cannot parse `{s}` into `{f}.{t}`");

		public static Action<string, Type> UnknownFieldAction = (s, f) =>
			throw new NotImplementedException($"FieldLoader: Missing field `{s}` on `{f.Name}`");

		static readonly ConcurrentCache<Type, FieldLoadInfo[]> TypeLoadInfo =
			new(BuildTypeLoadInfo);
		static readonly ConcurrentCache<string, BooleanExpression> BooleanExpressionCache =
			new(expression => new BooleanExpression(expression));
		static readonly ConcurrentCache<string, IntegerExpression> IntegerExpressionCache =
			new(expression => new IntegerExpression(expression));

		static readonly Dictionary<Type, Func<string, Type, string, object>> TypeParsers =
			new()
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
				{ typeof(CPos[]), ParseCPosArray },
				{ typeof(CVec), ParseCVec },
				{ typeof(CVec[]), ParseCVecArray },
				{ typeof(BooleanExpression), ParseBooleanExpression },
				{ typeof(IntegerExpression), ParseIntegerExpression },
				{ typeof(bool), ParseBool },
				{ typeof(int2[]), ParseInt2Array },
				{ typeof(Size), ParseSize },
				{ typeof(int2), ParseInt2 },
				{ typeof(float2), ParseFloat2 },
				{ typeof(float3), ParseFloat3 },
				{ typeof(Rectangle), ParseRectangle },
				{ typeof(DateTime), ParseDateTime }
			};

		static readonly Dictionary<Type, Func<string, Type, string, MiniYaml, object>> GenericTypeParsers =
			new()
			{
				{ typeof(HashSet<>), ParseHashSetOrList },
				{ typeof(List<>), ParseHashSetOrList },
				{ typeof(Dictionary<,>), ParseDictionary },
				{ typeof(BitSet<>), ParseBitSet },
				{ typeof(Nullable<>), ParseNullable },
			};

		static readonly object BoxedTrue = true;
		static readonly object BoxedFalse = false;
		static readonly object[] BoxedInts = Exts.MakeArray(33, i => (object)i);

		static object ParseInt(string fieldName, Type fieldType, string value)
		{
			if (Exts.TryParseInt32Invariant(value, out var res))
			{
				if (res >= 0 && res < BoxedInts.Length)
					return BoxedInts[res];
				return res;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseUShort(string fieldName, Type fieldType, string value)
		{
			if (Exts.TryParseUshortInvariant(value, out var res))
				return res;
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseLong(string fieldName, Type fieldType, string value)
		{
			if (Exts.TryParseInt64Invariant(value, out var res))
				return res;
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseFloat(string fieldName, Type fieldType, string value)
		{
			if (Exts.TryParseFloatOrPercentInvariant(value, out var res))
				return res;
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseDecimal(string fieldName, Type fieldType, string value)
		{
			if (value != null && decimal.TryParse(value.Replace("%", ""), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var res))
				return res * (value.Contains('%') ? 0.01m : 1m);
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseString(string fieldName, Type fieldType, string value)
		{
			return value;
		}

		static object ParseColor(string fieldName, Type fieldType, string value)
		{
			if (Color.TryParse(value, out var color))
				return color;

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseHotkey(string fieldName, Type fieldType, string value)
		{
			if (Hotkey.TryParse(value, out var res))
				return res;

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseHotkeyReference(string fieldName, Type fieldType, string value)
		{
			return Game.ModData.Hotkeys[value];
		}

		static object ParseWDist(string fieldName, Type fieldType, string value)
		{
			if (WDist.TryParse(value, out var res))
				return res;

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWVec(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 3
					&& WDist.TryParse(parts[0], out var rx)
					&& WDist.TryParse(parts[1], out var ry)
					&& WDist.TryParse(parts[2], out var rz))
					return new WVec(rx, ry, rz);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWVecArray(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

				if (parts.Length % 3 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var vecs = new WVec[parts.Length / 3];

				for (var i = 0; i < vecs.Length; ++i)
				{
					if (WDist.TryParse(parts[3 * i], out var rx)
						&& WDist.TryParse(parts[3 * i + 1], out var ry)
						&& WDist.TryParse(parts[3 * i + 2], out var rz))
						vecs[i] = new WVec(rx, ry, rz);
					else
						return InvalidValueAction(value, fieldType, fieldName);
				}

				return vecs;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWPos(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 3
					&& WDist.TryParse(parts[0], out var rx)
					&& WDist.TryParse(parts[1], out var ry)
					&& WDist.TryParse(parts[2], out var rz))
					return new WPos(rx, ry, rz);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWAngle(string fieldName, Type fieldType, string value)
		{
			if (Exts.TryParseInt32Invariant(value, out var res))
				return new WAngle(res);
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseWRot(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 3
					&& Exts.TryParseInt32Invariant(parts[0], out var rr)
					&& Exts.TryParseInt32Invariant(parts[1], out var rp)
					&& Exts.TryParseInt32Invariant(parts[2], out var ry))
					return new WRot(new WAngle(rr), new WAngle(rp), new WAngle(ry));
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseCPos(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 3
					&& Exts.TryParseInt32Invariant(parts[0], out var x)
					&& Exts.TryParseInt32Invariant(parts[1], out var y)
					&& Exts.TryParseByteInvariant(parts[2], out var layer))
					return new CPos(x, y, layer);

				if (parts.Length == 2
					&& Exts.TryParseInt32Invariant(parts[0], out x)
					&& Exts.TryParseInt32Invariant(parts[1], out y))
					return new CPos(x, y);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseCPosArray(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

				if (parts.Length % 2 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var vecs = new CPos[parts.Length / 2];
				for (var i = 0; i < vecs.Length; i++)
				{
					if (Exts.TryParseInt32Invariant(parts[2 * i], out var rx)
						&& Exts.TryParseInt32Invariant(parts[2 * i + 1], out var ry))
						vecs[i] = new CPos(rx, ry);
					else
						return InvalidValueAction(value, fieldType, fieldName);
				}

				return vecs;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseCVec(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 2
					&& Exts.TryParseInt32Invariant(parts[0], out var x)
					&& Exts.TryParseInt32Invariant(parts[1], out var y))
					return new CVec(x, y);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseCVecArray(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

				if (parts.Length % 2 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var vecs = new CVec[parts.Length / 2];
				for (var i = 0; i < vecs.Length; i++)
				{
					if (Exts.TryParseInt32Invariant(parts[2 * i], out var rx)
						&& Exts.TryParseInt32Invariant(parts[2 * i + 1], out var ry))
						vecs[i] = new CVec(rx, ry);
					else
						return InvalidValueAction(value, fieldType, fieldName);
				}

				return vecs;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseBooleanExpression(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				try
				{
					return BooleanExpressionCache[value];
				}
				catch (InvalidDataException e)
				{
					throw new YamlException($"FieldLoader: Cannot parse `{value}` into `{fieldName}.{fieldType}`: {e.Message}");
				}
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseIntegerExpression(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				try
				{
					return IntegerExpressionCache[value];
				}
				catch (InvalidDataException e)
				{
					throw new YamlException($"FieldLoader: Cannot parse `{value}` into `{fieldName}.{fieldType}`: {e.Message}");
				}
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseEnum(string fieldName, Type fieldType, string value)
		{
			// Will allow numeric values that fit the underlying type of the enum, even if they aren't defined enumeration members.
			if (Enum.TryParse(fieldType, value, true, out var enumValue))
			{
				return enumValue;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseBool(string fieldName, Type fieldType, string value)
		{
			if (bool.TryParse(value, out var result))
				return result ? BoxedTrue : BoxedFalse;

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseInt2Array(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length % 2 != 0)
					return InvalidValueAction(value, fieldType, fieldName);

				var ints = new int2[parts.Length / 2];

				for (var i = 0; i < ints.Length; i++)
				{
					if (Exts.TryParseInt32Invariant(parts[2 * i], out var x)
						&& Exts.TryParseInt32Invariant(parts[2 * i + 1], out var y))
						ints[i] = new int2(x, y);
					else
						return InvalidValueAction(value, fieldType, fieldName);
				}

				return ints;
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseSize(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 2
					&& Exts.TryParseInt32Invariant(parts[0], out var width)
					&& Exts.TryParseInt32Invariant(parts[1], out var height))
					return new Size(width, height);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseInt2(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 2
					&& Exts.TryParseInt32Invariant(parts[0], out var x)
					&& Exts.TryParseInt32Invariant(parts[1], out var y))
					return new int2(x, y);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseFloat2(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 2
					&& Exts.TryParseFloatOrPercentInvariant(parts[0], out var x)
					&& Exts.TryParseFloatOrPercentInvariant(parts[1], out var y))
					return new float2(x, y);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseFloat3(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 3
					&& Exts.TryParseFloatOrPercentInvariant(parts[0], out var x)
					&& Exts.TryParseFloatOrPercentInvariant(parts[1], out var y)
					&& Exts.TryParseFloatOrPercentInvariant(parts[2], out var z))
					return new float3(x, y, z);

				// z component is optional for compatibility with older float2 definitions
				if (parts.Length == 2
					&& Exts.TryParseFloatOrPercentInvariant(parts[0], out x)
					&& Exts.TryParseFloatOrPercentInvariant(parts[1], out y))
					return new float3(x, y, 0);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseRectangle(string fieldName, Type fieldType, string value)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length == 4
					&& Exts.TryParseInt32Invariant(parts[0], out var x)
					&& Exts.TryParseInt32Invariant(parts[1], out var y)
					&& Exts.TryParseInt32Invariant(parts[2], out var width)
					&& Exts.TryParseInt32Invariant(parts[3], out var height))
					return new Rectangle(x, y, width, height);
			}

			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseDateTime(string fieldName, Type fieldType, string value)
		{
			if (DateTime.TryParseExact(value, "yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
				return dt;
			return InvalidValueAction(value, fieldType, fieldName);
		}

		static object ParseHashSetOrList(string fieldName, Type fieldType, string value, MiniYaml yaml)
		{
			if (value == null)
				return Activator.CreateInstance(fieldType);

			var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var set = Activator.CreateInstance(fieldType, parts.Length);
			var arguments = fieldType.GetGenericArguments();
			var addMethod = fieldType.GetMethod(nameof(List<object>.Add), arguments);
			var addArgs = new object[1];
			for (var i = 0; i < parts.Length; i++)
			{
				addArgs[0] = GetValue(fieldName, arguments[0], parts[i]);
				addMethod.Invoke(set, addArgs);
			}

			return set;
		}

		static object ParseDictionary(string fieldName, Type fieldType, string value, MiniYaml yaml)
		{
			if (yaml == null)
				return Activator.CreateInstance(fieldType);

			var dict = Activator.CreateInstance(fieldType, yaml.Nodes.Length);
			var arguments = fieldType.GetGenericArguments();
			var addMethod = fieldType.GetMethod(nameof(Dictionary<object, object>.Add), arguments);
			var addArgs = new object[2];
			foreach (var node in yaml.Nodes)
			{
				addArgs[0] = GetValue(fieldName, arguments[0], node.Key);
				addArgs[1] = GetValue(fieldName, arguments[1], node.Value);
				addMethod.Invoke(dict, addArgs);
			}

			return dict;
		}

		static object ParseBitSet(string fieldName, Type fieldType, string value, MiniYaml yaml)
		{
			if (value != null)
			{
				var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				var ctor = fieldType.GetConstructor([typeof(string[])]);
				return ctor.Invoke([parts]);
			}
			else
			{
				var ctor = fieldType.GetConstructor([typeof(string[])]);
				return ctor.Invoke([Array.Empty<string>()]);
			}
		}

		static object ParseNullable(string fieldName, Type fieldType, string value, MiniYaml yaml)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			var innerType = fieldType.GetGenericArguments()[0];
			var innerValue = GetValue("Nullable<T>", innerType, value);
			return fieldType.GetConstructor([innerType]).Invoke([innerValue]);
		}

		public static void Load(object self, MiniYaml my)
		{
			var loadInfo = TypeLoadInfo[self.GetType()];
			List<string> missing = null;

			Dictionary<string, MiniYaml> md = null;

			foreach (var fli in loadInfo)
			{
				object val;

				md ??= my.ToDictionary();
				if (fli.Loader != null)
				{
					if (!fli.Attribute.Required || md.ContainsKey(fli.YamlName))
						val = fli.Loader(my);
					else
					{
						missing ??= [];
						missing.Add(fli.YamlName);
						continue;
					}
				}
				else
				{
					if (!TryGetValueFromYaml(fli.YamlName, fli.Field, md, out val))
					{
						if (fli.Attribute.Required)
						{
							missing ??= [];
							missing.Add(fli.YamlName);
						}

						continue;
					}
				}

				fli.Field.SetValue(self, val);
			}

			if (missing != null)
				throw new MissingFieldsException(missing.ToArray());
		}

		static bool TryGetValueFromYaml(string yamlName, FieldInfo field, Dictionary<string, MiniYaml> md, out object ret)
		{
			ret = null;

			if (!md.TryGetValue(yamlName, out var yaml))
				return false;

			ret = GetValue(field.Name, field.FieldType, yaml);
			return true;
		}

		public static T Load<T>(MiniYaml y) where T : new()
		{
			var t = new T();
			Load(t, y);
			return t;
		}

		public static void LoadFieldOrProperty(object target, string key, string value)
		{
			const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			key = key.Trim();

			var field = target.GetType().GetField(key, Flags);
			if (field != null)
			{
				field.SetValue(target, GetValue(field.Name, field.FieldType, value));
				return;
			}

			var prop = target.GetType().GetProperty(key, Flags);
			if (prop != null)
			{
				prop.SetValue(target, GetValue(prop.Name, prop.PropertyType, value), null);
				return;
			}

			UnknownFieldAction(key, target.GetType());
		}

		public static T GetValue<T>(string field, string value)
		{
			return (T)GetValue(field, typeof(T), value, null);
		}

		static object GetValue(string fieldName, Type fieldType, string value)
		{
			return GetValue(fieldName, fieldType, value, null);
		}

		static object GetValue(string fieldName, Type fieldType, MiniYaml yaml)
		{
			return GetValue(fieldName, fieldType, yaml.Value, yaml);
		}

		static object GetValue(string fieldName, Type fieldType, string value, MiniYaml yaml)
		{
			value = value?.Trim();
			if (fieldType.IsGenericType)
			{
				if (GenericTypeParsers.TryGetValue(fieldType.GetGenericTypeDefinition(), out var parseFuncGeneric))
					return parseFuncGeneric(fieldName, fieldType, value, yaml);
			}
			else
			{
				if (TypeParsers.TryGetValue(fieldType, out var parseFunc))
					return parseFunc(fieldName, fieldType, value);

				if (fieldType.IsArray && fieldType.GetArrayRank() == 1)
				{
					if (value == null)
						return Array.CreateInstance(fieldType.GetElementType(), 0);

					var parts = value.Split(Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

					var ret = Array.CreateInstance(fieldType.GetElementType(), parts.Length);
					for (var i = 0; i < parts.Length; i++)
						ret.SetValue(GetValue(fieldName, fieldType.GetElementType(), parts[i]), i);
					return ret;
				}

				if (fieldType.IsEnum)
					return ParseEnum(fieldName, fieldType, value);
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
			public readonly Func<MiniYaml, object> Loader;
			public string YamlName => Field.Name;

			public FieldLoadInfo(FieldInfo field, SerializeAttribute attr, Func<MiniYaml, object> loader = null)
			{
				Field = field;
				Attribute = attr;
				Loader = loader;
			}
		}

		public static IEnumerable<FieldLoadInfo> GetTypeLoadInfo(Type type)
		{
			return TypeLoadInfo[type].Where(fli => fli.Field.IsPublic || (fli.Attribute.Serialize && !fli.Attribute.IsDefault));
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

				var loader = sa.GetLoader(type);

				var fli = new FieldLoadInfo(field, sa, loader);
				ret.Add(fli);
			}

			return ret.ToArray();
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class IgnoreAttribute : SerializeAttribute
		{
			public IgnoreAttribute()
				: base(serialize: false) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class RequireAttribute : SerializeAttribute
		{
			public RequireAttribute()
				: base(serialize: true, required: true) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class LoadUsingAttribute : SerializeAttribute
		{
			public LoadUsingAttribute(string loader, bool required = false)
				: base(serialize: true, required, loader) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class SerializeAttribute : Attribute
		{
			public static readonly SerializeAttribute Default = new(true);

			public bool IsDefault => this == Default;

			public readonly bool Serialize;
			public readonly bool Required;
			public readonly string Loader;

			protected SerializeAttribute(bool serialize = true, bool required = false, string loader = null)
			{
				Serialize = serialize;
				Required = required;
				Loader = loader;
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
}
