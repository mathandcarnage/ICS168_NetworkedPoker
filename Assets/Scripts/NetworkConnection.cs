﻿using UnityEngine;
using System.Collections;
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

    }

    // Update is called once per frame
    void Update()
    {

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
    private ManualResetEvent connectDone =
        new ManualResetEvent(false);
    private ManualResetEvent sendDone =
        new ManualResetEvent(false);
    private ManualResetEvent receiveDone =
        new ManualResetEvent(false);

    // The response from the remote device.
    private String response = String.Empty;
    //Socket client = null;

    public bool NewClient(string un, string pw, string ip)
    {
        username = un;
        password = pw;
        ipString = ip;
        return StartClient();
    }

    private void CloseClient(Socket client)
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
            //Console.WriteLine(e.ToString());
        }
    }

    private bool StartClient()
    {
        // Connect to a remote device.
        try
        {
            // Establish the remote endpoint for the socket.
            IPAddress ipAddress = IPAddress.Parse(ipString);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            Socket client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();
            Debug.Log("Connected");

            // Send test data to the remote device.
            Send(client, "Login\n" + username + "\n" + GetHash(password) + "\n<EOF>");
            sendDone.WaitOne();
            Debug.Log("Sent");
            // Receive the response from the remote device.
            Receive(client);
            receiveDone.WaitOne();
            Debug.Log("Recieved");

            // Write the response to the console.
            //Console.WriteLine("Response received : {0}", response);

            CloseClient(client);
            if(response.Equals("Login\nRejected<EOF>"))
            {
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            //Console.WriteLine(e.ToString());
        }
        return false;
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.
            client.EndConnect(ar);

            //Console.WriteLine("Socket connected to {0}",
            //    client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.
            connectDone.Set();
        }
        catch (Exception e)
        {
            //Console.WriteLine(e.ToString());
        }
    }

    private void Receive(Socket client)
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
            //Console.WriteLine(e.ToString());
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                // All the data has arrived; put it in response.
                if (state.sb.Length > 1)
                {
                    response = state.sb.ToString();
                }
                // Signal that all bytes have been received.
                receiveDone.Set();
            }
        }
        catch (Exception e)
        {
            //Console.WriteLine(e.ToString());
        }
    }

    private void Send(Socket client, String data)
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
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);
            //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            // Signal that all bytes have been sent.
            sendDone.Set();
        }
        catch (Exception e)
        {
            //Console.WriteLine(e.ToString());
        }
    }

}
