using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;

namespace Microsoft.WebSolutionsPlatform.Event
{
	internal class RouteNode
	{
		private string id;
		public string Id
		{
			get
			{
				return id;
			}
		}

		private IPAddress ipAddress;
		public IPAddress IpAddress
		{
			get
			{
				return ipAddress;
			}
		}

		private int port;
		public int Port
		{
			get
			{
				return port;
			}
		}

		private IPEndPoint endPoint;
		public IPEndPoint EndPoint
		{
			get
			{
				return endPoint;
			}
		}

		private bool routeAllEvents;
		public bool RouteAllEvents
		{
			get
			{
				return routeAllEvents;
			}

			set
			{
				routeAllEvents = value;
			}
		}

		public Dictionary<int, bool> routedEvents;
		public Dictionary<int, bool> RoutedEvents
		{
			get
			{
				return routedEvents;
			}
		}

		public RouteNode( IPAddress ipAddress, int port )
		{
			this.ipAddress = ipAddress;
			this.port = port;

			endPoint = new IPEndPoint(ipAddress, port);

			routeAllEvents = true;
			routedEvents = new Dictionary<int, bool>();
		}
	}
}
