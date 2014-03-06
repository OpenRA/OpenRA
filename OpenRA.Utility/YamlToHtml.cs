using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenRA.FileFormats;

namespace OpenRA.Utility
{
	class YamlToHtml
	{
		public void WriteYamlNode(StreamWriter sw, MiniYamlNode node)
		{
			sw.WriteLine("<div class='node {0}'><div class='key'>{0}</div>", node.Key);

			WriteYaml(sw, node.Value);

			sw.WriteLine("</div>");
		}

		public void WriteYamlNodeList(StreamWriter sw, List<MiniYamlNode> nodes)
		{
			foreach (var node in nodes)
			{
				WriteYamlNode(sw, node);
			}
		}

		public void WriteYaml(StreamWriter sw, MiniYaml yaml)
		{
			sw.WriteLine("<div class='value'>{0}</div>", yaml.Value);

			WriteYamlNodeList(sw, yaml.Nodes);
		}

		public void WriteYamlFile(StreamWriter sw, string file)
		{
			sw.WriteLine("<div class='file'>");
			List<MiniYamlNode> yamlFile = MiniYaml.FromFile(file);
			WriteYamlNodeList(sw, yamlFile);
			sw.WriteLine("</div>");
		}

		public void ProcessDirectory(string dir, string output, bool recursive = true)
		{
			if (!Directory.Exists(dir))
				throw new ArgumentException(dir + " doesn't exist.");

			if (!Directory.Exists(output))
				Directory.CreateDirectory(output);

			
			string[] files = Directory.GetFiles(dir);
			foreach (var file in files.Where(f => f.EndsWith(".yaml")))
			{

				if (!Directory.Exists(Path.Combine(output,file.Substring(0, file.LastIndexOf(Path.DirectorySeparatorChar)))))
					Directory.CreateDirectory(Path.Combine(output, file.Substring(0, file.LastIndexOf(Path.DirectorySeparatorChar))));
				using (StreamWriter sw = new StreamWriter(Path.Combine(output, file.Substring(0, file.IndexOf(".yaml"))) + ".html"))
				{
					WriteYamlFile(sw, file);
				}
			}
			if (recursive)
			{
				string[] directories = Directory.GetDirectories(dir);
				foreach (var d in directories)
				{
					ProcessDirectory(d, output, recursive);
				}
			}
		}

		public void Run(string where, string output = "html", bool recursive = true)
		{
			ProcessDirectory(where, output, recursive);
		}
	}
}
