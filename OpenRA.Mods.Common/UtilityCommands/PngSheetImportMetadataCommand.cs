#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using System.Net;
using System.Text;
using ICSharpCode.SharpZipLib.Checksums;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class PngSheetImportMetadataCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--png-sheet-import"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2;
		}

		[Desc("PNGFILE", "Import yaml metadata to png")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: This could eventually be merged into the Png class (add a Write method), however this requires complex png writing algorythms.
			var rs = File.OpenRead(args[1]);
			var ws = new MemoryStream();

			using (var bw = new BinaryWriter(ws))
			{
				bw.Write(rs.ReadBytes(8));
				var crc32 = new Crc32();

				for (;;)
				{
					var length = IPAddress.NetworkToHostOrder(rs.ReadInt32());
					var type = Encoding.UTF8.GetString(rs.ReadBytes(4));
					var content = rs.ReadBytes(length);
					var crc = rs.ReadUInt32();

					switch (type)
					{
						case "tEXt":
							break;

						case "IEND":
							rs.Close();

							foreach (var node in MiniYaml.FromFile(Path.ChangeExtension(args[1], "yaml")))
							{
								bw.Write(IPAddress.NetworkToHostOrder(node.Key.Length + 1 + node.Value.Value.Length));
								bw.Write("tEXt".ToCharArray());
								bw.Write(node.Key.ToCharArray());
								bw.Write((byte)0x00);
								bw.Write(node.Value.Value.ToCharArray());
								crc32.Reset();
								crc32.Update(Encoding.ASCII.GetBytes("tEXt"));
								crc32.Update(Encoding.ASCII.GetBytes(node.Key + (char)0x00 + node.Value.Value));
								bw.Write((uint)IPAddress.NetworkToHostOrder((int)crc32.Value));
							}

							bw.Write(0);
							bw.Write(type.ToCharArray());
							bw.Write(crc);

							File.WriteAllBytes(args[1], ws.ToArray());
							ws.Close();
							return;

						default:
							bw.Write(IPAddress.NetworkToHostOrder(length));
							bw.Write(type.ToCharArray());
							bw.Write(content);
							bw.Write(crc);
							break;
					}
				}
			}
		}
	}
}
