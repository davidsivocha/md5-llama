using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace MD5CrackerClient
{
    class Client
    {
        static void Main(string[] args)
        {
            //tells the system to only use available processor cycles allowing the program to run in the background
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Idle;
            
            //Greetings Screen
            Console.WriteLine("********************");
            Console.WriteLine("* Distributed MD5  *");
            Console.WriteLine("*      Cracker     *");
            Console.WriteLine("********************");
            Console.WriteLine("");
            Console.WriteLine("********************");
            Console.WriteLine("*      Client      *");
            Console.WriteLine("********************");
            Console.WriteLine("");
            Console.WriteLine("********************");
            Console.WriteLine("*     09000451     *");
            Console.WriteLine("********************");
            Console.WriteLine("");
            Console.WriteLine("--------------------");

            //connects to server
            Console.WriteLine("What is the IP of your server");
            String ServerName = Console.ReadLine();
            string hash = serverConnect(ServerName);
            Console.WriteLine("Received Hash! " + hash);
            Thread.Sleep(50);

            //starts a thread listening for multicast terminate instructions, this allows the program to continue functioning while keeping a constant listen for global instructions.
            Thread terminatorThread = new Thread(new ThreadStart(terminateThread));
            terminatorThread.Start();

            //creates udp clients for listening!
            UdpClient udpClient = new UdpClient(); //outgoing Udp
            UdpClient udpClient2 = new UdpClient(8010); //incoming port

            //section executes code while the thread is alive, this will include requesting new chunks to work through
            String resultYN = null;
            while (terminatorThread.IsAlive)
            {
                Byte[] sendBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
                Byte[] recieveBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
                String textinput = null;
                String returnData = "";

                //sends an initial No to the server to request a chunk, as the server is keyed to pass out new chunks to clients that don't have an answer.
                try
                {
                    IPAddress remoteAddr = Dns.GetHostEntry(ServerName).AddressList[0];  //IP address of the server entered 
                    udpClient.Connect(remoteAddr.ToString(), 8009);  //address of the remotelocation
                    textinput = "n";
                    sendBytes = Encoding.ASCII.GetBytes(textinput.PadRight(1024));
                    udpClient.Send(sendBytes, sendBytes.GetLength(0));  //send the packet
                }//end of the try
                catch (Exception e)
                {
                    Console.WriteLine("Error with the Server Name: {0}", e.ToString());
                    Console.WriteLine("Did you start the Server First ?");
                }//end of the catch

                try
                {
                    //the IP Address.any allows any valid matching address for this machine to be used
                    //i.e. loopback, broadcast, IPv4, IPv6
                    IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 8009);  //open port 8009 on this machine
                    udpClient2.Client.ReceiveTimeout = 500; //sets timeout to prevent the programming hanging if no reply is recieved
                    recieveBytes = udpClient2.Receive(ref remoteIPEndPoint);
                    returnData = Encoding.ASCII.GetString(recieveBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Packet Timed out");
                }

                //grabs the counter value from the returned chunk packet. it only needs one value as the clients know to increment by 100000 immediately
                int counter = 0;
                try
                {
                    counter = Convert.ToInt32(returnData);
                }
                catch
                {
                    counter = 0;
                    Console.ReadLine();
                    Environment.Exit(0);
                }

                Console.WriteLine("Recieved Chunk {0} - {1}", counter, counter + 100000); //included to provide visual indication that the program is recieving chunks
                String result = checkHash(hash, counter, counter + 100000); //pass to the check hash function
                resultYN = result.Split()[0]; //the check hash function may pass back a yes result, this seperates the yes or no out for case checking

                //if the result is positive, the client sends a result packet straight away, that contains a yes terminate for the server, and the actual hash value
                if (resultYN == "y")
                {
                    try
                    {
                        IPAddress remoteAddr = Dns.GetHostEntry(ServerName).AddressList[0];  //IP address of the server entered 
                        udpClient.Connect(remoteAddr.ToString(), 8009);  //address of the remotelocation
                        //read in the text from the console
                        textinput = result;
                        sendBytes = Encoding.ASCII.GetBytes(textinput.PadRight(1024));
                        udpClient.Send(sendBytes, sendBytes.GetLength(0));  //send the packet
                    }//end of the try
                    catch (Exception e)
                    {
                        Console.WriteLine("Error with the Server Name: {0}", e.ToString());
                        Console.WriteLine("Did you start the Server First ?");
                    }//end of the catch
                }
            }

            //provides a delay to program close
            Console.WriteLine("");
            Console.ReadLine();

        }
        
        //checks the hashes against generated hashes
        static string checkHash(string original, int start, int end)
        {
            string tocheck = "";
            string foundHash = "n";
            for (; start < end; start++)
            {
                tocheck = start.ToString();
                if (original.CompareTo(generateHash(tocheck)) == 0)
                {
                    Console.WriteLine("Found clear text is " + tocheck);
                    start = end;
                    foundHash = "y " + tocheck;
                    
                }
            } //end of the FOR Loop
            return foundHash;
        }

        //used to generate the hashes to check against
        static string generateHash(string input)
        {
            //the method used here to generate the MD5 hash is a standard method provided by Microsoft
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        //This function runs in a thread, it constantly checks for a terminate signal
        static void terminateThread()
        {
            //forces the client to join a multicast group listening on port 8012 for a global terminate signal, which will be sent once the correct hash has been found
            UdpClient multicastClient = new UdpClient(8012);
            IPAddress multicastIpAddress = IPAddress.Parse("225.0.0.1"); // assign the Multicast address
            //join the Multicast group
            multicastClient.JoinMulticastGroup(multicastIpAddress);
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 8012);
            Byte[] recieveBytes = multicastClient.Receive(ref remoteIPEndPoint);
            string returnData = Encoding.ASCII.GetString(recieveBytes);

            //Prints the values to the client machines then kills the thread
            Console.WriteLine("The Hash has been Discovered: {0}", returnData);
            Console.WriteLine("Program now ready to end!");
            Thread.CurrentThread.Abort();

        }

        //provides initial connection to the server, and checks for timeout, if no response is found
        static string serverConnect(string ServerName)
        {
            //creates updclient on this machine to recieve data
            UdpClient udpClient = new UdpClient();
            UdpClient udpClient2 = new UdpClient(8010);
            Byte[] sendBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
            String textinput = null;

            //requests input of ip of server - consider switching to a multicast to join the server group

            String returnData = "";
            String hello = null;
            String hash = null;

            //sends data to the server address, in this case a Hello packet, if a hello is recieved back then the loop ends or until 4 packets have been sent
            int counter = 0;
            while (counter < 4)
            {
                try
                {
                    IPAddress remoteAddr = Dns.GetHostEntry(ServerName).AddressList[0];  //IP address of the server entered 
                    udpClient.Connect(remoteAddr.ToString(), 8009);  //address of the remotelocation
                    Console.WriteLine("Testing Connection");
                    //read in the text from the console
                    textinput = "Hello";
                    sendBytes = Encoding.ASCII.GetBytes(textinput.PadRight(1024));
                    udpClient.Send(sendBytes, sendBytes.GetLength(0));  //send the packet
                }//end of the try
                catch (Exception e)
                {
                    Console.WriteLine("Error with the Server Name: {0}", e.ToString());
                    Console.WriteLine("Did you start the Server First ?");
                }//end of the catch

                try
                {
                    Byte[] recieveBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
                    //the IP Address.any allows any valid matching address for this machine to be used
                    //i.e. loopback, broadcast, IPv4, IPv6
                    IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 8010);  //open port 8010 on this machine
                    udpClient2.Client.ReceiveTimeout = 500; //sets timeout to prevent the programming hanging if no reply is recieved
                    recieveBytes = udpClient2.Receive(ref remoteIPEndPoint);
                    returnData = Encoding.ASCII.GetString(recieveBytes);
                    hello = returnData.Split()[0];
                    hash = returnData.Split()[1];
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Packet {0} Timed out. Sending until 4!", counter + 1);
                    counter++;
                }

                if (counter == 4)
                {
                    Console.WriteLine("Unable to establish connection: program now terminating!");
                    Console.WriteLine("Press enter to close");
                }

                if (hello == "Hello")
                {
                    Console.WriteLine("Connected To Server!");
                    Console.WriteLine("");
                    counter = 4;
                }

            }
            udpClient.Close();
            udpClient2.Close();
            return hash;
        }

    }
}
