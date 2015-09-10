using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkedPokerServer
{
    class ServerSocket
    {

        // State object for reading client data asynchronously
        public class StateObject
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();

            public GameState myState = null;

            public int myIndex = -1;
        }

        static GameState game = new GameState();

        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static DatabaseConnection database = new DatabaseConnection();

        public static void StartListening()
        {
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 11000);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            try
            {
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read 
                    // more data.
                    content = state.sb.ToString();
                    if (content.IndexOf("<EOF>") > -1)
                    {
                        // All the data has been read from the 
                        // client. Display it on the console.
                        Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                            content.Length, content);

                        state.sb.Clear();

                        if (content.StartsWith("Login"))
                        {
                            string[] data = content.Split('\n');
                            string un = data[1];
                            string pw = data[2];

                            if (database.UserIsInDatabase(un))
                            {
                                if (database.IsCorrectPassword(un, pw))
                                {
                                    Console.WriteLine("Accepted");
                                    Send(handler, "Login\nAccepted\n<EOF>");
                                    int ix = game.Join(handler, data[1], 100);
                                    if(ix == -1)
                                    {
                                        handler.Shutdown(SocketShutdown.Both);
                                        handler.Close();
                                    }
                                    else
                                    {
                                        state.myIndex = ix;
                                        state.myState = game;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Rejected");
                                    Send(handler, "Login\nRejected\n<EOF>");

                                    handler.Shutdown(SocketShutdown.Both);
                                    handler.Close();
                                }
                            }
                            else
                            {
                                database.AddNewUser(un, pw);
                                Console.WriteLine("Created");
                                Send(handler, "Login\nCreated\n<EOF>");
                                int ix = game.Join(handler, data[1], 100);
                                if (ix == -1)
                                {
                                    handler.Shutdown(SocketShutdown.Both);
                                    handler.Close();
                                }
                                else
                                {
                                    state.myIndex = ix;
                                    state.myState = game;
                                }
                            }
                        }
                        else if(content.StartsWith("Fold"))
                        {

                        }
                        else if(content.StartsWith("Call"))
                        {

                        }
                        else if(content.StartsWith("Raise"))
                        {

                        }
                    }
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }
    }
}
