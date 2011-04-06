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
		
		static bool saveSanity(StreamReader SR){
				return true;
		}
		
		static void writeLog(StreamWriter SW, string hash, int last){
			StringBuilder textBuffer = new StringBuilder("");
			for (int i = 0; i < 20; i++)
				textBuffer.Append('*');
			textBuffer.AppendLine(DateTime.Now.ToString());
			textBuffer.AppendLine("Hash: " + hash);
			textBuffer.AppendLine("Last Cracked: " + last.ToString());
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
            string historyFile = @"./md5hist.txt";
            if (File.Exists(historyFile))
            {
                StreamReader SR = File.OpenText(historyFile);
                string tempmd5 = SR.ReadLine();
                if (isMD5(tempmd5))
                {
                    hash = tempmd5;
                    Int32.TryParse(SR.ReadLine(), out start);
                }
                SR.Close();
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
            }

            if (!File.Exists(historyFile))
            {
                //File.Create(historyFile);
                StreamWriter SW = new StreamWriter(historyFile);
                SW.WriteLine(hash + Environment.NewLine + start);
                SW.Close();
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
                        StreamWriter SW = new StreamWriter(historyFile);
                        SW.WriteLine(hash + Environment.NewLine + start);
                        SW.Close();
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