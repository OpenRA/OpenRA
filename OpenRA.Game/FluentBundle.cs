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
using System.IO;
using Linguini.Bundle;
using Linguini.Bundle.Builder;
using Linguini.Shared.Types.Bundle;
using Linguini.Syntax.Parser;
using Linguini.Syntax.Parser.Error;
using OpenRA.FileSystem;
using OpenRA.Traits;

namespace OpenRA
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class FluentReferenceAttribute : Attribute
	{
		public readonly bool Optional;
		public readonly string[] RequiredVariableNames;
		public readonly LintDictionaryReference DictionaryReference;

		public FluentReferenceAttribute() { }

		public FluentReferenceAttribute(params string[] requiredVariableNames)
		{
			RequiredVariableNames = requiredVariableNames;
		}

		public FluentReferenceAttribute(LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			DictionaryReference = dictionaryReference;
		}

		public FluentReferenceAttribute(bool optional)
		{
			Optional = optional;
		}
	}

	public class FluentBundle
	{
		readonly Linguini.Bundle.FluentBundle bundle;

		public FluentBundle(string culture, string[] paths, IReadOnlyFileSystem fileSystem)
			: this(culture, paths, fileSystem, error => Log.Write("debug", error.Message)) { }

		public FluentBundle(string culture, string[] paths, IReadOnlyFileSystem fileSystem, string text)
			: this(culture, paths, fileSystem, text, error => Log.Write("debug", error.Message)) { }

		public FluentBundle(string culture, string[] paths, IReadOnlyFileSystem fileSystem, Action<ParseError> onError)
			: this(culture, paths, fileSystem, null, onError) { }

		public FluentBundle(string culture, string text, Action<ParseError> onError)
			: this(culture, null, null, text, onError) { }

		public FluentBundle(string culture, string[] paths, IReadOnlyFileSystem fileSystem, string text, Action<ParseError> onError)
		{
			bundle = LinguiniBuilder.Builder()
				.CultureInfo(new CultureInfo(culture))
				.SkipResources()
				.SetUseIsolating(false)
				.UseConcurrent()
				.UncheckedBuild();

			if (paths != null)
			{
				foreach (var path in paths)
				{
					var stream = fileSystem.Open(path);
					using (var reader = new StreamReader(stream))
					{
						var parser = new LinguiniParser(reader);
						var resource = parser.Parse();
						foreach (var error in resource.Errors)
							onError(error);

						bundle.AddResourceOverriding(resource);
					}
				}
			}

			if (!string.IsNullOrEmpty(text))
			{
				var parser = new LinguiniParser(text);
				var resource = parser.Parse();
				foreach (var error in resource.Errors)
					onError(error);

				bundle.AddResourceOverriding(resource);
			}
		}

		public string GetMessage(string key, object[] args = null)
		{
			if (!TryGetMessage(key, out var message, args))
				message = key;

			return message;
		}

		public bool TryGetMessage(string key, out string value, object[] args = null)
		{
			ArgumentNullException.ThrowIfNull(key);

			try
			{
				if (!HasMessage(key))
				{
					value = null;
					return false;
				}

				Dictionary<string, IFluentType> fluentArgs = null;
				if (args != null)
				{
					if (args.Length % 2 != 0)
						throw new ArgumentException("Expected a comma separated list of name, value arguments " +
							"but the number of arguments is not a multiple of two", nameof(args));

					fluentArgs = [];
					for (var i = 0; i < args.Length; i += 2)
					{
						var argKey = args[i] as string;
						if (string.IsNullOrEmpty(argKey))
							throw new ArgumentException($"Expected the argument at index {i} to be a non-empty string", nameof(args));

						var argValue = args[i + 1];
						if (argValue == null)
							throw new ArgumentNullException(nameof(args), $"Expected the argument at index {i + 1} to be a non-null value");

						fluentArgs.Add(argKey, argValue.ToFluentType());
					}
				}

				var result = bundle.TryGetAttrMessage(key, fluentArgs, out var errors, out value);
				foreach (var error in errors)
					Log.Write("debug", $"FluentBundle of {key}: {error}");

				return result;
			}
			catch (Exception)
			{
				Log.Write("debug", $"FluentBundle of {key}: threw exception");

				value = null;
				return false;
			}
		}

		public bool HasMessage(string key)
		{
			return bundle.HasAttrMessage(key);
		}
	}
}
