using System.Collections.Generic;
using System.Linq;

namespace OpenRA
{
	public class Translator
	{
		public Dictionary<string, FontData> CurrentFontList { get; private set; }

		public void LoadTranslations(ModData modData)
		{
			var selectedTranslations = new Dictionary<string, string>();
			var defaultTranslations = new Dictionary<string, string>();

			var selectedFonts = new Dictionary<string, FontData>();
			var defaultFonts = new Dictionary<string, FontData>();

			if (!modData.Manifest.Translations.Any())
			{
				modData.Languages = new string[0];
				return;
			}

			var yaml = MiniYaml.Load(modData.DefaultFileSystem, modData.Manifest.Translations, null);
			modData.Languages = yaml.Select(t => t.Key).ToArray();

			MiniYaml temp;

			foreach (var language in yaml)
			{
				if (language.Key == Game.Settings.Graphics.Language)
				{
					if (language.Value.ToDictionary().TryGetValue("Translations", out temp))
					{
						foreach (var cls in temp.Nodes)
						{
							foreach (var item in cls.Value.Nodes)
							{
								if (selectedTranslations.ContainsKey(cls.Key + ":" + item.Key))
								{
									continue;
								}
								else
								{
									selectedTranslations.Add(cls.Key + ":" + item.Key, item.Value.Value.Replace("\\n", "\n") ?? string.Empty);
								}
							}
						}
					}

					if (language.Value.ToDictionary().TryGetValue("Fonts", out temp))
					{
						foreach (var node in temp.Nodes)
						{
							selectedFonts.Add(node.Key, FieldLoader.Load<FontData>(node.Value));
						}
					}
				}

				if (language.Key == Game.Settings.Graphics.DefaultLanguage)
				{
					if (language.Value.ToDictionary().TryGetValue("Translations", out temp))
					{
						foreach (var cls in temp.Nodes)
						{
							foreach (var item in cls.Value.Nodes)
							{
								if (defaultTranslations.ContainsKey(cls.Key + ":" + item.Key))
								{
									continue;
								}
								else
								{
									defaultTranslations.Add(cls.Key + ":" + item.Key, item.Value.Value.Replace("\\n", "\n") ?? string.Empty);
								}
							}
						}
					}

					if (language.Value.ToDictionary().TryGetValue("Fonts", out temp))
					{
						foreach (var node in temp.Nodes)
						{
							defaultFonts.Add(node.Key, FieldLoader.Load<FontData>(node.Value));
						}
					}
				}
			}

			var translations = new Dictionary<string, string>();
			foreach (var tkv in defaultTranslations.Concat(selectedTranslations))
			{
				if (translations.ContainsKey(tkv.Key))
				{
					continue;
				}

				if (selectedTranslations.ContainsKey(tkv.Key))
				{
					translations.Add(tkv.Key, selectedTranslations[tkv.Key]);
				}
				else
				{
					translations.Add(tkv.Key, tkv.Value);
				}
			}

			FieldLoader.SetTranslations(translations);
			CurrentFontList = selectedFonts.Count > 0 ? selectedFonts : defaultFonts;
		}
	}
}
