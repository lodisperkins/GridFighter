using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Packages.Replica.Bridge.Editor
{
    public class Connection
    {
        private static readonly Uri uri = new Uri("ws://localhost:13987");
        private ClientWebSocket client;
        public readonly List<string> JsonCommands = new List<string>();
        private Task wsTask;
        private readonly string projectName = Application.productName;
        private readonly string platformVersion = Application.unityVersion;
        private readonly PackageInfo thisPackageInfo = PlatformUtil.ThisPackageInfo();

        public void Connect()
        {
            if (IsRunning()) return;
            wsTask = Task.Factory.StartNew(StartListening);
        }

        public bool IsRunning()
        {
            return client != null
                   && client.State == WebSocketState.Open;
        }
       
        private async void StartListening()
        {
            client = new ClientWebSocket();
            try
            {
                await client.ConnectAsync(uri, CancellationToken.None);
                Debug.Log("Replica Link Connected");
                await SendAsyncObject(new HandshakeMessage()
                {
                    projectName = projectName,
                    platformVersion = platformVersion,
                    pluginName = thisPackageInfo != null ? thisPackageInfo.name : "unknown",
                    pluginVersion = thisPackageInfo != null ? thisPackageInfo.version : "unknown" ,
                });
                try
                {
                    await ReceiverLoop();
                }
                catch (Exception)
                {
                    Debug.Log("Replica Link Disconnected");
                    throw;
                }
            }
            catch (Exception)
            {
                client.Dispose();
                client = null;
            }
        }

        private async Task SendAsyncObject(object obj)
        {
            var json = JsonUtility.ToJson(obj);
            var encodedJson = Encoding.UTF8.GetBytes(json);
            var buffer = new ArraySegment<Byte>(encodedJson, 0, encodedJson.Length);
            await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        private async Task ReceiverLoop()
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            do
            {
                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await client.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    string json;
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        json = await reader.ReadToEndAsync();
                    }

                    JsonCommands.Add(json);
                }
            } while (true);
        }
        
    } 
}