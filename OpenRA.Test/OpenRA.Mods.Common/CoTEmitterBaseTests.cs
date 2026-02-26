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

using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public class CoTEmitterBaseTests
	{
		// SecurityElementEscape tests

		[Test]
		public void SecurityElementEscape_NullReturnsEmpty()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.SecurityElementEscape(null), Is.EqualTo(string.Empty));
		}

		[Test]
		public void SecurityElementEscape_EmptyReturnsEmpty()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.SecurityElementEscape(string.Empty), Is.EqualTo(string.Empty));
		}

		[Test]
		public void SecurityElementEscape_Ampersand()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.SecurityElementEscape("a&b"), Is.EqualTo("a&amp;b"));
		}

		[Test]
		public void SecurityElementEscape_Quote()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.SecurityElementEscape("a\"b"), Is.EqualTo("a&quot;b"));
		}

		[Test]
		public void SecurityElementEscape_Apostrophe()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.SecurityElementEscape("a'b"), Is.EqualTo("a&apos;b"));
		}

		[Test]
		public void SecurityElementEscape_LessThan()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.SecurityElementEscape("a<b"), Is.EqualTo("a&lt;b"));
		}

		[Test]
		public void SecurityElementEscape_GreaterThan()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.SecurityElementEscape("a>b"), Is.EqualTo("a&gt;b"));
		}

		[Test]
		public void SecurityElementEscape_AllSpecialCharacters()
		{
			var result = CoTEmitterBase<CoTVehicleEmitterInfo>.SecurityElementEscape("&\"'<>");
			Assert.That(result, Is.EqualTo("&amp;&quot;&apos;&lt;&gt;"));
		}

		// Clean tests

		[Test]
		public void Clean_NullReturnsEmpty()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.Clean(null), Is.EqualTo(string.Empty));
		}

		[Test]
		public void Clean_EmptyReturnsEmpty()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.Clean(string.Empty), Is.EqualTo(string.Empty));
		}

		[Test]
		public void Clean_TrimsWhitespace()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.Clean("  hello  "), Is.EqualTo("hello"));
		}

		[Test]
		public void Clean_StripsDoubleQuotePair()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.Clean("\"value\""), Is.EqualTo("value"));
		}

		[Test]
		public void Clean_StripsSingleQuotePair()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.Clean("'value'"), Is.EqualTo("value"));
		}

		[Test]
		public void Clean_DoesNotStripMismatchedQuotes()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.Clean("\"value'"), Is.EqualTo("\"value'"));
		}

		[Test]
		public void Clean_PreservesInnerContent()
		{
			Assert.That(CoTEmitterBase<CoTVehicleEmitterInfo>.Clean("  \"a-f-G-U-C\"  "), Is.EqualTo("a-f-G-U-C"));
		}

		// TryGetValueAnyCase tests

		[Test]
		public void TryGetValueAnyCase_NullDictReturnsFalse()
		{
			var result = CoTEmitterBase<CoTVehicleEmitterInfo>.TryGetValueAnyCase<string>(null, "key", out _);
			Assert.That(result, Is.False);
		}

		[Test]
		public void TryGetValueAnyCase_NullKeyReturnsFalse()
		{
			var dict = new Dictionary<string, string> { { "key", "val" } };
			var result = CoTEmitterBase<CoTVehicleEmitterInfo>.TryGetValueAnyCase(dict, null, out _);
			Assert.That(result, Is.False);
		}

		[Test]
		public void TryGetValueAnyCase_ExactMatch()
		{
			var dict = new Dictionary<string, string> { { "Tank", "a-f-G" } };
			var result = CoTEmitterBase<CoTVehicleEmitterInfo>.TryGetValueAnyCase(dict, "Tank", out var val);
			Assert.That(result, Is.True);
			Assert.That(val, Is.EqualTo("a-f-G"));
		}

		[Test]
		public void TryGetValueAnyCase_CaseInsensitiveMatch()
		{
			var dict = new Dictionary<string, string> { { "Tank", "a-f-G" } };
			var result = CoTEmitterBase<CoTVehicleEmitterInfo>.TryGetValueAnyCase(dict, "tank", out var val);
			Assert.That(result, Is.True);
			Assert.That(val, Is.EqualTo("a-f-G"));
		}

		[Test]
		public void TryGetValueAnyCase_UpperCaseLookup()
		{
			var dict = new Dictionary<string, string> { { "tank", "a-f-G" } };
			var result = CoTEmitterBase<CoTVehicleEmitterInfo>.TryGetValueAnyCase(dict, "TANK", out var val);
			Assert.That(result, Is.True);
			Assert.That(val, Is.EqualTo("a-f-G"));
		}

		[Test]
		public void TryGetValueAnyCase_NotFoundReturnsFalse()
		{
			var dict = new Dictionary<string, string> { { "Tank", "a-f-G" } };
			var result = CoTEmitterBase<CoTVehicleEmitterInfo>.TryGetValueAnyCase(dict, "Truck", out _);
			Assert.That(result, Is.False);
		}

		[Test]
		public void TryGetValueAnyCase_EmptyDictReturnsFalse()
		{
			var dict = new Dictionary<string, string>();
			var result = CoTEmitterBase<CoTVehicleEmitterInfo>.TryGetValueAnyCase(dict, "key", out _);
			Assert.That(result, Is.False);
		}

		// CotOutputConfig validation tests

		[Test]
		public void CotOutputConfig_ValidPort()
		{
			var cfg = new CotOutputConfig { Port = 4242 };
			Assert.That(cfg.Port, Is.EqualTo(4242));
		}

		[Test]
		public void CotOutputConfig_InvalidPort_Zero_Throws()
		{
			Assert.That(() => new CotOutputConfig { Port = 0 }, Throws.TypeOf<System.ArgumentOutOfRangeException>());
		}

		[Test]
		public void CotOutputConfig_InvalidPort_Negative_Throws()
		{
			Assert.That(() => new CotOutputConfig { Port = -1 }, Throws.TypeOf<System.ArgumentOutOfRangeException>());
		}

		[Test]
		public void CotOutputConfig_InvalidPort_TooHigh_Throws()
		{
			Assert.That(() => new CotOutputConfig { Port = 65536 }, Throws.TypeOf<System.ArgumentOutOfRangeException>());
		}

		[Test]
		public void CotOutputConfig_ValidMulticastTtl()
		{
			var cfg = new CotOutputConfig { MulticastTtl = 5 };
			Assert.That(cfg.MulticastTtl, Is.EqualTo(5));
		}

		[Test]
		public void CotOutputConfig_MulticastTtl_Null_Allowed()
		{
			var cfg = new CotOutputConfig { MulticastTtl = null };
			Assert.That(cfg.MulticastTtl, Is.Null);
		}

		[Test]
		public void CotOutputConfig_MulticastTtl_Zero_Throws()
		{
			Assert.That(() => new CotOutputConfig { MulticastTtl = 0 }, Throws.TypeOf<System.ArgumentOutOfRangeException>());
		}

		[Test]
		public void CotOutputConfig_MulticastTtl_Negative_Throws()
		{
			Assert.That(() => new CotOutputConfig { MulticastTtl = -1 }, Throws.TypeOf<System.ArgumentOutOfRangeException>());
		}
	}
}
