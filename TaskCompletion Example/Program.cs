using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TaskCompletion_Example
{
    class Program
    {
        private static Thread SendThread    { get; } = new Thread(new ParameterizedThreadStart(SendLoop));
        private static Thread RecieveThread { get; } = new Thread(new ParameterizedThreadStart(RecieveLoop));

        private static CancellationTokenSource CancellationToken { get; } = new CancellationTokenSource();

        private static BlockingCollection<RequestObject> SendQueue       { get; } = new BlockingCollection<RequestObject>();
        private static BlockingCollection<RequestObject> ProcessingQueue { get; } = new BlockingCollection<RequestObject>();

        public static async Task Main()
        {
            SendThread.Start(CancellationToken.Token);
            RecieveThread.Start(CancellationToken.Token);

            Console.WriteLine("Welcome!  You can send anything to simulate a process or 'q' to quit");

            while (true)
            {
                string command = Console.ReadLine();

                if (command.ToLower() == "q")
                {
                    CancellationToken.Cancel();

                    SendThread.Join();
                    RecieveThread.Join();

                    CancellationToken.Dispose();

                    return;
                }

                Console.WriteLine(await SendData(command));
            }
        }

        private static async Task<string> SendData(string Request)
        {
            var RequestObject = new RequestObject()
            {
                Request = Request,
                Response = new TaskCompletionSource<string>()
            };

            SendQueue.Add(RequestObject);

            return await RequestObject.Response.Task;
        }

        private static void SendLoop(object cancellation)
        {
            var ct = (CancellationToken)cancellation;

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    // Do whatever else
                    return;
                }

                if (SendQueue.TryTake(out var request))
                {
                    // Actually send to the server...
                    ProcessingQueue.Add(request);
                }
            }
        }
        private static void RecieveLoop(object cancellation)
        {
            var ct = (CancellationToken)cancellation;

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    // Do whatever else
                    return;
                }

                if (ProcessingQueue.TryTake(out var request))
                {
                    request.Response.SetResult($"Response for: {request.Request}");
                }
            }
        }
    }

    class RequestObject
    {
        public string Request { get; set; }
        public TaskCompletionSource<string> Response { get; set; }
    }
}
