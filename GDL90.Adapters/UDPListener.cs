#pragma warning disable IDE0090

using System;
using System.Net;
using System.Net.Sockets;

namespace GDL90.Adapters
{
    public class UDPListener
    {
        //private readonly GDL90.Core.MessageStreamParser messageParser;
        private readonly AsyncCallback ReceiveUDPDataCallbackDelegate;
        //public readonly MemoryStream NetworkMessageStream = new MemoryStream();
        public struct UdpState
        {
            public UdpClient udpClient;
            public IPEndPoint? ipEndpoint;
        }

        public UDPListener()
        {
            ReceiveUDPDataCallbackDelegate = new AsyncCallback(ReceiveUDPDataCallback);
        }

        public void ReceiveUDPDataCallback(IAsyncResult ar) {
            if (ar.AsyncState == null)
            {
                throw new ArgumentException("Missing asyncState. Expected a UdpState struct.");
            }

            UdpState asyncState = (UdpState)ar.AsyncState;

            // push these bytes into the GDL90 lib message stream parser instance.
            byte[] receiveBytes = asyncState.udpClient.EndReceive(ar, ref asyncState.ipEndpoint);
            
            // if (NetworkMessageStream.CanWrite)
            // {
            //     NetworkMessageStream.Write(receiveBytes, 0, receiveBytes.Length);    
            // }
            // else
            // {
            //     throw new IOException("NetworkMessageStream is not writable.");
            // }

            asyncState.udpClient.BeginReceive(ReceiveUDPDataCallbackDelegate, ar.AsyncState);
        }

        public void StartListening(int udpListenPort) {
            UdpClient udpClient = new UdpClient(udpListenPort);
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
            UdpState asyncState = new UdpState
            {
                udpClient = udpClient,
                ipEndpoint = ipEndPoint
            };
            udpClient.BeginReceive(ReceiveUDPDataCallbackDelegate, asyncState);
        }
    }
}