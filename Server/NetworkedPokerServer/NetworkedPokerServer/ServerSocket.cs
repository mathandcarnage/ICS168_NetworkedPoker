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

            public string myName = string.Empty;
        }

        static Dictionary<string, GameState> games = new Dictionary<string, GameState>();

        static HashSet<string> connectedPlayers = new HashSet<string>();

        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static DatabaseConnection database = new DatabaseConnection();

        public static void StartListening()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine("My IP:" + ip.ToString());
                }
            }

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

                        state.sb.Remove(0, content.IndexOf("<EOF>") + 5);

                        if (content.StartsWith("Login"))
                        {
                            string[] data = content.Split('\n');
                            string un = data[1];
                            string pw = data[2];

                            if (database.UserIsInDatabase(un))
                            {
                                if(connectedPlayers.Contains(un))
                                {
                                    Console.WriteLine("Already Connected");
                                    Send(handler, "Login\nAlreadyConnected\n<EOF>");

                                    handler.Shutdown(SocketShutdown.Both);
                                    handler.Close();
                                    return;
                                }
                                else if (database.IsCorrectPassword(un, pw))
                                {
                                    Console.WriteLine("Accepted");
                                    Send(handler, "Login\nAccepted\n<EOF>");
                                    Send(handler, printServers());
                                    Send(handler, "StoredChips\n" + database.getNumberOfChips(un) + "\n<EOF>");
                                    state.myName = un;
                                    connectedPlayers.Add(un);
                                }
                                else
                                {
                                    Console.WriteLine("Rejected");
                                    Send(handler, "Login\nRejected\n<EOF>");

                                    handler.Shutdown(SocketShutdown.Both);
                                    handler.Close();
                                    return;
                                }
                            }
                            else
                            {
                                database.AddNewUser(un, pw);
                                Console.WriteLine("Created");
                                Send(handler, "Login\nCreated\n<EOF>");
                                Send(handler, printServers());
                                Send(handler, "StoredChips\n" + database.getNumberOfChips(un) + "\n<EOF>");
                                state.myName = un;
                                connectedPlayers.Add(un);
                            }
                        }
                        else if (content.StartsWith("GetServers"))
                        {
                            Send(handler, printServers());
                        }
                        else if (content.StartsWith("AddChips"))
                        {
                            string[] data = content.Split('\n');
                            int chips = database.getNumberOfChips(state.myName) + Convert.ToInt32(data[1]);
                            database.UpdateChips(state.myName, chips);
                            Send(handler, "StoredChips\n" + chips + "\n<EOF>");
                        }
                        else if(content.StartsWith("Host"))
                        {
                            string[] data = content.Split('\n');
                            if (games.ContainsKey(data[1]))
                            {
                                Send(handler, "Join\nAlreadyExists\n<EOF>");
                            }
                            else
                            {
                                if (Convert.ToInt32(data[2]) >= 2 && Convert.ToInt32(data[2]) <= 8 && Convert.ToInt32(data[3]) % 100 == 0)
                                {
                                    GameState gs = new GameState(Convert.ToInt32(data[2]), Convert.ToInt32(data[3]),data[1]);
                                    games.Add(data[1], gs);
                                    int chips = database.getNumberOfChips(state.myName);
                                    int ix = gs.Join(handler, state.myName, chips);
                                    if (ix == -1)
                                    {
                                        if (chips < gs.buyIn)
                                        {
                                            Send(handler, "Join\nNotEnoughChips\n<EOF>");
                                        }
                                        else
                                        {
                                            Send(handler, "Join\nFull\n<EOF>");
                                        }
                                    }
                                    else
                                    {
                                        state.myIndex = ix;
                                        state.myState = gs;
                                        database.UpdateChips(state.myName, chips - gs.buyIn);
                                    }
                                }
                                else
                                {
                                    Send(handler, "Join\nWrongValue\n<EOF>");
                                }
                            }
                        }
                        else if (content.StartsWith("Join"))
                        {
                            string[] data = content.Split('\n');
                            if(games.ContainsKey(data[1]))
                            {
                                int chips = database.getNumberOfChips(state.myName);
                                int ix = games[data[1]].Join(handler,state.myName,chips);
                                if (ix == -1)
                                {
                                    if (chips < games[data[1]].buyIn)
                                    {
                                        Send(handler, "Join\nNotEnoughChips\n<EOF>");
                                    }
                                    else
                                    {
                                        Send(handler, "Join\nFull\n<EOF>");
                                    }
                                }
                                else
                                {
                                    state.myIndex = ix;
                                    state.myState = games[data[1]];
                                    database.UpdateChips(state.myName, chips - games[data[1]].buyIn);
                                }
                            }
                            else
                            {
                                Send(handler, "Join\nNoServer\n<EOF>");
                            }
                        }
                        else if(content.StartsWith("Fold"))
                        {
                            state.myState.Fold(state.myIndex);
                        }
                        else if(content.StartsWith("Call"))
                        {
                            state.myState.Call(state.myIndex);
                        }
                        else if(content.StartsWith("Raise"))
                        {
                            string[] data = content.Split('\n');
                            int amt = Convert.ToInt32(data[1]);
                            state.myState.Raise(state.myIndex, amt);
                        }
                        else if (content.StartsWith("Leave"))
                        {
                            int chips = state.myState.Leave(state.myIndex);
                            chips += database.getNumberOfChips(state.myName);
                            database.UpdateChips(state.myName, chips);
                            if(state.myState.connectedPlayers <= 0)
                            {
                                games.Remove(state.myState.serverName);
                            }
                            state.myState = null;
                            Send(handler, printServers());
                            Send(handler, "StoredChips\n" + database.getNumberOfChips(state.myName) + "\n<EOF>");
                        }
                        else if (content.StartsWith("Disconnect"))
                        {
                            if (state.myState != null)
                            {
                                int chips = state.myState.Leave(state.myIndex);
                                chips += database.getNumberOfChips(state.myName);
                                database.UpdateChips(state.myName, chips);
                                if (state.myState.connectedPlayers <= 0)
                                {
                                    games.Remove(state.myState.serverName);
                                }
                                state.myState = null;
                            }
                            connectedPlayers.Remove(state.myName);
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close();
                            return;
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

        public static string printServers()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Servers\n");
            sb.Append(games.Count() + "\n");
            foreach(string s in games.Keys)
            {
                sb.Append(s + "\n");
                sb.Append(games[s].numPlayers + "\n");
                sb.Append(games[s].maxPlayers + "\n");
                sb.Append(games[s].buyIn + "\n");
            }

            return sb.ToString() + "<EOF>";
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
