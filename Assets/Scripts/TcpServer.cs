using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

public class TcpServer : MonoBehaviour { 

    public class AnchorRange {
        public string id;
        public double dist;
    }

    public delegate void PacketEvent(string tagID, List<AnchorRange> ranges);
    public event PacketEvent OnPacketSent;

    Thread startThread;

    TcpListener myList;

    public int port = 9900;

    Socket socket;

    Thread readThread;

    public string number; 

    void Start() {
        //TCP Server will lock up Unity if not started
        //in its own thread
        startThread = new Thread(new ThreadStart(StartServer));
        startThread.IsBackground = true;
        startThread.Start(); 
    }

    void OnApplicationQuit() {
        stopThread(startThread);
    }

    private void stopThread(Thread thread) {
        if (thread.IsAlive) {
            thread.Abort();
        }
        if (myList != null)
            myList.Stop();
    }

    private void StartServer() {
        try {
            myList = new TcpListener(IPAddress.Any, port);
            myList.Start();
            print("server started");

            print("Waiting for connection......");
            socket = myList.AcceptSocket();
            print("Connection accepted on port: " + port.ToString());

            byte[] bytes = new byte[1024];
            StringBuilder msg = new StringBuilder(128);
            while (true) {
                int len = socket.Receive(bytes);
                if (len == 0)
                    break;

                for (int i = 0; i < len; i++) {
                    if (bytes[i] == '\n') {
                        continue;
                    } else if (bytes[i] == '\r') {
                        ParsePacket(msg.ToString());
                        msg.Length = 0;
                    } else {
                        msg.Append((char)bytes[i]);
                    }
                }
            }
        } catch (SocketException e) {
            print(e.StackTrace);
        }
        print("Connection closed on port: " + port.ToString());
    }

	void ParsePacket(string csvString) {
        // * 616A 57BB:0.58 44E8:1.49 B21A:1.14 ...
        string[] parts = csvString.Split(' ');
//		print ("ParsePacket: " + csvString);

        string tagid = parts[1];
        List<AnchorRange> results = new List<AnchorRange>();
        for (int i = 2; i < parts.Length; i++) {
            string[] ranges = parts[i].Split(':');
            AnchorRange r = new AnchorRange();
            r.id = ranges[0];
            r.dist = Double.Parse(ranges[1]);
            results.Add(r);
        }
        OnPacketSent(tagid, results);
    }

}