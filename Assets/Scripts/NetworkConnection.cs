using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Security.Cryptography;

public class NetworkConnection : MonoBehaviour
{
    private static bool created = false;

    private string username = "";
    private string password = "";
    private string ipString = "";

    private Queue<string> recieveQueue;

    void Awake()
    {
        if (created)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            created = true;
        }
    }

    // Use this for initialization
    void Start()
    {
        recieveQueue = new Queue<string>();
    }

    // Update is called once per frame
    void Update()
    {
        if(recieveQueue.Count > 0)
        {
            string content = recieveQueue.Dequeue();
            if (content.StartsWith("Login"))
            {
                Debug.Log("LI");
                string[] data = content.Split('\n');
                string res = data[1];
                if (!res.Equals("Rejected"))
                {
                    Application.LoadLevel("GameplayScene");
                }
            }
            else if (content.StartsWith("PlayerInfo"))
            {
                Debug.Log("PI");
                string[] data = content.Split('\n');
                Transform pi = GameObject.Find("Canvas").transform.FindChild("PlayerInfo" + data[1]);
                if (pi == null) Debug.Log("oops" + data[1]);
                pi.FindChild("Name").GetComponent<Text>().text = data[2];
                pi.FindChild("Chips").GetComponent<Text>().text = data[3];
                pi.FindChild("State").GetComponent<Text>().text = data[4];
                pi.FindChild("H1").GetComponent<Card>().setCard(Convert.ToInt32(data[5]));
                pi.FindChild("H2").GetComponent<Card>().setCard(Convert.ToInt32(data[6]));
            }
            else if (content.StartsWith("PlayerClear"))
            {
                Debug.Log("PC");
                string[] data = content.Split('\n');
                Transform pi = GameObject.Find("Canvas").transform.FindChild("PlayerInfo" + data[1]);
                pi.FindChild("Name").GetComponent<Text>().text = string.Empty;
                pi.FindChild("Chips").GetComponent<Text>().text = string.Empty;
                pi.FindChild("State").GetComponent<Text>().text = "Open Seat";
                pi.FindChild("H1").GetComponent<Card>().setCard(-1);
                pi.FindChild("H2").GetComponent<Card>().setCard(-1);
            }
            else if (content.StartsWith("CardsInfo"))
            {
                Debug.Log("CI");
                string[] data = content.Split('\n');
                GameObject.Find("Canvas").transform.FindChild("Flop1").GetComponent<Card>().setCard(Convert.ToInt32(data[1]));
                GameObject.Find("Canvas").transform.FindChild("Flop2").GetComponent<Card>().setCard(Convert.ToInt32(data[2]));
                GameObject.Find("Canvas").transform.FindChild("Flop3").GetComponent<Card>().setCard(Convert.ToInt32(data[3]));
                GameObject.Find("Canvas").transform.FindChild("Turn").GetComponent<Card>().setCard(Convert.ToInt32(data[4]));
                GameObject.Find("Canvas").transform.FindChild("River").GetComponent<Card>().setCard(Convert.ToInt32(data[5]));
            }
            else if (content.StartsWith("HandInfo"))
            {
                Debug.Log("HI");
                string[] data = content.Split('\n');
                GameObject.Find("Canvas").transform.FindChild("Hand1").GetComponent<Card>().setCard(Convert.ToInt32(data[1]));
                GameObject.Find("Canvas").transform.FindChild("Hand2").GetComponent<Card>().setCard(Convert.ToInt32(data[2]));
            }
            else if (content.StartsWith("CardsClear"))
            {
                Debug.Log("CC");
                GameObject.Find("Canvas").transform.FindChild("Hand1").GetComponent<Card>().setCard(-1);
                GameObject.Find("Canvas").transform.FindChild("Hand2").GetComponent<Card>().setCard(-1);
                GameObject.Find("Canvas").transform.FindChild("Flop1").GetComponent<Card>().setCard(-1);
                GameObject.Find("Canvas").transform.FindChild("Flop2").GetComponent<Card>().setCard(-1);
                GameObject.Find("Canvas").transform.FindChild("Flop3").GetComponent<Card>().setCard(-1);
                GameObject.Find("Canvas").transform.FindChild("Turn").GetComponent<Card>().setCard(-1);
                GameObject.Find("Canvas").transform.FindChild("River").GetComponent<Card>().setCard(-1);
            }
            else if (content.StartsWith("ChipInfo"))
            {
                Debug.Log("ChI");
                string[] data = content.Split('\n');
                GameObject.Find("Canvas").transform.FindChild("ChipsAmount").GetComponent<Text>().text = data[1];
            }
            else if (content.StartsWith("PotInfo"))
            {
                Debug.Log("PoI");
                string[] data = content.Split('\n');
                GameObject.Find("Canvas").transform.FindChild("PotAmount").GetComponent<Text>().text = data[1];
                GameObject.Find("Canvas").transform.FindChild("MaxBetAmount").GetComponent<Text>().text = data[2];
                if (data[2].Equals("0"))
                {
                    GameObject.Find("Canvas").transform.FindChild("BetRaiseButton").transform.FindChild("Text").GetComponent<Text>().text = "Bet";
                    GameObject.Find("Canvas").transform.FindChild("InputField").transform.FindChild("Placeholder").GetComponent<Text>().text = "Amount to bet";
                }
                else
                {
                    GameObject.Find("Canvas").transform.FindChild("BetRaiseButton").transform.FindChild("Text").GetComponent<Text>().text = "Raise";
                    GameObject.Find("Canvas").transform.FindChild("InputField").transform.FindChild("Placeholder").GetComponent<Text>().text = "Amount to raise by";
                }
            }
            else if (content.StartsWith("CallInfo"))
            {
                Debug.Log("ClI");
                string[] data = content.Split('\n');
                GameObject.Find("Canvas").transform.FindChild("CallAmount").GetComponent<Text>().text = data[1];
                if (data[1].Equals("0"))
                {
                    GameObject.Find("Canvas").transform.FindChild("CheckCallButton").transform.FindChild("Text").GetComponent<Text>().text = "Check";
                }
                else
                {
                    GameObject.Find("Canvas").transform.FindChild("CheckCallButton").transform.FindChild("Text").GetComponent<Text>().text = "Call";
                }
            }
        }
    }

    public string GetHash(string password)
    {
        MD5 md5 = System.Security.Cryptography.MD5.Create();
        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(password);
        byte[] hash = md5.ComputeHash(inputBytes);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2"));
        }
        return sb.ToString();
    }

    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    // The port number for the remote device.
    private const int port = 11000;

    // ManualResetEvent instances signal completion.
    /*private ManualResetEvent connectDone =
        new ManualResetEvent(false);
    private ManualResetEvent sendDone =
        new ManualResetEvent(false);
    private ManualResetEvent receiveDone =
        new ManualResetEvent(false);*/

    // The response from the remote device.
    //private String response = String.Empty;
    Socket client = null;

    public void NewClient(string un, string pw, string ip)
    {
        try
        {
            username = un;
            password = pw;
            ipString = ip;
            StartClient();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void CloseClient()
    {
        if (client == null) return;
        try
        {
            // Release the socket.
            client.Shutdown(SocketShutdown.Both);
            client.Close();
            client = null;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void StartClient()
    {
        // Connect to a remote device.
        try
        {
            // Establish the remote endpoint for the socket.
            IPAddress ipAddress = IPAddress.Parse(ipString);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);

            //CloseClient(client);
            //if (response.Equals("Login\nRejected<EOF>"))
            //{
            //    return false;
            //}
            //return true;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
        //return false;
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            //Socket client = (Socket)ar.AsyncState;

            // Complete the connection.
            client.EndConnect(ar);

            //Console.WriteLine("Socket connected to {0}",
            //    client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.
            //connectDone.Set();
            Debug.Log("Connected");
            Send("Login\n" + username + "\n" + GetHash(password) + "\n<EOF>");
            Receive();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void Receive()
    {
        try
        {
            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = client;

            // Begin receiving the data from the remote device.
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            //Socket client = state.workSocket;

            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                string content = state.sb.ToString();
                while (content.IndexOf("<EOF>") > -1)
                {
                    string str = content.Substring(0, content.IndexOf("<EOF>"));
                    Debug.Log(str);
                    recieveQueue.Enqueue(str);
                    state.sb.Remove(0, content.IndexOf("<EOF>")+5);
                    content = state.sb.ToString();
                }
                // Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);

            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    public void Send(String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            //Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);
            //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            // Signal that all bytes have been sent.
            //sendDone.Set();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

}
