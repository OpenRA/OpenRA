using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using OpenRa.FileFormats;

namespace SequenceConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			var xmlfile = args[0];
			var inifile = args[1];

			var doc = new XmlDocument();
			doc.Load(xmlfile);

			var ini = new IniWriter(inifile);

			foreach (var e in doc.SelectNodes(".//unit/sequence").Cast<XmlElement>())
				ini.Set(
					string.Format("Sequence.{0}",
						(e.ParentNode as XmlElement).GetAttribute("name")),
					e.GetAttribute("name"),
					BuildSequenceValue(e));
		}

		static string BuildSequenceValue(XmlElement e)
		{
			var range = string.Format(
				"{0},{1}",
				e.GetAttribute("start"),
				e.HasAttribute("length") ? e.GetAttribute("length") : "1");

			return e.HasAttribute("src")
				? range + "," + e.GetAttribute("src")
				: range;
		}
	}
}
