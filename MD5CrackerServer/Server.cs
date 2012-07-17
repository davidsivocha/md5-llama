using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;



namespace MD5CrackerServer
{
    class Server
    {
        static void Main(string[] args)
        {
            //Stores the time the server started
            DateTime startTime = DateTime.Now;
            
            //Variable to store the initial hash
            String hash = "";

            
            
            //Greetings Screen
            Console.WriteLine("********************");
            Console.WriteLine("* Distributed MD5  *");
            Console.WriteLine("*      Cracker     *");
            Console.WriteLine("********************");
            Console.WriteLine("");
            Console.WriteLine("********************");
            Console.WriteLine("*      Server      *");
            Console.WriteLine("********************");
            Console.WriteLine("");
            Console.WriteLine("********************");
            Console.WriteLine("*     09000451     *");
            Console.WriteLine("********************");
            Console.WriteLine("");
            Console.WriteLine("--------------------");
            Console.WriteLine("Server Has started at {0}", startTime);
            Console.WriteLine();

            Console.WriteLine("Please use any of the following addresses to connect to the server");
            
            //gets the machines IP Addresses and prints them for reference
            IPAddress[] Adresses = GetAllUnicastAddresses();
            foreach (IPAddress Adres in Adresses){
            Console.WriteLine("IP Address: {0}", Adres);
            }
            
            Console.WriteLine();

            //Request Hash Input, checks to make sure field is not null
            Console.WriteLine("Please Input the hash desired to be cracked");
            hash = Console.ReadLine();

            while (hash == "" || hash == null)
            {
                Console.WriteLine("No Input detected, please type in a hash!");
                hash = Console.ReadLine();
            }

            
            
            //Section Starts the Server and waits for a return hash value
            string hashValue = serverStart(hash);

            Thread.Sleep(50);

            //hashvalue passed onto terminate clients to be passed out to all machines
            terminateClients(hashValue);

            //delays the program end
            Console.ReadLine();
        }

        //Function to get IP addresses
        public static IPAddress[] GetAllUnicastAddresses(){
        // By passing an empty string to GetHostEntry we receive all the IP addresses on the local machine
        IPHostEntry LocalEntry = Dns.GetHostEntry("");
        return LocalEntry.AddressList;
        }

        //this function when called sends a multicast out to all clients available telling them the hash has been found and killing their own processes
        static void terminateClients(String hashValue) {
            UdpClient udpClient = new UdpClient();
            Byte[] sendBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
            Console.WriteLine("Server is Started");
            IPAddress address = IPAddress.Parse("225.0.0.1");  //get mulitcast address
            udpClient.Connect(address, 8012); //open a connection to that location on port 8012
            string data = hashValue;
            sendBytes = Encoding.ASCII.GetBytes(data.PadRight(1024));
            udpClient.Send(sendBytes, sendBytes.GetLength(0)); //send information to the port
            Console.WriteLine("Terminate Information Sent");  //user feedback
            Console.WriteLine("Program can now terminate");

        }

        static string serverStart(string hash)
        {
            string returnData = "";
            UdpClient udpClient = new UdpClient(); //udp client for sending data
            UdpClient udpClient2 = new UdpClient(8009); //udp client fixed on port 8009 for receiving data
            Byte[] recieveBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
            Byte[] sendBytes = new Byte[1024]; // buffer to read the data into 1 kilobyte at a time
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 8009);  //open port 8009 on this machine
            String reply = null;
            String splitYN = null;
            String hashValue = null;
            int count = 0;
            DateTime startTime = DateTime.Now;


            //Path to Logfile
            String fileName = "f:\\09000451-log.txt";
            StreamWriter Swriter; //stream to write to a logfile
            StreamReader Sreader; //stream to read from logfile

