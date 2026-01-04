using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClipFlow.Desktop.Services
{
    public sealed class SingleInstanceService : IDisposable
    {
        private readonly string _mutexName;
        private readonly string _pipeName;
        private Mutex? _mutex;
        public event Action<string>? MessageReceived;

        public SingleInstanceService(string appId)
        {
            _mutexName = $"{appId}.Mutex";
            _pipeName = $"{appId}.Pipe";
        }

        public bool CheckIsFirstInstance()
        {
            _mutex = new Mutex(true, _mutexName, out bool createdNew);
            if (createdNew) StartPipeServer();
            return createdNew;
        }

        public async Task SendMessageToFirstInstanceAsync(string message)
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            try
            {
                await client.ConnectAsync(500);
                using var writer = new StreamWriter(client, Encoding.UTF8);
                await writer.WriteLineAsync(message);
            }
            catch { /* 第一实例可能正在关闭 */ }
        }

        private void StartPipeServer()
        {
            Task.Run(async () => {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In);
                        await server.WaitForConnectionAsync();
                        using var reader = new StreamReader(server, Encoding.UTF8);
                        var msg = await reader.ReadLineAsync();
                        if (msg != null) MessageReceived?.Invoke(msg);
                    }
                    catch { await Task.Delay(1000); }
                }
            });
        }

        public void Dispose() => _mutex?.Dispose();
    }
}
