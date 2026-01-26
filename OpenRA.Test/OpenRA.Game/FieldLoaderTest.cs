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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class FieldLoaderTest
	{
		sealed class TypeInfo
		{
#pragma warning disable CS0169
#pragma warning disable CS0649
#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable RCS1170 // Use read-only auto-implemented property
			int privateField;
			public int PublicField;

			[FieldLoader.Ignore]
			int privateIgnoreField;
			[FieldLoader.Ignore]
			public int PublicIgnoreField;

			[FieldLoader.Require]
			int privateRequiredField;
			[FieldLoader.Require]
			public int PublicRequiredField;

			[FieldLoader.LoadUsing(nameof(LoadInt32))]
			int privateLoadUsingField;
			[FieldLoader.LoadUsing(nameof(LoadInt32))]
			public int PublicLoadUsingField;

			int PrivateProperty { get; set; }
			public int PublicProperty { get; set; }

			static int privateStaticField;
			public static int PublicStaticField;

			static object LoadInt32(MiniYaml _) => 123;
#pragma warning restore RCS1170 // Use read-only auto-implemented property
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CA1823 // Avoid unused private fields
#pragma warning disable CS0649
#pragma warning restore CS0169
		}

		[Test]
		public void GetTypeLoadInfo()
		{
			var infos = FieldLoader.GetTypeLoadInfo(typeof(TypeInfo)).ToList();

			Assert.That(infos, Has.Count.EqualTo(5));

			Assert.That(infos[0].Field, Is.EqualTo(typeof(TypeInfo).GetField(nameof(TypeInfo.PublicField))));
			Assert.That(infos[0].Attribute, Is.EqualTo(FieldLoader.SerializeAttribute.Default));
			Assert.That(infos[0].YamlName, Is.EqualTo(nameof(TypeInfo.PublicField)));
			Assert.That(infos[0].Loader, Is.Null);

			Assert.That(infos[1].Field, Is.EqualTo(typeof(TypeInfo).GetField("privateRequiredField", BindingFlags.NonPublic | BindingFlags.Instance)));
			Assert.That(infos[1].Attribute, Is.EqualTo(new FieldLoader.RequireAttribute()));
			Assert.That(infos[1].YamlName, Is.EqualTo("privateRequiredField"));
			Assert.That(infos[1].Loader, Is.Null);

			Assert.That(infos[2].Field, Is.EqualTo(typeof(TypeInfo).GetField(nameof(TypeInfo.PublicRequiredField))));
			Assert.That(infos[2].Attribute, Is.EqualTo(new FieldLoader.RequireAttribute()));
			Assert.That(infos[2].YamlName, Is.EqualTo(nameof(TypeInfo.PublicRequiredField)));
			Assert.That(infos[2].Loader, Is.Null);

			Assert.That(infos[3].Field, Is.EqualTo(typeof(TypeInfo).GetField("privateLoadUsingField", BindingFlags.NonPublic | BindingFlags.Instance)));
			Assert.That(infos[3].Attribute, Is.EqualTo(new FieldLoader.LoadUsingAttribute("LoadInt32")));
			Assert.That(infos[3].YamlName, Is.EqualTo("privateLoadUsingField"));
			Assert.That(infos[3].Loader(new MiniYaml(null)), Is.EqualTo(123));

			Assert.That(infos[4].Field, Is.EqualTo(typeof(TypeInfo).GetField(nameof(TypeInfo.PublicLoadUsingField))));
			Assert.That(infos[4].Attribute, Is.EqualTo(new FieldLoader.LoadUsingAttribute("LoadInt32")));
			Assert.That(infos[4].YamlName, Is.EqualTo(nameof(TypeInfo.PublicLoadUsingField)));
			Assert.That(infos[4].Loader(new MiniYaml(null)), Is.EqualTo(123));
		}

		[Test]
		public void GetValue_UnknownField()
		{
			static void Act() => FieldLoader.GetValue<object>("field", "test");

			Assert.That(Act, Throws.TypeOf<NotImplementedException>().And.Message.EqualTo("FieldLoader: Missing field `[Type] test` on `Object`"));
		}

		static IEnumerable<TestCaseData> GetValue_InvalidValue_TestCases()
		{
			return
			[
				new TestCaseData(null) { TypeArgs = [typeof(int)] },
				new TestCaseData("test") { TypeArgs = [typeof(int)] },
				new TestCaseData("1.2") { TypeArgs = [typeof(int)] },
				new TestCaseData((int.MaxValue + 1L).ToString(CultureInfo.InvariantCulture)) { TypeArgs = [typeof(int)] },
				new TestCaseData(null) { TypeArgs = [typeof(ushort)] },
				new TestCaseData("test") { TypeArgs = [typeof(ushort)] },
				new TestCaseData("1.2") { TypeArgs = [typeof(ushort)] },
				new TestCaseData((ushort.MaxValue + 1L).ToString(CultureInfo.InvariantCulture)) { TypeArgs = [typeof(ushort)] },
				new TestCaseData(null) { TypeArgs = [typeof(long)] },
				new TestCaseData("test") { TypeArgs = [typeof(long)] },
				new TestCaseData("1.2") { TypeArgs = [typeof(long)] },
				new TestCaseData((long.MaxValue + 1UL).ToString(CultureInfo.InvariantCulture)) { TypeArgs = [typeof(long)] },
				new TestCaseData(null) { TypeArgs = [typeof(float)] },
				new TestCaseData("test") { TypeArgs = [typeof(float)] },
				new TestCaseData("1,2") { TypeArgs = [typeof(float)] },
				new TestCaseData(null) { TypeArgs = [typeof(decimal)] },
				new TestCaseData("test") { TypeArgs = [typeof(decimal)] },
				new TestCaseData("1,2") { TypeArgs = [typeof(decimal)] },
				new TestCaseData(null) { TypeArgs = [typeof(Color)] },
				new TestCaseData("test") { TypeArgs = [typeof(Color)] },
				new TestCaseData(null) { TypeArgs = [typeof(Hotkey)] },
				new TestCaseData("test") { TypeArgs = [typeof(Hotkey)] },
				new TestCaseData(null) { TypeArgs = [typeof(WDist)] },
				new TestCaseData("test") { TypeArgs = [typeof(WDist)] },
				new TestCaseData(null) { TypeArgs = [typeof(WVec)] },
				new TestCaseData("test") { TypeArgs = [typeof(WVec)] },
				new TestCaseData("1,2") { TypeArgs = [typeof(WVec)] },
				new TestCaseData("1,test,3") { TypeArgs = [typeof(WVec)] },
				new TestCaseData("1,2,3,4") { TypeArgs = [typeof(WVec)] },
				new TestCaseData(null) { TypeArgs = [typeof(WVec[])] },
				new TestCaseData("test") { TypeArgs = [typeof(WVec[])] },
				new TestCaseData("1,2") { TypeArgs = [typeof(WVec[])] },
				new TestCaseData("1,test,3") { TypeArgs = [typeof(WVec[])] },
				new TestCaseData("1,2,3,4") { TypeArgs = [typeof(WVec[])] },
				new TestCaseData(null) { TypeArgs = [typeof(WPos)] },
				new TestCaseData("test") { TypeArgs = [typeof(WPos)] },
				new TestCaseData("1,2") { TypeArgs = [typeof(WPos)] },
				new TestCaseData("1,test,3") { TypeArgs = [typeof(WPos)] },
				new TestCaseData("1,2,3,4") { TypeArgs = [typeof(WPos)] },
				new TestCaseData(null) { TypeArgs = [typeof(WAngle)] },
				new TestCaseData("test") { TypeArgs = [typeof(WAngle)] },
				new TestCaseData("1,2") { TypeArgs = [typeof(WAngle)] },
				new TestCaseData(null) { TypeArgs = [typeof(WRot)] },
				new TestCaseData("test") { TypeArgs = [typeof(WRot)] },
				new TestCaseData("1,2") { TypeArgs = [typeof(WRot)] },
				new TestCaseData("1,test,3") { TypeArgs = [typeof(WRot)] },
				new TestCaseData("1,2,3,4") { TypeArgs = [typeof(WRot)] },
				new TestCaseData(null) { TypeArgs = [typeof(CPos)] },
				new TestCaseData("test") { TypeArgs = [typeof(CPos)] },
				new TestCaseData("1") { TypeArgs = [typeof(CPos)] },
				new TestCaseData("1,test,3") { TypeArgs = [typeof(CPos)] },
				new TestCaseData("1,2,3,4") { TypeArgs = [typeof(CPos)] },
				new TestCaseData(null) { TypeArgs = [typeof(CPos[])] },
				new TestCaseData("test") { TypeArgs = [typeof(CPos[])] },
				new TestCaseData("1") { TypeArgs = [typeof(CPos[])] },
				new TestCaseData("1,test") { TypeArgs = [typeof(CPos[])] },
				new TestCaseData("1,2,3") { TypeArgs = [typeof(CPos[])] },
				new TestCaseData(null) { TypeArgs = [typeof(CVec)] },
				new TestCaseData("test") { TypeArgs = [typeof(CVec)] },
				new TestCaseData("1") { TypeArgs = [typeof(CVec)] },
				new TestCaseData("1,test") { TypeArgs = [typeof(CVec)] },
				new TestCaseData("1,2,3") { TypeArgs = [typeof(CVec)] },
				new TestCaseData(null) { TypeArgs = [typeof(CVec[])] },
				new TestCaseData("test") { TypeArgs = [typeof(CVec[])] },
				new TestCaseData("1") { TypeArgs = [typeof(CVec[])] },
				new TestCaseData("1,test") { TypeArgs = [typeof(CVec[])] },
				new TestCaseData("1,2,3") { TypeArgs = [typeof(CVec[])] },
				new TestCaseData(null) { TypeArgs = [typeof(BooleanExpression)] },
				new TestCaseData(null) { TypeArgs = [typeof(IntegerExpression)] },
				new TestCaseData(null) { TypeArgs = [typeof(MapGridType)] },
				new TestCaseData(null) { TypeArgs = [typeof(bool)] },
				new TestCaseData("test") { TypeArgs = [typeof(bool)] },
				new TestCaseData(null) { TypeArgs = [typeof(int2[])] },
				new TestCaseData("test") { TypeArgs = [typeof(int2[])] },
				new TestCaseData("1") { TypeArgs = [typeof(int2[])] },
				new TestCaseData("1,test") { TypeArgs = [typeof(int2[])] },
				new TestCaseData("1,2,3") { TypeArgs = [typeof(int2[])] },
				new TestCaseData(null) { TypeArgs = [typeof(Size)] },
				new TestCaseData("test") { TypeArgs = [typeof(Size)] },
				new TestCaseData("1") { TypeArgs = [typeof(Size)] },
				new TestCaseData("1,test") { TypeArgs = [typeof(Size)] },
				new TestCaseData("1,2,3") { TypeArgs = [typeof(Size)] },
				new TestCaseData(null) { TypeArgs = [typeof(int2)] },
				new TestCaseData("test") { TypeArgs = [typeof(int2)] },
				new TestCaseData("1") { TypeArgs = [typeof(int2)] },
				new TestCaseData("1,test") { TypeArgs = [typeof(int2)] },
				new TestCaseData("1,2,3") { TypeArgs = [typeof(int2)] },
				new TestCaseData(null) { TypeArgs = [typeof(float2)] },
				new TestCaseData("test") { TypeArgs = [typeof(float2)] },
				new TestCaseData("1") { TypeArgs = [typeof(float2)] },
				new TestCaseData("1,test") { TypeArgs = [typeof(float2)] },
				new TestCaseData("1,2,3") { TypeArgs = [typeof(float2)] },
				new TestCaseData(null) { TypeArgs = [typeof(float3)] },
				new TestCaseData("test") { TypeArgs = [typeof(float3)] },
				new TestCaseData("1") { TypeArgs = [typeof(float3)] },
				new TestCaseData("1,test") { TypeArgs = [typeof(float3)] },
				new TestCaseData("1,2,3,4") { TypeArgs = [typeof(float3)] },
				new TestCaseData(null) { TypeArgs = [typeof(Rectangle)] },
				new TestCaseData("test") { TypeArgs = [typeof(Rectangle)] },
				new TestCaseData("1,2,3") { TypeArgs = [typeof(Rectangle)] },
				new TestCaseData("1,test,3,4") { TypeArgs = [typeof(Rectangle)] },
				new TestCaseData("1,2,3,4,5") { TypeArgs = [typeof(Rectangle)] },
				new TestCaseData(null) { TypeArgs = [typeof(DateTime)] },
				new TestCaseData("test") { TypeArgs = [typeof(DateTime)] },
				new TestCaseData("2000-01-01") { TypeArgs = [typeof(DateTime)] },
			];
		}

		[TestCaseSource(nameof(GetValue_InvalidValue_TestCases))]
		public void GetValue_InvalidValue<T>(string input)
		{
			void Act() => FieldLoader.GetValue<T>("field", input);

			Assert.That(Act, Throws.TypeOf<YamlException>().And.Message.EqualTo($"FieldLoader: Cannot parse `{input}` into `field.{typeof(T).FullName}`"));
		}

		[TestCase(TypeArgs = [typeof(BooleanExpression)])]
		[TestCase(TypeArgs = [typeof(IntegerExpression)])]
		public void GetValue_InvalidValue<T>()
		{
			static void Act() => FieldLoader.GetValue<T>("field", "");

			Assert.That(Act, Throws.TypeOf<YamlException>().And.Message.EqualTo($"FieldLoader: Cannot parse `` into `field.{typeof(T).FullName}`: Empty expression"));
		}

		static IEnumerable<TestCaseData> GetValue_Primitive_TestCases()
		{
			return
			[
				new TestCaseData(123),
				new TestCaseData((ushort)123),
				new TestCaseData(123L),
				new TestCaseData(123.4f.ToString(CultureInfo.InvariantCulture)),
				new TestCaseData(123m),
				new TestCaseData("test"),
				new TestCaseData(Color.CornflowerBlue),
				new TestCaseData(new Hotkey(Keycode.A, Modifiers.Shift)),
				new TestCaseData(new WDist(123)),
				new TestCaseData(new WVec(123, 456, 789)),
				new TestCaseData(new WPos(123, 456, 789)),
				new TestCaseData(new WAngle(123)),
				new TestCaseData(new WRot(new WAngle(123), new WAngle(456), new WAngle(789))),
				new TestCaseData(new CPos(123, 456)),
				new TestCaseData(new CPos(123, 456, 78)),
				new TestCaseData(new CVec(123, 456)),
				new TestCaseData(MapGridType.RectangularIsometric),
				new TestCaseData((MapGridType)byte.MaxValue),
				new TestCaseData(SystemActors.World | SystemActors.EditorWorld),
				new TestCaseData(true),
				new TestCaseData(new Size(123, 456)),
				new TestCaseData(new int2(123, 456)),
				new TestCaseData(new float2(123, 456)),
				new TestCaseData(new float3(123, 456, 789)),
				new TestCaseData(new Rectangle(123, 456, 789, 123)),
			];
		}

		[TestCaseSource(nameof(GetValue_Primitive_TestCases))]
		public void GetValue_Primitive<T>(T expected)
		{
			var actual = FieldLoader.GetValue<T>("field", $"  {expected}  ");

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void GetValue_NullString()
		{
			var actual = FieldLoader.GetValue<string>("field", null);

			Assert.That(actual, Is.Null);
		}

		[Test]
		public void GetValue_BooleanExpression()
		{
			var actual = FieldLoader.GetValue<BooleanExpression>("field", "  true  ");

			Assert.That(actual.Expression, Is.EqualTo("true"));
		}

		[Test]
		public void GetValue_IntegerExpression()
		{
			var actual = FieldLoader.GetValue<IntegerExpression>("field", "  1 + 2  ");

			Assert.That(actual.Expression, Is.EqualTo("1 + 2"));
		}

		[Test]
		public void GetValue_DateTime()
		{
			var expected = new DateTime(2000, 1, 1);
			var input = expected.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);

			var actual = FieldLoader.GetValue<DateTime>("field", $"  {input}  ");

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase("123%", 1.23f)]
		[TestCase("%123", 1.23f)]
		[TestCase("123.456%", 1.23456f)]
		[TestCase("%123.456", 1.23456f)]
		public void GetValue_FloatPercentage(string input, float expected)
		{
			var actual = FieldLoader.GetValue<float>("field", $"  {input}  ");

			Assert.That(actual, Is.EqualTo(expected));
		}

		static IEnumerable<TestCaseData> GetValue_DecimalPercentage_TestCases()
		{
			return
			[
				new TestCaseData("123%", 1.23m),
				new TestCaseData("%123", 1.23m),
				new TestCaseData("123.456%", 1.23456m),
				new TestCaseData("%123.456", 1.23456m),
			];
		}

		[TestCaseSource(nameof(GetValue_DecimalPercentage_TestCases))]
		public void GetValue_DecimalPercentage(string input, decimal expected)
		{
			var actual = FieldLoader.GetValue<decimal>("field", $"  {input}  ");

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void GetValue_float3_TwoElements()
		{
			var actual = FieldLoader.GetValue<float3>("field", "123,456");

			Assert.That(actual, Is.EqualTo(new float3(123, 456, 0)));
		}

		[Test]
		public void GetValue_WVecArray()
		{
			var actual = FieldLoader.GetValue<WVec[]>("field", " 1 , 2 , 3 , 4 , 5 , 6 , , ");

			Assert.That(actual, Is.EqualTo(new WVec[] { new(1, 2, 3), new(4, 5, 6) }));
		}

		[Test]
		public void GetValue_CPosArray()
		{
			var actual = FieldLoader.GetValue<CPos[]>("field", " 1 , 2 , 3 , 4 , 5 , 6 , , ");

			Assert.That(actual, Is.EqualTo(new CPos[] { new(1, 2), new(3, 4), new(5, 6) }));
		}

		[Test]
		public void GetValue_CVecArray()
		{
			var actual = FieldLoader.GetValue<CVec[]>("field", " 1 , 2 , 3 , 4 , 5 , 6 , , ");

			Assert.That(actual, Is.EqualTo(new CVec[] { new(1, 2), new(3, 4), new(5, 6) }));
		}

		[Test]
		public void GetValue_int2Array()
		{
			var actual = FieldLoader.GetValue<int2[]>("field", " 1 , 2 , 3 , 4 , 5 , 6 , , ");

			Assert.That(actual, Is.EqualTo(new int2[] { new(1, 2), new(3, 4), new(5, 6) }));
		}

		[Test]
		public void GetValue_WVecImmutableArray()
		{
			var actual = FieldLoader.GetValue<ImmutableArray<WVec>>("field", " 1 , 2 , 3 , 4 , 5 , 6 , , ");

			Assert.That(actual, Is.EqualTo(new WVec[] { new(1, 2, 3), new(4, 5, 6) }));
		}

		[Test]
		public void GetValue_CPosImmutableArray()
		{
			var actual = FieldLoader.GetValue<ImmutableArray<CPos>>("field", " 1 , 2 , 3 , 4 , 5 , 6 , , ");

			Assert.That(actual, Is.EqualTo(new CPos[] { new(1, 2), new(3, 4), new(5, 6) }));
		}

		[Test]
		public void GetValue_CVecImmutableArray()
		{
			var actual = FieldLoader.GetValue<ImmutableArray<CVec>>("field", " 1 , 2 , 3 , 4 , 5 , 6 , , ");

			Assert.That(actual, Is.EqualTo(new CVec[] { new(1, 2), new(3, 4), new(5, 6) }));
		}

		[Test]
		public void GetValue_int2ImmutableArray()
		{
			var actual = FieldLoader.GetValue<ImmutableArray<int2>>("field", " 1 , 2 , 3 , 4 , 5 , 6 , , ");

			Assert.That(actual, Is.EqualTo(new int2[] { new(1, 2), new(3, 4), new(5, 6) }));
		}

		[TestCase(null, null)]
		[TestCase("", null)]
		[TestCase("123", 123)]
		public void GetValue_Nullable(string input, int? expected)
		{
			var actual = FieldLoader.GetValue<int?>("field", $"  {input}  ");

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null, new int[] { })]
		[TestCase("", new int[] { })]
		[TestCase("1", new int[] { 1 })]
		[TestCase("1,2,3", new int[] { 1, 2, 3 })]
		[TestCase("1,,3", new int[] { 1, 3 })]
		[TestCase(" 1 , 2 , 3 ", new int[] { 1, 2, 3 })]
		[TestCase(" 1 ,  , 3 ", new int[] { 1, 3 })]
		[TestCase("1,1,2,2,3", new int[] { 1, 1, 2, 2, 3 })]
		[TestCase("1,1,1,1", new int[] { 1, 1, 1, 1 })]
		public void GetValue_Array(string input, int[] expected)
		{
			var actual = FieldLoader.GetValue<int[]>("field", input);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null, new int[] { })]
		[TestCase("", new int[] { })]
		[TestCase("1", new int[] { 1 })]
		[TestCase("1,2,3", new int[] { 1, 2, 3 })]
		[TestCase("1,,3", new int[] { 1, 3 })]
		[TestCase(" 1 , 2 , 3 ", new int[] { 1, 2, 3 })]
		[TestCase(" 1 ,  , 3 ", new int[] { 1, 3 })]
		[TestCase("1,1,2,2,3", new int[] { 1, 1, 2, 2, 3 })]
		[TestCase("1,1,1,1", new int[] { 1, 1, 1, 1 })]
		public void GetValue_List(string input, int[] expected)
		{
			var actual = FieldLoader.GetValue<List<int>>("field", input);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null, new int[] { })]
		[TestCase("", new int[] { })]
		[TestCase("1", new int[] { 1 })]
		[TestCase("1,2,3", new int[] { 1, 2, 3 })]
		[TestCase("1,,3", new int[] { 1, 3 })]
		[TestCase(" 1 , 2 , 3 ", new int[] { 1, 2, 3 })]
		[TestCase(" 1 ,  , 3 ", new int[] { 1, 3 })]
		[TestCase("1,1,2,2,3", new int[] { 1, 2, 3 })]
		[TestCase("1,1,1,1", new int[] { 1 })]
		public void GetValue_HashSet(string input, int[] expected)
		{
			var actual = FieldLoader.GetValue<HashSet<int>>("field", input);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("1")]
		[TestCase("1,2,3")]
		[TestCase("1,,3")]
		[TestCase(" 1 , 2 , 3 ")]
		[TestCase(" 1 ,  , 3 ")]
		[TestCase("1,1,2,2,3")]
		[TestCase("1,1,1,1")]
		public void GetValue_BitSet(string input)
		{
			var actual = FieldLoader.GetValue<BitSet<FieldLoaderTest>>("field", input);
			var expected = input == null
				? new BitSet<FieldLoaderTest>([])
				: new BitSet<FieldLoaderTest>(input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("1")]
		[TestCase("1,2")]
		public void GetValue_Dictionary(string input)
		{
			var actual = FieldLoader.GetValue<Dictionary<int, int>>("field", input);

			Assert.That(actual, Is.Empty);
		}

		[TestCase(null, new int[] { })]
		[TestCase("", new int[] { })]
		[TestCase("1", new int[] { 1 })]
		[TestCase("1,2,3", new int[] { 1, 2, 3 })]
		[TestCase("1,,3", new int[] { 1, 3 })]
		[TestCase(" 1 , 2 , 3 ", new int[] { 1, 2, 3 })]
		[TestCase(" 1 ,  , 3 ", new int[] { 1, 3 })]
		[TestCase("1,1,2,2,3", new int[] { 1, 1, 2, 2, 3 })]
		[TestCase("1,1,1,1", new int[] { 1, 1, 1, 1 })]
		public void GetValue_ImmutableArray(string input, int[] expected)
		{
			var actual = FieldLoader.GetValue<ImmutableArray<int>>("field", input);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null, new int[] { })]
		[TestCase("", new int[] { })]
		[TestCase("1", new int[] { 1 })]
		[TestCase("1,2,3", new int[] { 1, 2, 3 })]
		[TestCase("1,,3", new int[] { 1, 3 })]
		[TestCase(" 1 , 2 , 3 ", new int[] { 1, 2, 3 })]
		[TestCase(" 1 ,  , 3 ", new int[] { 1, 3 })]
		[TestCase("1,1,2,2,3", new int[] { 1, 2, 3 })]
		[TestCase("1,1,1,1", new int[] { 1 })]
		public void GetValue_FrozenSet(string input, int[] expected)
		{
			var actual = FieldLoader.GetValue<FrozenSet<int>>("field", input);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("1")]
		[TestCase("1,2")]
		public void GetValue_FrozenDictionary(string input)
		{
			var actual = FieldLoader.GetValue<FrozenDictionary<int, int>>("field", input);

			Assert.That(actual, Is.Empty);
		}

		[TestCase(TypeArgs = [typeof(int[][])])]
		[TestCase(TypeArgs = [typeof(List<List<int>>)])]
		[TestCase(TypeArgs = [typeof(HashSet<HashSet<int>>)])]
		[TestCase(TypeArgs = [typeof(ImmutableArray<ImmutableArray<int>>)])]
		[TestCase(TypeArgs = [typeof(FrozenSet<FrozenSet<int>>)])]
		public void GetValue_NestedCollections<T>()
		{
			var actual = FieldLoader.GetValue<T>("field", "1,2,3");

			Assert.That(actual, Is.EquivalentTo(new int[][] { [1], [2], [3] }));
		}

		[Test]
		public void GetValue_TypeConverter()
		{
			// We don't have hardcoded handling for sbyte, but a TypeConverter for it does exist.
			var actual = FieldLoader.GetValue<sbyte>("field", "  123  ");

			Assert.That(actual, Is.EqualTo(123));
		}

		[Test]
		public void GetValue_TypeConverter_Invalid()
		{
			static void Act() => FieldLoader.GetValue<sbyte>("field", "  test  ");

			Assert.That(Act, Throws.TypeOf<YamlException>().And.Message.EqualTo($"FieldLoader: Cannot parse `test` into `field.{typeof(sbyte).FullName}`"));
		}

		sealed class LoadFieldOrPropertyTarget
		{
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable RCS1170 // Use read-only auto-implemented property
			int privateIntField;
			int PrivateIntProp { get; set; }

			public int PublicIntField;
			public int PublicIntProp { get; set; }

			public int GetPrivateIntField() => privateIntField;
			public int GetPrivateIntProp() => PrivateIntProp;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore RCS1170 // Use read-only auto-implemented property
		}

		[Test]
		public void LoadFieldOrProperty()
		{
			var target = new LoadFieldOrPropertyTarget();
			FieldLoader.LoadFieldOrProperty(target, $"  {nameof(LoadFieldOrPropertyTarget.PublicIntField)}  ", "12");
			FieldLoader.LoadFieldOrProperty(target, $"  {nameof(LoadFieldOrPropertyTarget.PublicIntProp)}  ", "34");
			FieldLoader.LoadFieldOrProperty(target, "  privateIntField  ", "56");
			FieldLoader.LoadFieldOrProperty(target, "  PrivateIntProp  ", "78");
			void Act() => FieldLoader.LoadFieldOrProperty(target, "unknown", "");

			Assert.That(target.PublicIntField, Is.EqualTo(12));
			Assert.That(target.PublicIntProp, Is.EqualTo(34));
			Assert.That(target.GetPrivateIntField(), Is.EqualTo(56));
			Assert.That(target.GetPrivateIntProp(), Is.EqualTo(78));
			Assert.That(Act, Throws.TypeOf<NotImplementedException>().And.Message.EqualTo("FieldLoader: Missing field `unknown` on `LoadFieldOrPropertyTarget`"));
		}

		sealed class LoadTarget
		{
			public int Int;
			public string String;
			public string Unset;
		}

		[Test]
		public void Load()
		{
			var target = new LoadTarget() { Unset = "unset" };
			var yaml = new MiniYaml(
				null,
				[
					new MiniYamlNode(nameof(LoadTarget.Int), "123"),
					new MiniYamlNode(nameof(LoadTarget.String), "test"),
				]);

			FieldLoader.Load(target, yaml);

			Assert.That(target.Int, Is.EqualTo(123));
			Assert.That(target.String, Is.EqualTo("test"));
			Assert.That(target.Unset, Is.EqualTo("unset"));
		}

		[Test]
		public void Load_Generic()
		{
			var expected = new LoadTarget();
			var yaml = new MiniYaml(
				null,
				[
					new MiniYamlNode(nameof(LoadTarget.Int), "123"),
					new MiniYamlNode(nameof(LoadTarget.String), "test"),
				]);
			FieldLoader.Load(expected, yaml);

			var actual = FieldLoader.Load<LoadTarget>(yaml);

			Assert.That(actual.Int, Is.EqualTo(expected.Int));
			Assert.That(actual.String, Is.EqualTo(expected.String));
			Assert.That(actual.Unset, Is.EqualTo(expected.Unset));
		}

		sealed class LoadDictionaryTarget
		{
			public Dictionary<int, int> Dictionary;
		}

		[Test]
		public void Load_Dictionary()
		{
			var target = new LoadDictionaryTarget();
			var yaml = new MiniYaml(
				null,
				[
					new MiniYamlNode(
						nameof(LoadDictionaryTarget.Dictionary),
						new MiniYaml(
							null,
							[
								new MiniYamlNode("12", "34"),
								new MiniYamlNode("56", "78")
							]))
				]);

			FieldLoader.Load(target, yaml);

			Assert.That(target.Dictionary, Is.EquivalentTo(new Dictionary<int, int> { { 12, 34 }, { 56, 78 } }));
		}

		sealed class LoadFrozenDictionaryTarget
		{
			public FrozenDictionary<int, int> Dictionary;
		}

		[Test]
		public void Load_FrozenDictionary()
		{
			var target = new LoadFrozenDictionaryTarget();
			var yaml = new MiniYaml(
				null,
				[
					new MiniYamlNode(
						nameof(LoadFrozenDictionaryTarget.Dictionary),
						new MiniYaml(
							null,
							[
								new MiniYamlNode("12", "34"),
								new MiniYamlNode("56", "78")
							]))
				]);

			FieldLoader.Load(target, yaml);

			Assert.That(target.Dictionary, Is.EquivalentTo(new Dictionary<int, int> { { 12, 34 }, { 56, 78 } }));
		}

		sealed class LoadRequiredTarget
		{
			[FieldLoader.Require]
			public int Int1 = 1;
			[FieldLoader.Require]
			public int Int2 = 2;
			[FieldLoader.Require]
			public int Int3 = 3;

			public int Int4 = 4;
			public int Int5 = 5;
		}

		[Test]
		public void Load_Required()
		{
			var target = new LoadRequiredTarget();
			var yaml = new MiniYaml(
				null,
				[
					new MiniYamlNode(nameof(LoadRequiredTarget.Int1), "123"),
					new MiniYamlNode(nameof(LoadRequiredTarget.Int4), "456"),
				]);

			void Act() => FieldLoader.Load(target, yaml);

			Assert.That(Act,
				Throws.TypeOf<FieldLoader.MissingFieldsException>().And
				.Message.EqualTo($"{nameof(LoadRequiredTarget.Int2)}, {nameof(LoadRequiredTarget.Int3)}"));
			Assert.That(target.Int1, Is.EqualTo(123));
			Assert.That(target.Int2, Is.EqualTo(2));
			Assert.That(target.Int3, Is.EqualTo(3));
			Assert.That(target.Int4, Is.EqualTo(456));
			Assert.That(target.Int5, Is.EqualTo(5));
		}

		sealed class LoadIgnoreTarget
		{
			[FieldLoader.Ignore]
			public int Int1 = 1;
			[FieldLoader.Ignore]
			public int Int2 = 2;

			public int Int3 = 3;
		}

		[Test]
		public void Load_Ignore()
		{
			var target = new LoadIgnoreTarget();
			var yaml = new MiniYaml(
				null,
				[
					new MiniYamlNode(nameof(LoadIgnoreTarget.Int1), "123"),
					new MiniYamlNode(nameof(LoadIgnoreTarget.Int3), "456"),
				]);

			FieldLoader.Load(target, yaml);

			Assert.That(target.Int1, Is.EqualTo(1));
			Assert.That(target.Int2, Is.EqualTo(2));
			Assert.That(target.Int3, Is.EqualTo(456));
		}

		sealed class LoadUsingTarget
		{
			[FieldLoader.LoadUsing(nameof(LoadInt))]
			public int Int1 = 1;
			[FieldLoader.LoadUsing(nameof(LoadInt), true)]
			public int Int2 = 2;

			[FieldLoader.LoadUsing(nameof(LoadInt))]
			public int Int3 = 3;
			[FieldLoader.LoadUsing(nameof(LoadInt), true)]
			public int Int4 = 4;

			public int Int5 = 5;

			static object LoadInt(MiniYaml yaml) => Exts.ParseInt32Invariant(yaml.NodeWithKey("ForLoadUsing").Value.Value);
		}

		[Test]
		public void Load_Using()
		{
			var target = new LoadUsingTarget();
			var yaml = new MiniYaml(
				null,
				[
					new MiniYamlNode("ForLoadUsing", "100"),
					new MiniYamlNode(nameof(LoadUsingTarget.Int1), "12"),
					new MiniYamlNode(nameof(LoadUsingTarget.Int2), "34"),
					new MiniYamlNode(nameof(LoadUsingTarget.Int5), "56"),
				]);

			void Act() => FieldLoader.Load(target, yaml);

			Assert.That(Act,
				Throws.TypeOf<FieldLoader.MissingFieldsException>().And
				.Message.EqualTo(nameof(LoadRequiredTarget.Int4)));
			Assert.That(target.Int1, Is.EqualTo(100));
			Assert.That(target.Int2, Is.EqualTo(100));
			Assert.That(target.Int3, Is.EqualTo(100));
			Assert.That(target.Int4, Is.EqualTo(4));
			Assert.That(target.Int5, Is.EqualTo(56));
		}

		sealed class LoadUsingMissingTarget
		{
			[FieldLoader.LoadUsing("unknown")]
			public int Int = 1;
		}

		[Test]
		public void Load_UsingMissing()
		{
			var target = new LoadUsingMissingTarget();
			var yaml = new MiniYaml(null);

			void Act() => FieldLoader.Load(target, yaml);

			Assert.That(Act,
				Throws.TypeOf<InvalidOperationException>().And
				.Message.EqualTo("LoadUsingMissingTarget does not specify a loader function 'unknown'"));
			Assert.That(target.Int, Is.EqualTo(1));
		}
	}
}
