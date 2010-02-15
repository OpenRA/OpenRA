using System.Xml;
using System.Drawing;
using OpenRa.Gl;
using System.IO;
namespace OpenRa.Graphics
{
	class MappedImage
	{
		readonly Rectangle rect;
		public readonly string Src;
		public readonly string Name;

		public MappedImage(string defaultSrc, XmlElement e)
		{
			Src = (e.HasAttribute("src")) ? e.GetAttribute("src") : defaultSrc;
			Name = e.GetAttribute("name");
			if (Src == null)
				throw new InvalidDataException("Image src missing");

			rect = new Rectangle(int.Parse(e.GetAttribute("x")),
								 int.Parse(e.GetAttribute("y")),
								 int.Parse(e.GetAttribute("width")),
								 int.Parse(e.GetAttribute("height")));
		}

		public Sprite GetImage(Renderer r, Sheet s)
		{
			return new Sprite(s, rect, TextureChannel.Alpha);
		}
	}
}
