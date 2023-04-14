
/* File: MainWindow.xaml.cs
*  Project: Prog2121-Assignment05
*  Programmer: Hoda Akrami, Suka Sun
*  Last Version: 2020-11-09
*  Description: This file is a clinent side of a chat system. It contains a ConnectClient method to send and recive a message from the server, UpdateChatMsg method to display the messages on the list box
*  Worker method to connect to the server and display messages sent by others from the server to the client, BtnLogout_Click to logout, BtnConnect_Click method to conect to the other clients,
*  BtnDisconnect_Click method to discconnect from othere clients, OnSelectionChanged method to update txtContact when cboxUsrList selection is changed, TxtContactChangedEventHandler to select which user you want to talk,
*  btnSendMessage_Click to send the message, btnLogin_Click method to login.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Windows.Threading;
using System.Security.Cryptography;

namespace ChatSystem_A5
{
    /*
    * NAME: MainWindow : Window
    * PURPOSE: This is a class for having ConnectClient method to send and recive a message from the server, UpdateChatMsg method to display the messages on the list box
    *  Worker method to connect to the server and display messages sent by others from the server to the client, BtnLogout_Click to logout, BtnConnect_Click method to conect to the other clients,
    *  BtnDisconnect_Click method to discconnect from othere clients, OnSelectionChanged method to update txtContact when cboxUsrList selection is changed, TxtContactChangedEventHandler to select which user you want to talk,
    *  btnSendMessage_Click to send the message, btnLogin_Click method to login.
    */
    public partial class MainWindow : Window
    {
        delegate void MyCallback(Object obj);       // Delegate declaration for use in Invoke

        TcpClient client = null;
        NetworkStream stream = null;
        Thread clientThread = null;

        int processInterval = 200;                 // in msec

        public MainWindow()
        {
            InitializeComponent();

            // Add default ComboBox option
            cboxUsrList.Items.Add("ALL");
            // Initialize buttons
            btnLogin.IsEnabled = true;
            btnConnect.IsEnabled = false;
            btnDisconnect.IsEnabled = false;
            btnSendMessage.IsEnabled = false;
            btnLogout.IsEnabled = false;
        }

        /*  -- Method Header Comment
        Name	:   UpdateChatMsg
        Purpose :   Update chat messages and auto scroll down to the latest message
        Inputs	:	string      msg
        Outputs	:	Nothhing 
        Returns	:	Nothing  
        */
        public void UpdateChatMsg(String msg)
        {
            lstChat.Items.Add(msg);
            lstChat.SelectedIndex = lstChat.Items.Count - 1;
            lstChat.ScrollIntoView(lstChat.SelectedItem);
        }

        /*  -- Method Header Comment
        Name	:   ConnectClient
        Purpose :   Send and recive a message from the server
        Inputs	:	NetworkStream       givenStream
                    String              message
                    bool                displayResp = true
        Outputs	:	Display message 
        Returns	:	string      returndata  
        */
        public string ConnectClient(NetworkStream givenStream, String message, bool displayResp = true)
        {
            string returndata = "";
            try
            {
                // Send the message to the connected TcpServer.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                givenStream.Write(data, 0, data.Length);

                // Receive the TcpServer.response.

                // Buffer to store the response bytes.
                data = new Byte[256];

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                returndata = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                if (displayResp)
                {
                    UpdateChatMsg(returndata);
                }
            }
            catch (ArgumentNullException e)
            {
                returndata = "ArgumentNullException: " + e.Message;
            }
            catch (SocketException e)
            {
                returndata = "SocketException: " + e.Message;
            }
            return returndata;
        }

        /*  -- Method Header Comment
        Name	:   Worker
        Purpose :   The Worker connects to the server and display messages sent by others from the server to the client.
        Inputs	:	object       strList
        Outputs	:	Display message 
        Returns	:	Nothing 
        */
        public void Worker(object strList)
        {
            try
            {
                // Define TCPClient with the given server IP
                TcpClient clientX = new TcpClient(((List<String>)strList)[0], 13000);

                // Get the NetworkStream from the TCPClient defined above
                NetworkStream streamX = clientX.GetStream();

                // Second item in the string list is the msgTo client's name 
                string msgTo = ((List<String>)strList)[1];

                bool keepChecking = true;
                while (keepChecking)
                {
                    // Send to server
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes("R" + msgTo);
                    streamX.Write(data, 0, data.Length);

                    // Get respond fron the sever
                    data = new Byte[256];
                    String responseData = String.Empty;
                    Int32 bytes = streamX.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                    Console.WriteLine("responseData: {0}", responseData);
                    if (!responseData.Equals("<No New Message Received>"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {

                            if (responseData.Equals("system: logout"))
                            {
                                // Update buttons
                                btnLogin.IsEnabled = true;
                                btnConnect.IsEnabled = false;
                                btnDisconnect.IsEnabled = false;
                                btnSendMessage.IsEnabled = false;
                                btnLogout.IsEnabled = false;
                                // Kill Worker
                                keepChecking = false;
                            }
                            else if (responseData.StartsWith("system: connect user,"))
                            {

                                string connectToClient = responseData.Split(',')[1];

                                // Add client to the ComboBox
                                if (!cboxUsrList.Items.Contains(connectToClient))
                                {
                                    cboxUsrList.Items.Add(connectToClient);
                                }

                                // Update Contact Name under txtContact
                                txtContact.Text = connectToClient;
                                // Update buttons
                                btnLogin.IsEnabled = false;
                                btnConnect.IsEnabled = true;
                                btnDisconnect.IsEnabled = true;
                                btnSendMessage.IsEnabled = true;
                                btnLogout.IsEnabled = false;
                            }
                            else if (responseData.StartsWith("system: disconnect user,"))
                            {
                                string disconnetClient = responseData.Split(',')[1];
                                // Add client to the ComboBox
                                cboxUsrList.Items.Remove(disconnetClient);
                                txtContact.Text = "";
                                // Update buttons
                                btnLogin.IsEnabled = false;
                                btnConnect.IsEnabled = true;
                                btnDisconnect.IsEnabled = true;
                                btnSendMessage.IsEnabled = true;
                                btnLogout.IsEnabled = false;

                                // Deal with the case when no more contact but ALL left which needs to be removed
                                if (cboxUsrList.Items.Count == 1 && cboxUsrList.Items.Contains("ALL"))
                                {
                                    cboxUsrList.Items.Remove("ALL");
                                    txtContact.Text = "";
                                }
                            }
                            UpdateChatMsg(responseData);
                        });
                    }
                    Thread.Sleep(processInterval);
                }

                Console.WriteLine("Worker is going to be terminated ...");
                clientX.Close();
                streamX.Close();
            }
            catch (SocketException e)
            {
                lstChat.Items.Add("SocketException. Cannot start the Worker. Try again later...");
                Thread.Sleep(processInterval);
                Worker(strList);
            }
        }

        /*  -- Method Header Comment
        Name	:   BtnSendMessage_Click
        Purpose :   Send the message to the server
        Inputs	:	object              sender
                    RoutedEventArgs     e
        Outputs	:	Display message 
        Returns	:	Nothing 
        */
        private void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            // Deal with the case when txtContact.Text is empty which send should not be allowed
            if (txtContact.Text == "")
            {
                lstChat.Items.Add("No Contact is selected, cannot send this msg ...");
                return;
            }

            // Send msg
            UpdateChatMsg("[Self] -> " + txtContact.Text + ": " + messageText.Text);
            string response = ConnectClient(
                stream, "S" + txtUserIP.Text + "," + txtContact.Text + "," + messageText.Text, false);
            messageText.Text = "";
        }

        /*  -- Method Header Comment
        Name	:   BtnLogin_Click
        Purpose :   Login to the server
        Inputs	:	object              sender
                    RoutedEventArgs     e
        Outputs	:	Display message 
        Returns	:	Nothing 
        */
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (client == null)
            {
                try
                {
                    client = new TcpClient(txtServerIP.Text, 13000);
                }
                catch (SocketException exception)
                {
                    //Console.WriteLine("SocketException: {0}", exception);
                    lstChat.Items.Add("SocketException: " + exception);
                    return;
                }
            }
            if (stream == null)
            {
                try
                {
                    stream = client.GetStream();
                }
                catch (SocketException exception)
                {
                    // Console.WriteLine("SocketException: {0}", exception);
                    lstChat.Items.Add("SocketException: " + exception);
                    return;
                }
            }
            string returndata = ConnectClient(stream, "L" + txtUserIP.Text);

            if (returndata.Equals("You have already logged in"))
            {
                return;
            }

            if (clientThread == null)
            {
                // Start Woker to receive msg from server sent by other client.
                ParameterizedThreadStart ts = new ParameterizedThreadStart(Worker);
                clientThread = new Thread(ts);
                clientThread.Start(new List<String>() { txtServerIP.Text, txtUserIP.Text });
                Thread.Sleep(1);
            }

            btnLogin.IsEnabled = false;
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
            btnSendMessage.IsEnabled = false;
            btnLogout.IsEnabled = true;
        }

        /*  -- Method Header Comment
        Name	:   BtnLogout_Click
        Purpose :   Logout to the server
        Inputs	:	object              sender
                    RoutedEventArgs     e
        Outputs	:	Display message 
        Returns	:	Nothing 
        */
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            ConnectClient(stream, "X" + txtUserIP.Text);

            // Wait till the worker thread is terminated
            while (clientThread != null && clientThread.ThreadState == 0)
            {
                Console.WriteLine("Wait till the worker is no longer alive ...");
                Thread.Sleep(500);
            }
            clientThread = null;

            stream.Close();
            client.Close();

            stream = null;
            client = null;

            // Update buttons
            btnLogin.IsEnabled = true;
            btnConnect.IsEnabled = false;
            btnDisconnect.IsEnabled = false;
            btnSendMessage.IsEnabled = false;
            btnLogout.IsEnabled = false;
        }

        /*  -- Method Header Comment
        Name	:   BtnConnect_Click
        Purpose :   Connect to the another user
        Inputs	:	object              sender
                    RoutedEventArgs     e
        Outputs	:	Display message 
        Returns	:	Nothing 
        */
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string serverRespMsg = ConnectClient(stream, "C" + txtUserIP.Text + "," + txtContact.Text);

            // If unable to connect to the given client, do nothing, just return;
            if (serverRespMsg.Contains("is not online ... try again later"))
            {
                return;
            }

            // Deal with the case when "ALL" is not added to the cboxUsrList
            if (!cboxUsrList.Items.Contains("ALL"))
            {
                cboxUsrList.Items.Add("ALL");
            }

            messageText.Focus();
            if (!cboxUsrList.Items.Contains(txtContact.Text))
            {
                cboxUsrList.Items.Add(txtContact.Text);
            }

            // Update buttons
            btnLogin.IsEnabled = false;
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = true;
            btnSendMessage.IsEnabled = true;
            btnLogout.IsEnabled = false;
        }

        /*  -- Method Header Comment
        Name	:   BtnDisconnect_Click
        Purpose :   Disconnect from the another user
        Inputs	:	object              sender
                    RoutedEventArgs     e
        Outputs	:	Display message 
        Returns	:	Nothing 
        */
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            // Deal with the case when the user is trying to disconnect all - not allowed
            if(txtContact.Text == "ALL")
            {
                lstChat.Items.Add("Ops, cannot disconnect all, try to disconnect one by one");
                return;
            }

            // Ping Server to disconnect
            string serverRespMsg = ConnectClient(stream, "D" + txtUserIP.Text + "," + txtContact.Text);

            if (!serverRespMsg.StartsWith("You are disconnected with "))
            {
                return;
            }

            cboxUsrList.Items.Remove(txtContact.Text);
            txtContact.Text = "";

            // Update buttons
            btnLogin.IsEnabled = false;
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = true;
            btnSendMessage.IsEnabled = true;
            btnLogout.IsEnabled = true;

            // Deal with the case when no more contact but ALL left which needs to be removed
            if (cboxUsrList.Items.Count == 1 && cboxUsrList.Items.Contains("ALL"))
            {
                cboxUsrList.Items.Remove("ALL");
                txtContact.Text = "";
            }
        }

        /*  -- Method Header Comment
        Name	:   OnSelectionChanged
        Purpose :   Update txtContact when cboxUsrList selection is changed
        Inputs	:	object              sender
                    SelectionChangedEventArgs     e
        Outputs	:	Nothing 
        Returns	:	Nothing 
        */
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update txtContact when cboxUsrList selection is changed.
            if (cboxUsrList.SelectedItem == null)
            {
                return;
            }
            txtContact.Text = cboxUsrList.SelectedItem.ToString();
        }

        /*  -- Method Header Comment
        Name	:   TxtContactChangedEventHandler
        Purpose :   For the case when the client is connected with the contact that is defined under the txtContact textbox If the contact is not conected wit the client, disable the send message button, otherwise enable
        Inputs	:	object              sender
                    TextChangedEventArgs     e
        Outputs	:	Nothing 
        Returns	:	Nothing 
        */
        private void TxtContactChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            if (cboxUsrList.Items.Contains(txtContact.Text))
            {
                btnSendMessage.IsEnabled = true;
            }
            else
            {
                btnSendMessage.IsEnabled = false;
            }
        }

        /*  -- Method Header Comment
        Name	:   TxtMsgChangedEventHandler
        Purpose :   For the case when the client is connected with the contact that is defined under the txtMsg textbox If the contact is not conected wit the client, disable the send message button, otherwise enable
        Inputs	:	object              sender
                    TextChangedEventArgs     e
        Outputs	:	Nothing 
        Returns	:	Nothing 
        */
        private void TxtMsgChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            if (cboxUsrList.Items.Contains(txtContact.Text))
            {
                btnSendMessage.IsEnabled = true;
            }
            else
            {
                btnSendMessage.IsEnabled = false;
            }
        }

    }
}
