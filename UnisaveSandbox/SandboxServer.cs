using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using UnisaveSandbox.Execution;
using UnisaveSandbox.Http;

namespace UnisaveSandbox
{
    /// <summary>
    /// Represents the sandbox server
    /// (which is composed of many components, http server being one of them)
    /// </summary>
    public class SandboxServer : IDisposable
    {
        private readonly Config config;

        private readonly HealthStateManager healthStateManager;
        private readonly Initializer initializer;
        private readonly RequestQueue requestQueue;
        private readonly RequestConsumer requestConsumer;
        private readonly HttpClient httpClient;
        private readonly HttpServer httpServer;
        
        public SandboxServer(Config config)
        {
            this.config = config;
            
            healthStateManager = new HealthStateManager();
            httpClient = new HttpClient();
            initializer = new Initializer(httpClient);
            requestQueue = new RequestQueue(healthStateManager, config.MaxQueueLength);
            requestConsumer = new RequestConsumer(requestQueue, initializer);
            httpServer = new HttpServer(
                config.Port,
                new Router(healthStateManager, requestQueue)
            );
        }
        
        /// <summary>
        /// Start the server
        /// </summary>
        public void Start()
        {
            PrintStartupMessage();
            
            healthStateManager.Initialize();
            InitializeAsync().GetAwaiter().GetResult();
            requestConsumer.Initialize();
            httpServer.Start();
            
            Log.Info("Unisave Sandbox running.");
        }
        
        private void PrintStartupMessage()
        {
            string version = typeof(SandboxServer).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
            
            Console.WriteLine($"Starting Unisave Sandbox {version} ...");
            Console.WriteLine($"Listening on port {config.Port}");
            Console.WriteLine("Process ID: " + Process.GetCurrentProcess().Id);
        }

        /// <summary>
        /// Downloads the game assemblies
        /// </summary>
        private async Task InitializeAsync()
        {
            // dummy init
            if (config.DummyInitialization)
            {
                initializer.DummyInitialization();
                return;
            }

            // regular init
            if (config.InitializationRecipeUrl == null)
                Log.Info("Skipping startup sandbox initialization.");
            else
                await initializer.InitializeSandbox(config.InitializationRecipeUrl);
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop()
        {
            Log.Info("Stopping Unisave Sandbox...");
            
            httpServer?.Stop();
            requestConsumer?.Dispose();
            requestQueue?.Dispose();
            httpClient?.Dispose();
            healthStateManager?.Dispose();
            
            Log.Info("Bye.");
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}