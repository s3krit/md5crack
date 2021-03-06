﻿// Licensed under Stallman's beard

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO;

namespace server
{
    class MainClass
    {
        static bool isMD5(string s)
        {
            if (!String.IsNullOrEmpty(s))
            {
                if (Regex.IsMatch(s, @"[a-fA-F0-9]{32}"))
                    return true;
            }
            return false;
        }
		
		static bool logSanity(string logFile){
			StreamReader SR = new StreamReader(logFile);
			StringBuilder stringBuffer = new StringBuilder("");
			string logRegex = @"^[\d/: ]+[AP]M.*Hash: [a-fA-F0-9]{32}.*Last Cracked: \d+$";
			// only needs to check the latest log; that is, the first 3 lines
			for (int i = 0; i < 3; i++){
				stringBuffer.Append(SR.ReadLine());
			}
			Match match = Regex.Match(stringBuffer.ToString(),logRegex,RegexOptions.Singleline);
			if (match.Success){
				return true;
			}
			return false;
		}
		
		static void writeLog(string logFile, string hash, int last){
			if (!File.Exists(logFile)){
				File.Create(logFile);
			}
			StreamReader SR = new StreamReader(logFile);
			StringBuilder textBuffer = new StringBuilder("");
			textBuffer.AppendLine(DateTime.Now.ToString());
			textBuffer.AppendLine("Hash: " + hash);
			textBuffer.AppendLine("Last Cracked: " + last.ToString());
			for (int i = 0; i < 20; i++)
				textBuffer.Append('-');
			textBuffer.AppendLine();
			textBuffer.Append(SR.ReadToEnd());
			SR.Close();
			StreamWriter SW = new StreamWriter(logFile);
			SW.Write(textBuffer.ToString());
			SW.Close();
		}
		
        static void Main(string[] args)
        {
            // TODO: debug that weird 'client started before server' thing (maybe on client)
            // TODO: find minimum byte size

            int start = 0;
            int listeningPort = 8008;
            int remotePort = 8009;
            string returnData = "";
            string plaintext = "";
            string hash = null;
            string logFile = @"./md5log.txt";
            if (File.Exists(logFile))
            {
				if (logSanity(logFile)){
                	StreamReader SR = File.OpenText(logFile);
					SR.ReadLine();
               		string tempmd5 =  SR.ReadLine().Split()[1];
                	if (isMD5(tempmd5))
               		 {
                    	hash = tempmd5;
                   	 	Int32.TryParse(SR.ReadLine().Split()[2], out start);
                	}
                	SR.Close();
				} else {
					Console.WriteLine("Corrupt log file. Proceeding without.");
				}
				
            } else {
				Console.WriteLine("No log found. Proceeding without");
			}

            // check that hash contains a valid MD5 sum
            setHash(ref hash);
            if (hash == null)
            {
                if (args.Length > 0 && isMD5(args[0]))
                    hash = args[0];
                else
                {
                    while (!isMD5(hash))
                    {
                        Console.WriteLine("MD5 hash not present or not valid. Please enter MD5 hash");
                        hash = Console.ReadLine();
                    }
                }
				File.Create(logFile);
				start = 0;
            }
			
			if(!File.Exists(logFile)){
				writeLog(logFile,hash,start);
			}
				
			
            bool complete = false;

            UdpClient udpClient = new UdpClient(listeningPort);
            Byte[] recieveBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
            //the IP Address.any allows any valid matching address for this machine to be used
            //i.e. loopback, broadcast, IPv4, IPv6
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, listeningPort);  //open port 8008 on this machine
            Console.WriteLine("Server is Started");

            //recieve the data from the UDP packet
            //loop until q is sent
            while (true)
            {
                recieveBytes = udpClient.Receive(ref remoteIPEndPoint);
                returnData = Encoding.ASCII.GetString(recieveBytes);
                returnData.TrimEnd();
                if (!complete && returnData.Length > 2)
                { // a bit shitty, checks the packet has something
                    if (returnData.Substring(0, 3) == "NEW")
                    {
                        sendBlock(start, remoteIPEndPoint.Address, remotePort,hash);
						writeLog(logFile,hash,start);
                        start += Int32.Parse(returnData.Substring(3));
                    }
                    if (returnData.Substring(0, 3) == "FIN")
                    {
                        plaintext = returnData.Substring(3);
                        complete = true;
                        Console.WriteLine(plaintext);
                    }
                }
                else
                {
                    sendFin(remoteIPEndPoint.Address, remotePort);
                }

            }
        }
        static void setHash(ref string hash)
        {
            if (!String.IsNullOrEmpty(hash))
            {
                Console.WriteLine("Found hash {0} in file, use this? [Y/n]", hash);
                string response = Console.ReadLine();
                while (true)
                {
                    switch (response)
                    {
                        case "y":
                            return;
                        case "":
                            return;
                        case "n":
                            hash = null;
                            return;
                        default:
                            Console.WriteLine("Please enter y or n");
                            response = Console.ReadLine();
                            break;
                    }
                }
            }
        }

        static void sendBlock(int start, IPAddress dest, int port, string hash)
        {
            UdpClient client = new UdpClient();
            Byte[] sendBytes = new Byte[1024];
            try
            {
                client.Connect(dest, port);
                sendBytes = Encoding.ASCII.GetBytes("NEW" + hash + start);
                client.Send(sendBytes, sendBytes.GetLength(0));
                Console.WriteLine("Sent block {0} to {1}", start, dest.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with the Server Name: {0}", e.ToString());
            }
        }
        static void sendFin(IPAddress dest, int port)
        {
            UdpClient client = new UdpClient();
            Byte[] sendBytes = new Byte[1024];
            try
            {
                client.Connect(dest, port);
                sendBytes = Encoding.ASCII.GetBytes("FIN");
                client.Send(sendBytes, sendBytes.GetLength(0));
                Console.WriteLine("Sent FIN to {0}", dest.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with the Server Name: {0}", e.ToString());
            }
        }
    }
}