            if (File.Exists(fileName)) //check to see if the file exists
            {
                Console.WriteLine("Log File found! Continuing from last entry!");
                String inputtext = null;
                Sreader = new StreamReader(fileName);
                while (Sreader.Peek() >= 0)
                {
                    inputtext = Sreader.ReadLine();
                }
                String tempcount = inputtext.Split()[1];
                try
                {
                    //checks to see if there is at least one numerical entry in the logfile, if not, count will start at 0, but this does stop the program crashing if there is no logfile contents
                    count = Convert.ToInt32(tempcount);
                    Console.WriteLine("Starting from {0}", count);
                }
                catch (FormatException e)
                {
                    Console.WriteLine("Unable to find any entries in logfile! Starting from 0!");
                    count = 0;
                }
                Sreader.Close();
                
            }
            else
            {
                //file does not already exist start from the begining
                Console.WriteLine("No Log File found! Creating now!");
                Swriter = new StreamWriter(fileName, false); //name of file
                startTime = DateTime.Now;
                Swriter.WriteLine("Hash {0} started on {1}", hash, startTime);
                Swriter.Close();
            }//end of the IF else
            Swriter = new StreamWriter(fileName, true); //name of file
            Console.WriteLine("Server is Started");
            Console.WriteLine("");
            Console.WriteLine("Clients can now connect");

            //keep recieving packets until a terminate is sent from a client, in this case terminate is in the form of a yes packet sent by a machine discovering the correct hash
            while (splitYN != "y")
            {
                recieveBytes = udpClient2.Receive(ref remoteIPEndPoint);
                returnData = Encoding.ASCII.GetString(recieveBytes);
                splitYN = returnData.Split()[0];
                hashValue = returnData.Split()[1];
                if (splitYN == "Hello")
                {
                    //if a hello packet is detected, sends a confirm hello back with the hash attached to the datagram, so the clients recieve hashes straight away
                    Console.WriteLine(remoteIPEndPoint.Address.ToString() + " connected!");
                    IPAddress remoteAddr = remoteIPEndPoint.Address;  //IP address of the server entered 
                    udpClient.Connect(remoteAddr.ToString(), 8010);  //address of the remotelocation
                    reply = "Hello " +hash;
                    sendBytes = Encoding.ASCII.GetBytes(reply.PadRight(1024));
                    udpClient.Send(sendBytes, sendBytes.GetLength(0));  //send the packet

                }
                else if (splitYN == "n")
                {
                    //writes to the logfile to let it know what chunks have been sent
                    Console.WriteLine("Sending chunk to " + remoteIPEndPoint.Address.ToString());
                    String ip = remoteIPEndPoint.Address.ToString();
                    Swriter.WriteLine("Sent {0} - {1} to {2}", count, count+100000, ip);
                    Swriter.Flush(); // included to ensure data is written to the logfile immediately
                    IPAddress remoteAddr = remoteIPEndPoint.Address;  //IP address of the server entered 
                    udpClient.Connect(remoteAddr.ToString(), 8010);  //address of the remotelocation
                    reply = count.ToString();
                    sendBytes = Encoding.ASCII.GetBytes(reply.PadRight(1024));
                    udpClient.Send(sendBytes, sendBytes.GetLength(0));  //send the packet
                    count = count + 100000; //increments the count ready to send the next chunk on
                }
                else if (splitYN == "y")
                {
                    //once a tes packet is received these statements kick in, writing to both screen and file
                    DateTime endTime = DateTime.Now;
                    Console.WriteLine("HASH FOUND!");
                    Console.WriteLine("{0} converts to {1}", hash, hashValue);
                    Swriter.WriteLine("");
                    Swriter.WriteLine("Cleartext Found: the hash converst to {0}!", hashValue);
                    Swriter.WriteLine("");
                    Swriter.WriteLine("Cracking ended at {0}", endTime);
                    Swriter.Close();
                }
            }

            //the actual value of the hash will not be returned until the while loop closes on receipt of a yes packet
            return hashValue;

        }
    }
}
