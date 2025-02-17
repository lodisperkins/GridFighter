using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class NetworkMessengerBehaviour : MonoBehaviour
{
    // Singleton instance so the script can be easily accessed from anywhere
    public static NetworkMessengerBehaviour Instance { get; private set; }

    // Event triggered when a message is received
    public event Action<string> OnMessageReceived;

    // Event triggered when a message is successfully sent
    public event Action<string> OnMessageSend;

    // The UDP client that will send and receive messages
    private UdpClient udpClient;

    // Stores the endpoint (IP + Port) of the remote client (the one you're communicating with)
    private IPEndPoint remoteEndPoint;

    // Stores the endpoint of this client (where we listen for messages)
    private IPEndPoint localEndPoint;

    // A separate thread for listening to incoming messages
    private Thread receiveThread;

    // A flag to track whether listening is currently active
    private bool isListening = false;

    private void Awake()
    {
        // Implement Singleton Pattern (Ensures only one instance of this script exists)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object alive across scene changes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
    }

    /// <summary>
    /// Initializes the connection settings for sending and receiving messages.
    /// </summary>
    /// <param name="localIP">The IP address this client will listen on.</param>
    /// <param name="localPort">The port this client will listen on.</param>
    /// <param name="remoteIP">The IP address of the remote client (the other person).</param>
    /// <param name="remotePort">The port of the remote client.</param>
    public void SetConnection(string localIP, int localPort, string remoteIP, int remotePort)
    {
        // Define the local endpoint (where this client listens for incoming messages)
        localEndPoint = new IPEndPoint(IPAddress.Parse(localIP), localPort);

        // Define the remote endpoint (where messages are sent)
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);

        // Create a new UDP client and bind it to the local endpoint
        udpClient = new UdpClient(localEndPoint);

        // Automatically start listening for messages after setting up the connection
        SetIsListening(true);
    }

    /// <summary>
    /// Sends a text message to the remote client.
    /// </summary>
    /// <param name="message">The message string to send.</param>
    public void SendMessageToClient(string message)
    {
        // Ensure the connection is properly initialized before sending messages
        if (udpClient == null || remoteEndPoint == null)
        {
            Debug.LogWarning("NetworkMessengerBehaviour is not initialized. Call SetConnection() first.");
            return;
        }

        try
        {
            // Convert the string message into a byte array (required for UDP transmission)
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Send the byte data to the remote endpoint (other client)
            udpClient.Send(data, data.Length, remoteEndPoint);

            // Trigger the OnMessageSend event so other scripts know a message was sent
            OnMessageSend?.Invoke(message);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message: " + e.Message);
        }
    }

    /// <summary>
    /// Toggles the message listening behavior on or off.
    /// </summary>
    /// <param name="state">True to enable listening, False to disable it.</param>
    public void SetIsListening(bool state)
    {
        if (state && !isListening)
        {
            // If listening is not already active, start it
            isListening = true;

            // Create a new thread for receiving messages so it doesn't block the Unity main thread
            receiveThread = new Thread(new ThreadStart(ReceiveMessages))
            {
                IsBackground = true // Mark it as a background thread so it automatically stops when Unity quits
            };
            receiveThread.Start();
        }
        else if (!state && isListening)
        {
            // If listening is active and we want to stop it, do so
            isListening = false;

            // Stop the receiving thread to prevent errors
            receiveThread?.Abort();
        }
    }

    /// <summary>
    /// Listens for incoming messages and processes them.
    /// This method runs on a separate thread to avoid freezing Unity.
    /// </summary>
    private void ReceiveMessages()
    {
        while (isListening)
        {
            try
            {
                // This endpoint will store the sender's information (IP and port)
                IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);

                // Wait for an incoming message (this call blocks until data is received)
                byte[] receivedData = udpClient.Receive(ref senderEndPoint);

                // Convert the received byte array into a readable string
                string receivedMessage = Encoding.UTF8.GetString(receivedData);

                // Trigger the OnMessageReceived event so other scripts can respond to the message
                OnMessageReceived?.Invoke(receivedMessage);

                // Log the received message for debugging
                Debug.Log("Message received: " + receivedMessage);
            }
            catch (Exception e)
            {
                if (isListening) // Prevent errors if the thread is stopped intentionally
                {
                    Debug.LogError("Error receiving message: " + e.Message);
                }
            }
        }
    }

    /// <summary>
    /// Ensures proper cleanup when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        // Stop listening before destruction to prevent memory leaks
        SetIsListening(false);

        // Close the UDP client to free up the port
        udpClient?.Close();
    }
}
