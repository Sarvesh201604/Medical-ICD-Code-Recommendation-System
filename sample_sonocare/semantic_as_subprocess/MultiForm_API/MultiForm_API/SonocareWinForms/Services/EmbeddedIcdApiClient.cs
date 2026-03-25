using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SonocareWinForms.Models;

namespace SonocareWinForms.Services
{
    /// <summary>
    /// API Client using embedded Python (No HTTP, No Ports)
    /// Singleton pattern ensures single Python instance across app
    /// </summary>
    public class EmbeddedIcdApiClient
    {
        private static EmbeddedIcdApiClient _instance;
        private static readonly object _lockObject = new object();
        private static readonly System.Threading.SemaphoreSlim _requestSemaphore = new System.Threading.SemaphoreSlim(5, 5); // Max 5 concurrent requests
        private bool _initialized = false;

        public static EmbeddedIcdApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new EmbeddedIcdApiClient();
                        }
                    }
                }
                return _instance;
            }
        }

        private EmbeddedIcdApiClient() { }

        /// <summary>
        /// Initialize the embedded ICD recommender
        /// </summary>
        public bool Initialize()
        {
            if (_initialized)
                return true;

            bool success = EmbeddedIcdRecommender.Instance.Initialize();
            _initialized = success;
            return success;
        }

        /// <summary>
        /// Get ICD predictions - TRULY ASYNC call to embedded Python
        /// NO HTTP, NO PORTS - direct function call with proper async/await and timeout support
        /// </summary>
        public async Task<IcdPredictionResponse> PredictIcdAsync(
            IcdPredictionRequest request, 
            CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                return new IcdPredictionResponse
                {
                    Query = request?.Query ?? "",
                    IcdCodes = new List<Models.IcdCodeResult>(),
                    Context = "Error: ICD recommender not initialized"
                };
            }

            // Acquire semaphore to limit concurrent requests
            await _requestSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            
            try
            {
                Console.WriteLine($"[API] Starting recommendation search for: {request.Query.Substring(0, Math.Min(50, request.Query.Length))}...");
                
                // Call Python ASYNCHRONOUSLY with cancellation support
                var codes = await EmbeddedIcdRecommender.Instance.GetRecommendationsAsync(
                    request.Query,
                    request.Category ?? "both",
                    cancellationToken
                ).ConfigureAwait(false);

                Console.WriteLine($"[API] Got {codes.Count} codes from Python");

                // Convert to Models.IcdCodeResult
                var icdCodes = new List<Models.IcdCodeResult>();
                foreach (var code in codes)
                {
                    icdCodes.Add(new Models.IcdCodeResult
                    {
                        Code = code.Code,
                        Description = code.Description
                    });
                }

                return new IcdPredictionResponse
                {
                    Query = request.Query,
                    IcdCodes = icdCodes,
                    Context = $"Found {icdCodes.Count} recommendations"
                };
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[API] Request canceled by user or timeout");
                return new IcdPredictionResponse
                {
                    Query = request?.Query ?? "",
                    IcdCodes = new List<Models.IcdCodeResult>(),
                    Context = "Request was canceled or timed out"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] ERROR: {ex.GetType().Name}: {ex.Message}");
                return new IcdPredictionResponse
                {
                    Query = request?.Query ?? "",
                    IcdCodes = new List<Models.IcdCodeResult>(),
                    Context = $"Error: {ex.Message}"
                };
            }
            finally
            {
                // ALWAYS release semaphore
                _requestSemaphore.Release();
            }
        }

        public bool IsInitialized => _initialized;
    }
}
