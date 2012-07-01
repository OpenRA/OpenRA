#region Copyright & License Information
/*
 * Copyright 2008-2009 http://www.codeproject.com/Members/Harold-Aptroot
 * Source: http://www.codeproject.com/Articles/27992/NAT-Traversal-with-UPnP-in-C
 * This file is licensed under A Public Domain dedication.
 * For more information, see http://creativecommons.org/licenses/publicdomain/
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Xml;
using System.IO;

namespace UPnP
{
	public class NAT
	{
		public static TimeSpan _timeout = new TimeSpan(0, 0, 0, 3);
		static string _serviceUrl;

		public static bool Discover()
		{
			Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
			string req = "M-SEARCH * HTTP/1.1\r\n" +
			"HOST: 239.255.255.250:1900\r\n" +
			"ST:upnp:rootdevice\r\n" +
			"MAN:\"ssdp:discover\"\r\n" +
			"MX:3\r\n\r\n";
			byte[] data = Encoding.ASCII.GetBytes(req);
			IPEndPoint ipe = new IPEndPoint(IPAddress.Broadcast, 1900);
			byte[] buffer = new byte[0x1000];

			DateTime start = DateTime.Now;

			try
			{
				do
				{
					s.SendTo(data, ipe);
	
					int length = 0;
					do
					{
						length = s.Receive(buffer);
	
						string resp = Encoding.ASCII.GetString(buffer, 0, length).ToLower();
						if (resp.Contains("upnp:rootdevice"))
						{
							resp = resp.Substring(resp.ToLower().IndexOf("location:") + 9);
							resp = resp.Substring(0, resp.IndexOf("\r")).Trim();
							if (!string.IsNullOrEmpty(_serviceUrl = GetServiceUrl(resp)))
								return true;
						}
					} while (length > 0);
				} while ((start - DateTime.Now) < _timeout);
			}
			catch (Exception e)
			{
				OpenRA.Log.Write("server", "UPNP discover failed: {0}", e);
			}

			return false;
		}

		private static String GetServiceUrl(string resp)
		{
			XmlDocument desc = new XmlDocument();
			desc.Load(WebRequest.Create(resp).GetResponse().GetResponseStream());
			XmlNamespaceManager nsMgr = new XmlNamespaceManager(desc.NameTable);
			nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
			XmlNode typen = desc.SelectSingleNode("//tns:device/tns:deviceType/text()", nsMgr);
			if (!typen.Value.Contains("InternetGatewayDevice"))
				return null;
			XmlNode node = desc.SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:WANIPConnection:1\"]/tns:controlURL/text()", nsMgr);
			if (node == null)
				return null;
			Uri respUri = new Uri(resp);
			Uri combinedUri = new Uri(respUri, node.Value);
			return combinedUri.AbsoluteUri;
		}

		public static void ForwardPort(int port, ProtocolType protocol, string description)
		{
			if (string.IsNullOrEmpty(_serviceUrl))
				throw new Exception("No UPnP service available or Discover() has not been called");
			string body = String.Format("<u:AddPortMapping xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">"+
						    "<NewRemoteHost></NewRemoteHost><NewExternalPort>{0}</NewExternalPort>"+
						    "<NewProtocol>{1}</NewProtocol><NewInternalPort>{0}</NewInternalPort>" +
						    "<NewInternalClient>{2}</NewInternalClient><NewEnabled>1</NewEnabled>" +
						    "<NewPortMappingDescription>{3}</NewPortMappingDescription>"+
						    "<NewLeaseDuration>0</NewLeaseDuration></u:AddPortMapping>",
						    port, protocol.ToString().ToUpper(),Dns.GetHostAddresses(Dns.GetHostName())[0], description);
			SOAPRequest(_serviceUrl, body, "AddPortMapping");
		}

		public static void DeleteForwardingRule(int port, ProtocolType protocol)
		{
			if (string.IsNullOrEmpty(_serviceUrl))
				throw new Exception("No UPnP service available or Discover() has not been called");
			string body = String.Format("<u:DeletePortMapping xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
						    "<NewRemoteHost></NewRemoteHost><NewExternalPort>{0}</NewExternalPort>"+
						    "<NewProtocol>{1}</NewProtocol></u:DeletePortMapping>", port, protocol.ToString().ToUpper() );
			SOAPRequest(_serviceUrl, body, "DeletePortMapping");
		}

		public static IPAddress GetExternalIP()
		{
			if (string.IsNullOrEmpty(_serviceUrl))
				throw new Exception("No UPnP service available or Discover() has not been called");
			XmlDocument xdoc = SOAPRequest(_serviceUrl, "<u:GetExternalIPAddress xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
						       "</u:GetExternalIPAddress>", "GetExternalIPAddress");
			XmlNamespaceManager nsMgr = new XmlNamespaceManager(xdoc.NameTable);
			nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
			string IP = xdoc.SelectSingleNode("//NewExternalIPAddress/text()", nsMgr).Value;
			return IPAddress.Parse(IP);
		}

		private static XmlDocument SOAPRequest(string url, string soap, string function)
		{
			string req = "<?xml version=\"1.0\"?>" +
			"<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
			"<s:Body>" +
			soap +
			"</s:Body>" +
			"</s:Envelope>";
			WebRequest r = HttpWebRequest.Create(url);
			r.Method = "POST";
			byte[] b = Encoding.UTF8.GetBytes(req);
			r.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:WANIPConnection:1#" + function + "\"");
			r.ContentType = "text/xml; charset=\"utf-8\"";
			r.ContentLength = b.Length;
			r.GetRequestStream().Write(b, 0, b.Length);
			XmlDocument resp = new XmlDocument();
			WebResponse wres = r.GetResponse();
			Stream ress = wres.GetResponseStream();
			resp.Load(ress);
			return resp;
		}
	}
}
