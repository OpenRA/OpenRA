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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OpenRA
{
	public static class CryptoUtil
	{
		// Fixed byte pattern for the OID header
		static readonly byte[] OIDHeader = { 0x30, 0xD, 0x6, 0x9, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0xD, 0x1, 0x1, 0x1, 0x5, 0x0 };

		public static string PublicKeyFingerprint(RSAParameters parameters)
		{
			// Public key fingerprint is defined as the SHA1 of the modulus + exponent bytes
			return SHA1Hash(parameters.Modulus.Append(parameters.Exponent).ToArray());
		}

		public static string EncodePEMPublicKey(RSAParameters parameters)
		{
			var data = Convert.ToBase64String(EncodePublicKey(parameters));
			var output = new StringBuilder();
			output.AppendLine("-----BEGIN PUBLIC KEY-----");
			for (var i = 0; i < data.Length; i += 64)
				output.AppendLine(data.Substring(i, Math.Min(64, data.Length - i)));
			output.Append("-----END PUBLIC KEY-----");

			return output.ToString();
		}

		public static RSAParameters DecodePEMPublicKey(string key)
		{
			try
			{
				// Reconstruct original key data
				var lines = key.Split('\n');
				var data = Convert.FromBase64String(lines.Skip(1).Take(lines.Length - 2).JoinWith(""));

				// Pull the modulus and exponent bytes out of the ASN.1 tree
				// Expect this to blow up if the key is not correctly formatted
				using (var s = new MemoryStream(data))
				{
					// SEQUENCE
					s.ReadByte();
					ReadTLVLength(s);

					// SEQUENCE -> fixed header junk
					s.ReadByte();
					var headerLength = ReadTLVLength(s);
					s.Position += headerLength;

					// SEQUENCE -> BIT_STRING
					s.ReadByte();
					ReadTLVLength(s);
					s.ReadByte();

					// SEQUENCE -> BIT_STRING -> SEQUENCE
					s.ReadByte();
					ReadTLVLength(s);

					// SEQUENCE -> BIT_STRING -> SEQUENCE -> INTEGER (modulus)
					s.ReadByte();
					var modulusLength = ReadTLVLength(s);
					s.ReadByte();
					var modulus = s.ReadBytes(modulusLength - 1);

					// SEQUENCE -> BIT_STRING -> SEQUENCE -> INTEGER (exponent)
					s.ReadByte();
					var exponentLength = ReadTLVLength(s);
					s.ReadByte();
					var exponent = s.ReadBytes(exponentLength - 1);

					return new RSAParameters
					{
						Modulus = modulus,
						Exponent = exponent
					};
				}
			}
			catch (Exception e)
			{
				throw new InvalidDataException("Invalid PEM public key", e);
			}
		}

		static byte[] EncodePublicKey(RSAParameters parameters)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream);

				var modExpLength = TripletFullLength(parameters.Modulus.Length + 1) + TripletFullLength(parameters.Exponent.Length + 1);
				var bitStringLength = TripletFullLength(modExpLength + 1);
				var sequenceLength = TripletFullLength(bitStringLength + OIDHeader.Length);

				// SEQUENCE
				writer.Write((byte)0x30);
				WriteTLVLength(writer, sequenceLength);

				// SEQUENCE -> fixed header junk
				writer.Write(OIDHeader);

				// SEQUENCE -> BIT_STRING
				writer.Write((byte)0x03);
				WriteTLVLength(writer, bitStringLength);
				writer.Write((byte)0x00);

				// SEQUENCE -> BIT_STRING -> SEQUENCE
				writer.Write((byte)0x30);
				WriteTLVLength(writer, modExpLength);

				// SEQUENCE -> BIT_STRING -> SEQUENCE -> INTEGER
				// Modulus is padded with a zero to avoid issues with the sign bit
				writer.Write((byte)0x02);
				WriteTLVLength(writer, parameters.Modulus.Length + 1);
				writer.Write((byte)0);
				writer.Write(parameters.Modulus);

				// SEQUENCE -> BIT_STRING -> SEQUENCE -> INTEGER
				// Exponent is padded with a zero to avoid issues with the sign bit
				writer.Write((byte)0x02);
				WriteTLVLength(writer, parameters.Exponent.Length + 1);
				writer.Write((byte)0);
				writer.Write(parameters.Exponent);

				return stream.ToArray();
			}
		}

		static void WriteTLVLength(BinaryWriter writer, int length)
		{
			if (length < 0x80)
			{
				// Length < 128 is stored in a single byte
				writer.Write((byte)length);
			}
			else
			{
				// If 128 <= length < 256**128 first byte encodes number of bytes required to hold the length
				// High-bit is set as a flag to use this long-form encoding
				var lengthBytes = BitConverter.GetBytes(length).Reverse().SkipWhile(b => b == 0).ToArray();
				writer.Write((byte)(0x80 | lengthBytes.Length));
				writer.Write(lengthBytes);
			}
		}

		static int ReadTLVLength(Stream s)
		{
			var length = s.ReadByte();
			if (length < 0x80)
				return length;

			var data = new byte[4];
			s.ReadBytes(data, 0, Math.Min(length & 0x7F, 4));
			return BitConverter.ToInt32(data.ToArray(), 0);
		}

		static int TripletFullLength(int dataLength)
		{
			if (dataLength < 0x80)
				return 2 + dataLength;

			return 2 + dataLength + BitConverter.GetBytes(dataLength).Reverse().SkipWhile(b => b == 0).Count();
		}

		public static string DecryptString(RSAParameters parameters, string data)
		{
			try
			{
				using (var rsa = new RSACryptoServiceProvider())
				{
					rsa.ImportParameters(parameters);
					return Encoding.UTF8.GetString(rsa.Decrypt(Convert.FromBase64String(data), false));
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to decrypt string with exception: {0}", e);
				Console.WriteLine("String decryption failed: {0}", e);
				return null;
			}
		}

		public static string Sign(RSAParameters parameters, string data)
		{
			return Sign(parameters, Encoding.UTF8.GetBytes(data));
		}

		public static string Sign(RSAParameters parameters, byte[] data)
		{
			try
			{
				using (var rsa = new RSACryptoServiceProvider())
				{
					rsa.ImportParameters(parameters);
					using (var csp = SHA1.Create())
						return Convert.ToBase64String(rsa.SignHash(csp.ComputeHash(data), CryptoConfig.MapNameToOID("SHA1")));
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to sign string with exception: {0}", e);
				Console.WriteLine("String signing failed: {0}", e);
				return null;
			}
		}

		public static bool VerifySignature(RSAParameters parameters, string data, string signature)
		{
			return VerifySignature(parameters, Encoding.UTF8.GetBytes(data), signature);
		}

		public static bool VerifySignature(RSAParameters parameters, byte[] data, string signature)
		{
			try
			{
				using (var rsa = new RSACryptoServiceProvider())
				{
					rsa.ImportParameters(parameters);
					using (var csp = SHA1.Create())
						return rsa.VerifyHash(csp.ComputeHash(data), CryptoConfig.MapNameToOID("SHA1"), Convert.FromBase64String(signature));
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to verify signature with exception: {0}", e);
				Console.WriteLine("Signature validation failed: {0}", e);
				return false;
			}
		}

		public static string SHA1Hash(Stream data)
		{
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(data).SelectMany(a => a.ToString("x2")).ToArray());
		}

		public static string SHA1Hash(byte[] data)
		{
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(data).SelectMany(a => a.ToString("x2")).ToArray());
		}

		public static string SHA1Hash(string data)
		{
			return SHA1Hash(Encoding.UTF8.GetBytes(data));
		}
	}
}
