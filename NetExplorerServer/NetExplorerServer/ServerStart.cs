﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetExplorerServer
{
    internal class ServerStart
    {
        public string DefaultRoot { get; set; }
        private const int Port = 21;
        private TcpListener _serverListiner;


        public ServerStart(string root)
        {
            DefaultRoot = root;
        }

        public void Start()
        {
            _serverListiner = new TcpListener(IPAddress.Any, Port);
            _serverListiner.Start();
            while (true)
            {
                TcpClient serverClient = _serverListiner.AcceptTcpClient();
                FtpBackend ftpBackend = new FtpBackend(serverClient);
                new Thread(ftpBackend.HandleFtp).Start();
            }
        }
    }
}
