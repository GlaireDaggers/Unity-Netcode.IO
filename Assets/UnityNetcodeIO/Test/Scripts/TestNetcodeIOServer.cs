using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityNetcodeIO;
using NetcodeIO.NET;

public class TestNetcodeIOServer : MonoBehaviour
{
	static readonly byte[] privateKey = new byte[]
	{
		0x60, 0x6a, 0xbe, 0x6e, 0xc9, 0x19, 0x10, 0xea,
		0x9a, 0x65, 0x62, 0xf6, 0x6f, 0x2b, 0x30, 0xe4,
		0x43, 0x71, 0xd6, 0x2c, 0xd1, 0x99, 0x27, 0x26,
		0x6b, 0x3c, 0x60, 0xf4, 0xb7, 0x15, 0xab, 0xa1,
	};

	public Text outputText;
	public Text NumClientsText;

	public string PublicIP = "127.0.0.1";
	public int Port = 40000;
	public int MaxClients = 256;

	private NetcodeServer server;
	private int clients = 0;

	private void Start()
	{
		server = UnityNetcode.CreateServer(PublicIP, Port, 0x1122334455667788L, MaxClients, privateKey);

		server.ClientConnectedEvent.AddListener(Server_OnClientConnected);
		server.ClientDisconnectedEvent.AddListener(Server_OnClientDisconnected);
		server.ClientMessageEvent.AddListener(Server_OnClientMessage);

		server.StartServer();

		logLine("Server started");
	}

	private void OnDestroy()
	{
		server.Dispose();
	}

	private void Server_OnClientMessage(RemoteClient client, ByteBuffer payload)
	{
		// just redirect payload back
		server.SendPayload(client, payload);
	}

	private void Server_OnClientConnected(RemoteClient client)
	{
		logLine("Client connected: " + client.RemoteEndpoint.ToString());

		clients++;
		NumClientsText.text = clients.ToString() + "/" + MaxClients.ToString();
	}

	private void Server_OnClientDisconnected(RemoteClient client)
	{
		logLine("Client disconnected: " + client.RemoteEndpoint.ToString());

		clients--;
		NumClientsText.text = clients.ToString() + "/" + MaxClients.ToString();
	}

	protected void log(string text)
	{
		outputText.text += text;
	}

	protected void logLine(string text)
	{
		log(text + "\n");
	}
}
