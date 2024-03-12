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
using System.Linq;
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
	public sealed class TranslationReferenceAttribute : Attribute
	{
		public readonly bool Optional;
		public readonly string[] RequiredVariableNames;
		public readonly LintDictionaryReference DictionaryReference;

		public TranslationReferenceAttribute() { }

		public TranslationReferenceAttribute(params string[] requiredVariableNames)
		{
			RequiredVariableNames = requiredVariableNames;
		}

		public TranslationReferenceAttribute(LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			DictionaryReference = dictionaryReference;
		}

		public TranslationReferenceAttribute(bool optional)
		{
			Optional = optional;
		}
	}

	public class Translation
	{
		readonly FluentBundle bundle;

		public Translation(string language, string[] translations, IReadOnlyFileSystem fileSystem)
			: this(language, translations, fileSystem, error => Log.Write("debug", error.Message)) { }

		public Translation(string language, string[] translations, IReadOnlyFileSystem fileSystem, Action<ParseError> onError)
		{
			if (translations == null || translations.Length == 0)
				return;

			bundle = LinguiniBuilder.Builder()
				.CultureInfo(new CultureInfo(language))
				.SkipResources()
				.SetUseIsolating(false)
				.UseConcurrent()
				.UncheckedBuild();

			ParseTranslations(language, translations, fileSystem, onError);
		}

		public Translation(string language, string text, Action<ParseError> onError)
		{
			var parser = new LinguiniParser(text);
			var resource = parser.Parse();
			foreach (var error in resource.Errors)
				onError(error);

			bundle = LinguiniBuilder.Builder()
				.CultureInfo(new CultureInfo(language))
				.SkipResources()
				.SetUseIsolating(false)
				.UseConcurrent()
				.UncheckedBuild();

			bundle.AddResourceOverriding(resource);
		}

		void ParseTranslations(string language, string[] translations, IReadOnlyFileSystem fileSystem, Action<ParseError> onError)
		{
			// Always load english strings to provide a fallback for missing translations.
			// It is important to load the english files first so the chosen language's files can override them.
			var paths = translations.Where(t => t.EndsWith("en.ftl", StringComparison.Ordinal)).ToList();
			foreach (var t in translations)
				if (t.EndsWith($"{language}.ftl", StringComparison.Ordinal))
					paths.Add(t);

			foreach (var path in paths.Distinct())
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

		public string GetString(string key, IDictionary<string, object> arguments = null)
		{
			if (!TryGetString(key, out var message, arguments))
				message = key;

			return message;
		}

		public bool TryGetString(string key, out string value, IDictionary<string, object> arguments = null)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			try
			{
				if (!HasMessage(key))
				{
					value = null;
					return false;
				}

				var fluentArguments = new Dictionary<string, IFluentType>();
				if (arguments != null)
					foreach (var (k, v) in arguments)
						fluentArguments.Add(k, v.ToFluentType());

				var result = bundle.TryGetAttrMessage(key, fluentArguments, out var errors, out value);
				foreach (var error in errors)
					Log.Write("debug", $"Translation of {key}: {error}");

				return result;
			}
			catch (Exception)
			{
				Log.Write("debug", $"Failed translation: {key}");

				value = null;
				return false;
			}
		}

		public bool HasMessage(string key)
		{
			return bundle.HasAttrMessage(key);
		}

		// Adapted from Fluent.Net.SimpleExample.TranslationService by Mark Weaver
		public static Dictionary<string, object> Arguments(string name, object value, params object[] args)
		{
			if (args.Length % 2 != 0)
				throw new ArgumentException("Expected a comma separated list of name, value arguments"
					+ " but the number of arguments is not a multiple of two", nameof(args));

			var argumentDictionary = new Dictionary<string, object> { { name, value } };

			for (var i = 0; i < args.Length; i += 2)
			{
				name = args[i] as string;
				if (string.IsNullOrEmpty(name))
					throw new ArgumentException($"Expected the argument at index {i} to be a non-empty string",
						nameof(args));

				value = args[i + 1];
				if (value == null)
					throw new ArgumentNullException(nameof(args), $"Expected the argument at index {i + 1} to be a non-null value");

				argumentDictionary.Add(name, value);
			}

			return argumentDictionary;
		}
	}
}
