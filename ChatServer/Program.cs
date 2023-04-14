
/* File: Program.cs
*  Project: Prog2121-Assignment05
*  Programmer: Hoda Akrami, Suka Sun
*  Last Version: 2020-11-09
*  Description: This file is a Server side of a chat system. It contains a Worker method to recive a message from the client and respond base on the request, ClientMappingAdd method to add the client to the Client Mapping,
*  ClientMappingDelete method to remove all the records from the mapping related to the given username, CommandExecuteLogin method to execute commands when the client requests for Login, CommandExecuteLogout mthod to execute commands when the client requests for Logout
*  CommandExecuteDisconnect method to execute commands when the client requests for Disconnect, CommandExecuteConnect method to execute commands when the client requests for Connect, CommandExecuteSend metod to execute commands when the client requests for sending message,
*  CommandExecuteReceive method to retrieve message from the message queue if there is any unread message to the given clients, CommandExecute method to handel the client's input/request.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;

namespace ChatServer
{
    /*
    * NAME: Program
    * PURPOSE: This is a class for having a Worker method to recive a message from the client and respond base on the request, ClientMappingAdd method to add the client to the Client Mapping,
    *  ClientMappingDelete method to remove all the records from the mapping related to the given username, CommandExecuteLogin method to execute commands when the client requests for Login, CommandExecuteLogout mthod to execute commands when the client requests for Logout
    *  CommandExecuteDisconnect method to execute commands when the client requests for Disconnect, CommandExecuteConnect method to execute commands when the client requests for Connect, CommandExecuteSend metod to execute commands when the client requests for sending message,
    *  CommandExecuteReceive method to retrieve message from the message queue if there is any unread message to the given clients, CommandExecute method to handel the client's input/request.
    */
    
    class Program
    {
        // Create a list of clients who have already logged in
        static List<String> list_clients = new List<String>();

        // Create mapping for clients who have been connected  
        // key(string): msgFrom client , Value(List<String>): msgTo client(s) )
        static Dictionary<string, List<String>> client_map = new Dictionary<string, List<String>>();

        // Create message queue for the client
        // key(string): clients who have been connected with other clients, value(Queue<String>): message queque
        static Dictionary<string, Queue<String>> client_msg = new Dictionary<string, Queue<String>>();


        private static readonly object clientMappingLock = new object();

        static void Main(string[] args)
        {
            TcpListener server = null;

            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    ParameterizedThreadStart ts = new ParameterizedThreadStart(Worker);
                    Thread clientThread = new Thread(ts);
                    clientThread.Start(client);
                    Thread.Sleep(1);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

       /*  -- Method Header Comment
       Name	    :   Worker
       Purpose  :   to recive a message from the client and respond base on the request
       Inputs	:	Object      o
       Outputs	:	display messages based on the request, or error message if an error occuered 
       Returns	:	Nothing
       */
        public static void Worker(Object o)
        {
            TcpClient client = (TcpClient)o;
            // Buffer for reading data
            Byte[] bytes = new Byte[256];
            string data = null;

            // Get a stream object for reading and writing
            NetworkStream stream = client.GetStream();

            int i;

            // Loop to receive all the data sent by the client.
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                if(!data.StartsWith("R"))
                {
                    Console.WriteLine("Received: {0}", data);
                }
                
                // Process the data sent by the client.
                string result = CommandExecute(data);

                byte[] msg = System.Text.Encoding.ASCII.GetBytes(result);

                // Send back a response.
                stream.Write(msg, 0, msg.Length);
                if (!result.Equals("<No New Message Received>"))
                {
                    Console.WriteLine("Sent: {0}", result);
                }
                
            }

            // Shutdown and end connection
            client.Close();
        }

        /*  -- Method Header Comment
        Name	:   ClientMappingAdd
        Purpose :   Add the client to the Client Mapping
        Inputs	:	string      msgFrom
                    string      msgTo
        Outputs	:	display messages based on the request, or error message if an error occuered 
        Returns	:	Nothing
        */
        public static string ClientMappingAdd(string msgFrom, string msgTo)
        {
            // Case when msgFrom client has already connected with other client(s)
            if (client_map.ContainsKey(msgFrom))
            {
                // Case when msgTo client has already connected with you.
                if (client_map[msgFrom].Contains(msgTo))
                {
                    return "User " + msgTo + " has already connected with you ...";
                }
                else
                {
                    // Add the msgTo client to the client mapping of the msgFrom client
                    client_map[msgFrom].Add(msgTo);
                }
            }
            else
            {
                // Case when msgFrom client has not connected with any client yet
                client_map.Add(msgFrom, new List<String>() { msgTo });
            }

            // Response message to confirm connection
            return "User " + msgTo + " is added to " + msgFrom + "'s client mapping list ";
        }

        /*  -- Method Header Comment
        Name	:   ClientMappingDelete
        Purpose :   Remove all the records from the mapping related to the given username
        Inputs	:	string      username
        Outputs	:	display a message
        Returns	:	string      response
        */
        public static string ClientMappingDelete(string username)
        {
            lock (clientMappingLock)
            {
                string response = "";

                if (client_map.ContainsKey(username))
                {
                    // Remove the given username from each conneted client's record
                    foreach (string usr in client_map[username])
                    {
                        if (client_map.ContainsKey(usr))
                        {
                            if (client_map[usr].Contains(username))
                            {
                                client_map[usr].Remove(username);
                                response += username + " is removed from " + usr + "'s client mapping\n";
                            }
                        }
                    }
                    // Remove the given username from the client mapping
                    client_map.Remove(username);
                    response += username + "'s client mapping is cleared";
                }
                return response;
            }
        }

        /*  -- Method Header Comment
        Name	:   ClientMappingDelete
        Purpose :   delete the client to the Client Mapping
        Inputs	:	string      msgFrom
                    string      msgTo
        Outputs	:	Nothing 
        Returns	:	Nothing
        */
        public static bool ClientMappingDelete(string msgFrom, string msgTo)
        {
            lock (clientMappingLock)
            {
                if (client_map.ContainsKey(msgFrom))
                {
                    if(client_map[msgFrom].Contains(msgTo))
                    {
                        client_map[msgFrom].Remove(msgTo);
                        return true;
                    }
                }
                return false;
            }
        }

        /*  -- Method Header Comment
        Name	:   CommandExecuteLogin
        Purpose :   Execute commands when the client requests for Login
        Inputs	:	string      msg
        Outputs	:	Display message 
        Returns	:	string    response  
        */
        public static string CommandExecuteLogin(string msg)
        {
            string loginUser = msg;
            string response = "";

            // If the client has already logged in, response a message
            if (list_clients.Contains(loginUser))
            {
                response = "You have already logged in";
                return response;
            }

            if (!client_msg.ContainsKey(loginUser))
            {
                // Create a message queue for the client if the client does not exist in the client list yet
                client_msg.Add(loginUser, new Queue<string>());
            }
            
            // Add the client to the client list
            list_clients.Add(loginUser);
            // Reponse a message to confirm connection with the available clients info
            response = "You are connected! Below are vailable user(s) to contact with:\n" +
                "------------------------\n" +
                String.Join("\n", list_clients.ToArray()) +
                "\n------------------------\n";
            return response;
        }

        /*  -- Method Header Comment
        Name	:   CommandExecuteLogout
        Purpose :   Execute commands when the client requests for Logout
        Inputs	:	string      msg
        Outputs	:	Display message 
        Returns	:	string    response  
        */
        public static string CommandExecuteLogout(string msg)
        {
            string loginUser = msg;
            string response = "";

            // Remove user from client list
            list_clients.Remove(loginUser);
            response += loginUser + "removed from client list\n";

            response += ClientMappingDelete(loginUser);

            // Signal the client's worker to exit
            client_msg[loginUser].Enqueue("system: logout");

            // Wait till the loginUser's message queue is empty 
            while (client_msg[loginUser].Count != 0)
            {
                Console.WriteLine("Wait till the loginUser's message queue is empty... {0}", client_msg[loginUser].Count);
                Thread.Sleep(500);
            }

            // Remove client from the message queue
            if (client_msg.ContainsKey(loginUser))
            {
                client_msg.Remove(loginUser);
                response += loginUser + "removed from message queue\n";
            }

            return response;
        }

        /*  -- Method Header Comment
        Name	:   CommandExecuteDisconnect
        Purpose :   Execute commands when the client requests for Disconnect
        Inputs	:	string      givenMsg
        Outputs	:	Display message 
        Returns	:	string    response  
        */
        public static string CommandExecuteDisconnect(string givenMsg)
        {
            string[] inputD = givenMsg.Split(',');
            string msgFrom = inputD[0];
            string msgTo = inputD[1];

            ClientMappingDelete(msgFrom, msgTo);

            client_msg[msgTo].Enqueue("system: disconnect user," + msgFrom);

            return "You are disconnected with " + msgTo;
        }

        /*  -- Method Header Comment
        Name	:   CommandExecuteConnect
        Purpose :   Execute commands when the client requests for Connect
        Inputs	:	string      msg
        Outputs	:	Display message 
        Returns	:	string    response  
        */
        public static string CommandExecuteConnect(string msg)
        {
            string[] inputC = msg.Split(',');
            string msgFrom = inputC[0];
            string msgTo = inputC[1];
            string response = "";

            // Case when msgTo user is not connected with the server
            if (!list_clients.Contains(msgTo))
            {
                return "User " + msgTo + " is not online ... try again later ...";
            }

            // Update client mapping for clients on both sides, msgFrom and msgTo
            response += ClientMappingAdd(msgFrom, msgTo);
            response += ClientMappingAdd(msgTo, msgFrom);

            // Display message to confirm connection
            client_msg[msgTo].Enqueue("system: connect user," + msgFrom);

            return response;
        }

        /*  -- Method Header Comment
        Name	:   CommandExecuteSend
        Purpose :   Execute commands when the client requests for Sending message
        Inputs	:	string      msg
        Outputs	:	Display message 
        Returns	:	string    message  
        */
        public static string CommandExecuteSend(string msg)
        {
            string msgFrom = "";
            string msgTo = "";
            string msgStr = "";

            int index = 0;
            foreach (string substr in msg.Split(','))
            {
                if (index == 0)
                {
                    msgFrom = substr;
                }
                else if (index == 1)
                {
                    msgTo = substr;
                }
                else if (index == 2)
                {
                    msgStr = substr;
                }
                else  // In case the input msg contains key word ","
                {
                    msgStr += "," + substr;
                }
                index++;
            }

            // Create a message queue for the client if the client does not have a message queque yet
            if (!client_msg.ContainsKey(msgTo))
            {
                client_msg.Add(msgTo, new Queue<string>());
            }

            // Case when the msgFrom client has not connected with any other client
            if (!client_map.ContainsKey(msgFrom))
            {
                return "You are not connected with anyone ...";
            }
            // Case when the msgTo client has not connected with the msgTo client
            else if (!client_map.ContainsKey(msgTo))
            {
                // Display message if the client requests for sending message to all 
                if (msgTo == "ALL")
                {
                    foreach (string usr in client_map[msgFrom])
                    {
                        client_msg[usr].Enqueue(msgFrom + " -> [ALL] : " + msgStr);
                    }
                    return "your message is set to ALL";
                }
                // Display error message if the client's name is not in the client mapping and client
                // does not request for sending message to all
                return msgFrom + " is not connected with " + msgTo + "...";
            }
            // Case when msgFrom client and msgTo client are connected
            else if (client_map[msgFrom].Contains(msgTo) && client_map[msgTo].Contains(msgFrom))
            {
                // Add message to the msgTo client's message queue
                client_msg[msgTo].Enqueue(msgFrom + ": " + msgStr);
                // Response message to confirm sending
                return "your message is sent to " + msgTo + "!";
            }
            else
            {
                return "Something is wrong, your message cannot be sent out ...";
            }
        }

        /*  -- Method Header Comment
        Name	:   CommandExecuteReceive
        Purpose :   Retrieve message from the message queue if there is any unread message to the given clients
        Inputs	:	string      msgTo
        Outputs	:	Display message 
        Returns	:	string    message  
        */
        public static string CommandExecuteReceive(string msgTo)
        {
            // Check if client has any unread message
            if (client_msg.ContainsKey(msgTo))
            {
                if (client_msg[msgTo].Count > 0)
                {
                    // Response the unread message to the cient
                    return client_msg[msgTo].Dequeue();
                }
                else
                {
                    return "<No New Message Received>";
                }
            }
            else
            {
                return "There is no msg for you :)";
            }
        }

        /*  -- Method Header Comment
        Name	:   CommandExecute
        Purpose :   Call CommandExecute function based on the client's input/request.
        Inputs	:	string      data
        Outputs	:	Display message 
        Returns	:	string    message  
        */
        public static string CommandExecute(string data)
        {
            TcpClient client = new TcpClient();
            // Console.Write("--- Recieved String = [{0}]\n", data);

            switch (data.Substring(0, 1))  // Check the first letter from data sent by the client
            {
                case "L":   // Case for the user to Login (connect to the server)
                    return CommandExecuteLogin(data.Substring(1));
                case "C":   // Case for the user to connect others
                    return CommandExecuteConnect(data.Substring(1));
                case "S":   // Case for the user to send msg to others
                    return CommandExecuteSend(data.Substring(1));
                case "R":   // Case for the user to receive msg from others
                    return CommandExecuteReceive(data.Substring(1));
                case "D":   // Case fot the user to disconnect the current contact
                    return CommandExecuteDisconnect(data.Substring(1));
                case "X":   // Case for the user to Logout (disconnect with the server and clear the mapping)
                    return CommandExecuteLogout(data.Substring(1));
                default:
                    return "Error";
            }
        }
    }
}
