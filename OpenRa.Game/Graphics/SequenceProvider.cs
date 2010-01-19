using System.Collections.Generic;
using System.Linq;
using System.Xml;
using OpenRa.FileFormats;
using System;

namespace OpenRa.Graphics
{
	static class SequenceProvider
	{
		static Dictionary<string, Dictionary<string, Sequence>> units;
		static Dictionary<string, CursorSequence> cursors;
		
		static Dictionary<string, Dictionary<string, MappedImage>> collections;
		static Dictionary<string, Sheet> cachedSheets;
		static Dictionary<string, Dictionary<string, Sprite>> cachedSprites;
		
		public static void Initialize( bool useAftermath )
		{
			units = new Dictionary<string, Dictionary<string, Sequence>>();
			cursors = new Dictionary<string, CursorSequence>();

			collections = new Dictionary<string, Dictionary<string, MappedImage>>();
			cachedSheets = new Dictionary<string, Sheet>();
			cachedSprites = new Dictionary<string, Dictionary<string, Sprite>>();
		
			LoadSequenceSource("sequences.xml");
			if (useAftermath)
				LoadSequenceSource("sequences-aftermath.xml");
				
			LoadChromeSource("chrome.xml");
		}

		static void LoadSequenceSource(string filename)
		{
			XmlDocument document = new XmlDocument();
			document.Load(FileSystem.Open(filename));

			foreach (XmlElement eUnit in document.SelectNodes("/sequences/unit"))
				LoadSequencesForUnit(eUnit);

			foreach (XmlElement eCursor in document.SelectNodes("/sequences/cursor"))
				LoadSequencesForCursor(eCursor);
		}
		
		static void LoadChromeSource(string filename)
		{
			XmlDocument document = new XmlDocument();
			document.Load(FileSystem.Open(filename)); 
			foreach (XmlElement eCollection in document.SelectNodes("/chrome/collection"))
				LoadChromeForCollection(eCollection);
		}

		static void LoadChromeForCollection(XmlElement eCollection)
		{
			string elementName = eCollection.GetAttribute("name");
			string defaultSrc = (eCollection.HasAttribute("src") ? eCollection.GetAttribute("src") : null);

			var images = eCollection.SelectNodes("./image").OfType<XmlElement>()
				.Select(e => new MappedImage(defaultSrc, e))
				.ToDictionary(s => s.Name);

			collections.Add(elementName, images);
		}
		
		static void LoadSequencesForCursor(XmlElement eCursor)
		{
			string cursorSrc = eCursor.GetAttribute("src");

			foreach (XmlElement eSequence in eCursor.SelectNodes("./sequence"))
				cursors.Add(eSequence.GetAttribute("name"), new CursorSequence(cursorSrc, eSequence));

			Log.Write("* LoadSequencesForCursor() done");
		}

		static void LoadSequencesForUnit(XmlElement eUnit)
		{
			string unitName = eUnit.GetAttribute("name");

			var sequences = eUnit.SelectNodes("./sequence").OfType<XmlElement>()
				.Select(e => new Sequence(unitName, e))
				.ToDictionary(s => s.Name);

			units.Add(unitName, sequences);
		}

		public static Sequence GetSequence(string unitName, string sequenceName)
		{
			try { return units[unitName][sequenceName]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Unit `{0}` does not have a sequence `{1}`".F(unitName, sequenceName));
			}
		}

		public static bool HasSequence(string unit, string seq)
		{
			return units[unit].ContainsKey(seq);
		}

		public static CursorSequence GetCursorSequence(string cursor)
		{
			return cursors[cursor];
		}


		public static Sprite GetImageFromCollection(Renderer renderer,string collection, string image)
		{
			// Cached sprite
			if (cachedSprites.ContainsKey(collection) && cachedSprites[collection].ContainsKey(image))
				return cachedSprites[collection][image];
			
			MappedImage mi;
			try { mi = collections[collection][image];}
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Collection `{0}` does not have an image `{1}`".F(collection, image));
			}
			
			// Cached sheet
			Sheet sheet;
			if (cachedSheets.ContainsKey(mi.Src))
				sheet = cachedSheets[mi.Src];
			else
			{
				sheet = new Sheet(renderer, mi.Src);
				cachedSheets.Add(mi.Src, sheet);
			}
			
			// Cache the sprite
			if (!cachedSprites.ContainsKey(collection))
				cachedSprites.Add(collection, new Dictionary<string,Sprite>());
			cachedSprites[collection].Add(image, mi.GetImage(renderer, sheet));

			return cachedSprites[collection][image];
		}
	}
}
