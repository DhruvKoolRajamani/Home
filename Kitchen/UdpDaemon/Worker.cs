using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;
using System.Net;

namespace UdpDaemon
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private string remoteIp = "192.168.1.25";
        private int localPort = 4210;
        private UdpClient udpClient;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                udpClient = new UdpClient(localPort);

                udpClient.Connect(remoteIp, localPort);
                Byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");
                udpClient.Send(sendBytes, sendBytes.Length);
                _logger.LogInformation($"Sent {Encoding.ASCII.GetString(sendBytes)}");

                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                string returnData = Encoding.ASCII.GetString(receiveBytes);
                _logger.LogInformation($"Received {returnData}");

                udpClient.Close();
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
