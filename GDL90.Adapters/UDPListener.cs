#pragma warning disable IDE0090

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GDL90.Adapters
{
    public class UDPListener
    {
        public ChannelWriter<byte[]> messageParserChannelWriter;

        private readonly AsyncCallback ReceiveUDPDataCallbackDelegate;

        public struct UdpState
        {
            public UdpClient udpClient;
            public IPEndPoint? ipEndpoint;
        }

        public UDPListener(ChannelWriter<byte[]> messageParserChannelWriter)
        {
            this.messageParserChannelWriter = messageParserChannelWriter;
            ReceiveUDPDataCallbackDelegate = new AsyncCallback(ReceiveUDPDataCallback);
        }

        public void ReceiveUDPDataCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null)
            {
                throw new ArgumentException("Missing asyncState. Expected a UdpState struct.");
            }

            UdpState asyncState = (UdpState)ar.AsyncState;
            byte[] receiveBytes = asyncState.udpClient.EndReceive(ar, ref asyncState.ipEndpoint);
            //await messageParserChannelWriter.WriteAsync(receiveBytes.AsMemory(0, receiveBytes.Length).ToArray());
            if (messageParserChannelWriter.TryWrite(receiveBytes.AsMemory(0, receiveBytes.Length).ToArray()) == true)
            {
                asyncState.udpClient.BeginReceive(ReceiveUDPDataCallbackDelegate, ar.AsyncState);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to write UDP data to channel. Channel may be full or closed. Closing UDP socket.");
                asyncState.udpClient.Close();
                //throw new InvalidOperationException("Failed to write UDP data to channel. Channel may be full or closed.");
            }
        }

        public Task StartListening(int udpListenPort) {
            UdpClient udpClient = new UdpClient(udpListenPort);
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
            UdpState asyncState = new UdpState
            {
                udpClient = udpClient,
                ipEndpoint = ipEndPoint
            };
            udpClient.BeginReceive(ReceiveUDPDataCallbackDelegate, asyncState);
            return Task.CompletedTask;
        }
    }
}