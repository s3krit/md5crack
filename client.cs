using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace stringcount
{
    class Program
    {
        static void Main(string[] args)
        {
            Boolean complete = false;
            IPAddress server;
            if (args.Length > 0 && IPAddress.TryParse(args[0], out server))
                server = IPAddress.Parse(args[0]);
            else
            {
                Console.WriteLine("Server IP not present or not valid. Please enter a valid IP address.");
                while (!IPAddress.TryParse(Console.ReadLine(), out server))
                {
                    Console.WriteLine("Server IP not present or not valid. Please enter a valid IP address.");
                }
            }
            int blockSize;
            int benchmarkSecs = 2;
            string range = "";
            string hash = "";
            string recv = "";
          string plaintext;
	     Console.WriteLine("Benchmarking...");
	      blockSize = crackMin(benchmarkSecs);
	      Console.WriteLine("Cracked {0} in {1} secs. Proceeding with execution",blockSize,benchmarkSecs);
            while (complete == false)
            {
                recv = getBlock(server, 8008,blockSize);
                if (recv.Length > 2)
                {
                    if (recv.Substring(0, 3) == "NEW" && recv.Length > 19)
                    {
                        hash = recv.Substring(3, 32);
                        string start = recv.Substring(3 + 32);
                        Console.WriteLine("Cracking block: {0}", start);
                        plaintext = crackBlock(hash, int.Parse(start), blockSize);
                        if (plaintext != null)
                        {
                            
                            sendFin(server, 8008, plaintext);
                        }
                    }
                    if (recv.Substring(0, 3) == "FIN")
                    {
                        Console.WriteLine("Received FIN, quitting");
                        complete = true;
                    }
                }
            }
        }

        static void sendFin(IPAddress dest, int port, string plaintext)
        {
            UdpClient client = new UdpClient();
            Byte[] sendBytes = new Byte[1024];
            try
            {
                client.Connect(dest, port);
                sendBytes = Encoding.ASCII.GetBytes("FIN" + plaintext);
                client.Send(sendBytes, sendBytes.GetLength(0));
                Console.WriteLine("Sent FIN and payload to server");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with the Server Name: {0}", e.ToString());
            }
        }
        static int crackMin(int secs)
        {
            DateTime end = DateTime.Now + new TimeSpan(0,0,secs);
	    int i = 0;
            for (i = 0; DateTime.Now < end; i++)
            {
                MD5Crack(i.ToString());
            }
            return i;
            
        }
        static string getBlock(IPAddress server, int port, int blockSize)
        {
            int localport = 8009;
            string returnData = "";
            UdpClient client = new UdpClient();
            UdpClient client2 = new UdpClient(localport);
            Byte[] sendBytes = new Byte[1024];
            Byte[] receiveBytes = new Byte[1024];
            try
            {
                IPEndPoint remoteIPEndPoint = new IPEndPoint(server, localport);
                client.Connect(server.ToString(), port);
                sendBytes = Encoding.ASCII.GetBytes("NEW"+blockSize.ToString());
                client.Send(sendBytes, sendBytes.GetLength(0));
                receiveBytes = client2.Receive(ref remoteIPEndPoint);
                returnData = Encoding.ASCII.GetString(receiveBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with the Server Name: {0}", e.ToString());
            }
            return returnData.TrimEnd();

        }

        static string crackBlock(string hash, int start, int count)
        {
            int max = start + count;
            while (start < max)
            {
                if (MD5Crack(start.ToString()) != hash)
                {
                    start++;
                }
                else
                {
                    return start.ToString();
                }
            }
            return null;
        }

        /*		USE FOLLOWING CODE FOR STRINGS, SLOW FOR INTS
                static string Increment(string s){
                    string startChar = "0";
                    char endChar = '9';
                    if ((s == null) || (s.Length == 0))
                        return startChar;
                    char lastChar = s[s.Length - 1];
                    string fragment = s.Substring(0, s.Length - 1);
                    if (lastChar < endChar)
                    {
                        ++lastChar;
                        return fragment + lastChar;
                    }
                    return Increment(fragment) + startChar;
                }
        */
        static string MD5Crack(string s)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] data = Encoding.ASCII.GetBytes(s);
            data = x.ComputeHash(data);
            string ret = "";
            for (int i = 0; i < data.Length; i++)
                ret += data[i].ToString("x2").ToLower();
            return ret;
        }

    }
}
