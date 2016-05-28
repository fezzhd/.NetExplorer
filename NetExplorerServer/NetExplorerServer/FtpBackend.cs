﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace NetExplorerServer
{
    internal class FtpBackend
    {
        private readonly TcpClient _commandClient;
        private TcpClient   _dataClient;
        private readonly NetworkStream _commandNetworkStream;
        public static NetworkStream DataNetworkStream;
        private StreamWriter _commandStreamWriter;
        private StreamReader _commadStreamReader;
        private ushort _clientPort;
        private string _ipAdress;
        private DirectoriesBackend _directoriesBackend;
        private Thread _fileThread;
        public static string TempPath;

        public FtpBackend(TcpClient commandClient)
        {
            _commandClient = commandClient;
            _commandNetworkStream = _commandClient.GetStream();
            _commandStreamWriter = new StreamWriter(_commandNetworkStream);
            _commadStreamReader = new StreamReader(_commandNetworkStream);
            _directoriesBackend = new DirectoriesBackend();
        }

        public void HandleFtp()
        {
            _commandStreamWriter.WriteLine("220 OK");
            Console.WriteLine("220 OK");
            _commandStreamWriter.Flush();

            try
            {
                string commandLine;
                while (!string.IsNullOrEmpty(commandLine = _commadStreamReader.ReadLine()))
                {
                    string commandResponse = null;
                    string[] commandStringsArray = commandLine.Split(' ');
                    string realCommand = commandStringsArray[0].ToUpperInvariant();
                    string arguments = commandStringsArray.Length > 1
                        ? commandLine.Substring(commandStringsArray[0].Length + 1)
                        : null;//TODO make array
                    if (string.IsNullOrWhiteSpace(arguments))
                    {
                        arguments = null;
                        
                    }
                    switch (realCommand)
                    {
                        case "USER":
                            commandResponse = HandleUser(arguments);
                            break;
                        case "QUIT":
                            CloseConnetcion();
                            break;
                        case "PASS":
                            commandResponse = HandlePass();
                            break;
                        case "TYPE":
                            commandResponse = HandleType(arguments);
                            break;
                        case "PWD":
                            commandResponse = HandlePwd();
                            break;
                        case "PORT":
                            commandResponse = HandlePort(arguments);
                            break;
                        case "LIST":
                            commandResponse = HandleList();
                            break;
                        case "CWD":
                            commandResponse = HandleCwd(arguments);
                            break;
                        case "CDUP":
                            commandResponse = HandleCdup();
                            break;
                        case "MKD":
                            commandResponse = HandleMkd(arguments);
                            break;
                        case "RMD":
                            commandResponse = HandleRmd(arguments);
                            break;
                        case "DELE":
                            commandResponse = HandleDele(arguments);
                            break;
                        case "RETR":
                            commandResponse = HandleRetr(arguments);
                            break;
                        case "STOR":
                            commandResponse = HandleStor(arguments);
                            break;
                        case "NOOP":
                            commandResponse = "200 OK";
                            break;
                        case "AROR":
                            commandResponse = HandleAbor();
                            break;
                        default:
                            commandResponse = "502 command not implemented\n";
                            break;
                    }

                    if ((_commandClient == null) || !_commandClient.Connected)
                    {
                        break;
                    }
                    else
                    {
                        _commandStreamWriter.WriteLine(commandResponse);
                        Console.WriteLine("{0} : {1} ", realCommand, commandResponse);
                        _commandStreamWriter.Flush();
                        if(commandResponse != null && commandResponse.StartsWith("221"))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private string HandleUser(string argument)
        {
            if(argument.ToLower().Equals("admin"))
            {
                return "311 Admin access allowed";
            }
            return "530 Not allowed user";//todo make user check
        }

        private void CloseConnetcion()
        {
            _commandNetworkStream.Close();
        }

        private string HandlePass()
        {
            return "230 login success";
        }

        private string HandleType(string argument)
        {
            string response;
            switch (argument)
            {
                case "I":
                case "A":
                    response = "220 OK\n";
                    break;
                default:
                    response = "501 Unknow argument";
                    break;
            }
            return response;
        }

        private string HandlePort(string argument)
        {
            string[] ipEndPoint = argument.Split(',');
            _ipAdress = ipEndPoint[0] + '.' + ipEndPoint[1] + '.' + ipEndPoint[2] + '.' + ipEndPoint[3];
            byte[] portByte =
            {
                Convert.ToByte(ipEndPoint[4]), Convert.ToByte((ipEndPoint[5]))
            };
            _clientPort = (ushort) ((portByte[0] << 8) | portByte[1]);
            return "200 active";
        }

        private string HandlePwd()
        {
            if (_directoriesBackend == null)
            {
                _directoriesBackend = new DirectoriesBackend();
            }
            return "257 \"" + _directoriesBackend.CurrentDirectory + "\" is currentd directory";
        }

        private string HandleList()
        {
            _commandStreamWriter.WriteLine("150 ready to send\n");
            _commandStreamWriter.Flush();
            DataNetworkStream = CreateNetworkStream();
            _directoriesBackend.GetList(DataNetworkStream);
            return "226 transfer complete";
        }

        private NetworkStream CreateNetworkStream()
        {
            if ((_dataClient != null) && (_dataClient.Connected))
            {
                _dataClient.Close();
            }
            try
            {
                _dataClient = new TcpClient(_ipAdress, _clientPort);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
            return _dataClient.GetStream();
        }


        private string HandleCwd(string argument)
        {
            _directoriesBackend.ChangeDirectory(argument);
            return "250 directiry changed";
        }


        private string HandleCdup()
        {
            try
            {
                _directoriesBackend.HandleCdup();
            }
            catch (Exception)
            {
                return "552 Error changing directory";
            }
            return "250 directory changed";
        }

        private string HandleRetr(string path)
        {
            TempPath = path;
            _commandStreamWriter.WriteLine("150 ready to send\n");
            _commandStreamWriter.Flush();
            DataNetworkStream = CreateNetworkStream();
            _fileThread = new Thread(_directoriesBackend.SendFile);
            _fileThread.Start();
            return "226 Transfer complited";
        }

        private string HandleStor(string path)
        {
            TempPath = path;
            _commandStreamWriter.WriteLine("150 ready to recieve\n");
            _commandStreamWriter.Flush();
            DataNetworkStream = CreateNetworkStream();
            _fileThread = new Thread(_directoriesBackend.GetFile);
            _fileThread.Start();
            return "226 upload complete";
        }

        private string HandleAbor()
        {
            DataNetworkStream.Close();
            _fileThread.Abort();
            return "226 transfer abort";
        }

        private string HandleDele(string path)
        {
            _directoriesBackend.DeleteFile(path);
            return "250 file deleted";
        }


        private string HandleRmd(string path)
        {
            _directoriesBackend.DeleteDirectory(path);
            return "250 directory deleted";
        }

        private string HandleMkd(string path)
        {
            _directoriesBackend.CreateDirectory(path);
            return "250 directory created";
        }
    }
}
