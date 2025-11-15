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
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class FieldSaverTest
	{
		static IEnumerable<TestCaseData> FormatValue_Primitive_TestCases()
		{
			return
			[
				new TestCaseData(123),
				new TestCaseData((ushort)123),
				new TestCaseData(123L),
				new TestCaseData(123.4f),
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
				new TestCaseData(new BooleanExpression("true")),
				new TestCaseData(new IntegerExpression("1 + 2")),
				new TestCaseData(MapGridType.RectangularIsometric),
				new TestCaseData(SystemActors.World | SystemActors.EditorWorld),
				new TestCaseData(true),
				new TestCaseData(new Size(123, 456)),
				new TestCaseData(new int2(123, 456)),
				new TestCaseData(new float2(123, 456)),
				new TestCaseData(new float3(123, 456, 789)),
				new TestCaseData(new Rectangle(123, 456, 789, 123)),
			];
		}

		[TestCaseSource(nameof(FormatValue_Primitive_TestCases))]
		public void FormatVaue_Primitive<T>(T expected)
		{
			var actual = FieldSaver.FormatValue(expected);

			Assert.That(actual, Is.EqualTo(expected.ToString()));
		}

		[Test]
		public void FormatValue_Null()
		{
			var actual = FieldSaver.FormatValue(null);

			Assert.That(actual, Is.EqualTo(""));
		}

		[Test]
		public void FormatValue_DateTime()
		{
			var input = new DateTime(2000, 1, 1);

			var actual = FieldSaver.FormatValue(input);

			Assert.That(actual, Is.EqualTo(input.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture)));
		}

		[Test]
		public void FormatValue_WVecArray()
		{
			var actual = FieldSaver.FormatValue(new WVec[] { new(1, 2, 3), new(4, 5, 6) });

			Assert.That(actual, Is.EqualTo("1,2,3, 4,5,6"));
		}

		[Test]
		public void FormatValue_CPosArray()
		{
			var actual = FieldSaver.FormatValue(new CPos[] { new(1, 2), new(3, 4), new(5, 6) });

			Assert.That(actual, Is.EqualTo("1,2, 3,4, 5,6"));
		}

		[Test]
		public void FormatValue_CVecArray()
		{
			var actual = FieldSaver.FormatValue(new CVec[] { new(1, 2), new(3, 4), new(5, 6) });

			Assert.That(actual, Is.EqualTo("1,2, 3,4, 5,6"));
		}

		[Test]
		public void FormatValue_int2Array()
		{
			var actual = FieldSaver.FormatValue(new int2[] { new(1, 2), new(3, 4), new(5, 6) });

			Assert.That(actual, Is.EqualTo("1,2, 3,4, 5,6"));
		}

		[TestCase(null, "")]
		[TestCase(123, "123")]
		public void FormatValue_Nullable(int? input, string expected)
		{
			var actual = FieldSaver.FormatValue(input);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null, "")]
		[TestCase(new int[] { }, "")]
		[TestCase(new int[] { 1 }, "1")]
		[TestCase(new int[] { 1, 2, 3 }, "1, 2, 3")]
		[TestCase(new int[] { 1, 1, 2, 2, 3 }, "1, 1, 2, 2, 3")]
		[TestCase(new int[] { 1, 1, 1, 1 }, "1, 1, 1, 1")]
		public void FormatValue_Array(int[] input, string expected)
		{
			var actual = FieldSaver.FormatValue(input);

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null, "")]
		[TestCase(new int[] { }, "")]
		[TestCase(new int[] { 1 }, "1")]
		[TestCase(new int[] { 1, 2, 3 }, "1, 2, 3")]
		[TestCase(new int[] { 1, 1, 2, 2, 3 }, "1, 1, 2, 2, 3")]
		[TestCase(new int[] { 1, 1, 1, 1 }, "1, 1, 1, 1")]
		public void FormatValue_List(int[] input, string expected)
		{
			var actual = FieldSaver.FormatValue(input?.ToList());

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase(null, "")]
		[TestCase(new int[] { }, "")]
		[TestCase(new int[] { 1 }, "1")]
		[TestCase(new int[] { 1, 2, 3 }, "1, 2, 3")]
		[TestCase(new int[] { 1, 1, 2, 2, 3 }, "1, 2, 3")]
		[TestCase(new int[] { 1, 1, 1, 1 }, "1")]
		public void FormatValue_HashSet(int[] input, string expected)
		{
			var actual = FieldSaver.FormatValue(input?.ToHashSet());

			Assert.That(actual, Is.EqualTo(expected));
		}

		[TestCase("")]
		[TestCase("1")]
		[TestCase("1,2,3")]
		[TestCase("1,1,2,2,3")]
		[TestCase("1,1,1,1")]
		public void FormatValue_BitSet(string input)
		{
			var actual = FieldSaver.FormatValue(new BitSet<FieldSaverTest>(input));

			Assert.That(actual, Is.EqualTo(input));
		}

		[Test]
		public void FormatValue_Dictionary()
		{
			var input = new Dictionary<int, int> { { 12, 34 }, { 56, 78 } };

			var actual = FieldSaver.FormatValue(input);

			Assert.That(actual, Is.EqualTo($"12: 34{Environment.NewLine}56: 78{Environment.NewLine}"));
		}

		sealed class FieldTarget
		{
			public int Int = 123;
		}

		[Test]
		public void FormatValue_Field()
		{
			var actual = FieldSaver.FormatValue(new FieldTarget(), typeof(FieldTarget).GetField(nameof(FieldTarget.Int)));

			Assert.That(actual, Is.EqualTo("123"));
		}

		[Test]
		public void SaveField()
		{
			var actual = FieldSaver.SaveField(new FieldTarget(), nameof(FieldTarget.Int));

			var actualString = MiniYamlExts.WriteToString([actual]);
			var expectedString = MiniYamlExts.WriteToString([new MiniYamlNode(nameof(FieldTarget.Int), "123")]);
			Assert.That(actualString, Is.EqualTo(expectedString));
		}

		sealed class SaveTarget
		{
			public int Int = 123;
			public string String = "test";
			public int[] IntArray = [1, 2, 3];
			public Dictionary<string, string> StringDictionary = new() { { "a", "b" }, { "c", "d" } };
		}

		[Test]
		public void Save()
		{
			var expected = new MiniYaml(
				null,
				[
					new MiniYamlNode(nameof(SaveTarget.Int), "123"),
					new MiniYamlNode(nameof(SaveTarget.String), "test"),
					new MiniYamlNode(nameof(SaveTarget.IntArray), "1, 2, 3"),
					new MiniYamlNode(
						nameof(SaveTarget.StringDictionary),
						new MiniYaml(null, [new MiniYamlNode("a", "b"), new MiniYamlNode("c", "d")])),
				]);

			var actual = FieldSaver.Save(new SaveTarget());

			var actualString = MiniYamlExts.WriteToString(actual.Nodes);
			var expectedString = MiniYamlExts.WriteToString(expected.Nodes);
			Assert.That(actual.Value, Is.EqualTo(expected.Value));
			Assert.That(actualString, Is.EqualTo(expectedString));
		}

		[Test]
		public void SaveDifferences()
		{
			var expected = new MiniYaml(
				null,
				[
					new MiniYamlNode(nameof(SaveTarget.String), ""),
					new MiniYamlNode(nameof(SaveTarget.IntArray), "1, 2, 4"),
				]);

			var actual = FieldSaver.SaveDifferences(new SaveTarget { IntArray = [1, 2, 4], String = null }, new SaveTarget());

			var actualString = MiniYamlExts.WriteToString(actual.Nodes);
			var expectedString = MiniYamlExts.WriteToString(expected.Nodes);
			Assert.That(actual.Value, Is.EqualTo(expected.Value));
			Assert.That(actualString, Is.EqualTo(expectedString));
		}

		[Test]
		public void SaveDifferences_DifferentTypes()
		{
			static void Act() => FieldSaver.SaveDifferences(new object(), new SaveTarget());

			Assert.That(Act, Throws.TypeOf<InvalidOperationException>().And.Message.EqualTo("FieldSaver: can't diff objects of different types"));
		}
	}
}
