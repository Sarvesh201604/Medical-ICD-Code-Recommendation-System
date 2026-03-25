using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace SonocareWinForms.Services
{
    public class AugnitoService : IDisposable
    {
        // Configuration
        private const string ServerDomain = "apis.augnito.ai";
        private const string AccountCode = "ab0535d9-94cf-4cdb-b815-5c9d606761ca";
        private const string AccessKey = "79579c8cd5c947869a606cc4e9965175";
        private const string LmId = "111801201";
        private const string UserTag = "demo_user";
        private const string SourceApp = "SonocareWinForms";

        // State
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private WaveInEvent _waveIn;
        private bool _isListening;
        public bool IsListening => _isListening;
        private bool _isRecordingSessionActive;
        private WaveFileWriter _waveWriter;
        // private string _accessToken; // Removed as we fetch fresh token each time or cache appropriately

        
        // Events
        public event Action<string> OnLog;
        public event Action<bool> OnListeningStateChanged;
        public event Action<string> OnTranscriptReceived;
        public event Action<string> OnError;

        public AugnitoService()
        {
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async void ToggleListening()
        {
            if (_isListening)
            {
                await StopListeningAsync();
            }
            else
            {
                await StartListeningAsync();
            }
        }

        public async Task StartListeningAsync()
        {
            if (_isListening) return;

            try
            {
                var token = await GetAccessTokenAsync();
                Log($"Got Access Token: {token.Substring(0, 10)}...");

                _webSocket = new ClientWebSocket();
                
                // Use v2 endpoint as per original implementation
                var uriBuilder = new UriBuilder($"wss://{ServerDomain}/v2/speechapi");
                var query = $"content-type=audio/x-raw,layout=interleaved,rate=16000,format=S16LE,channels=1" +
                            $"&accountcode={AccountCode}" +
                            $"&accesskey={token}" +
                            $"&lmid={LmId}" +
                            $"&usertag={UserTag}" +
                            $"&sourceapp={SourceApp}";
                uriBuilder.Query = query;
                
                Log($"Connecting to WebSocket: {uriBuilder.Uri}...");
                await _webSocket.ConnectAsync(uriBuilder.Uri, CancellationToken.None);
                Log("WebSocket Connected.");

                _cancellationTokenSource = new CancellationTokenSource();
                _ = ReceiveLoopAsync(_cancellationTokenSource.Token);

                StartRecording();
                _isListening = true;
                OnListeningStateChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                Log($"Error in StartListening: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task StopListeningAsync()
        {
            if (!_isListening) return;

            Log("Stopping Listening...");
            
            _isListening = false; // Mark as stopped BEFORE checking if we should stop hardware
            OnListeningStateChanged?.Invoke(false);

            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stop", CancellationToken.None);
            }
            
            _cancellationTokenSource?.Cancel();

            _cancellationTokenSource?.Cancel();

            // ALWAYS stop hardware when Mic stops, even if recording session is active (Pause behavior)
            StopRecording();

            Log("Stopped Listening.");
        }

        public void StartLocalRecording(string filePath)
        {
            if (_isRecordingSessionActive) return;

            try
            {
                _waveWriter = new WaveFileWriter(filePath, new WaveFormat(16000, 16, 1));
                _isRecordingSessionActive = true;
                
                // Do NOT force StartRecording. 
                // Recording only happens when the user actually turns on the Mic (Augnito).
                Log($"Local Recording Session Active: {filePath}");
            }
            catch (Exception ex)
            {
                Log($"Error starting local recording: {ex.Message}");
            }
        }

        public void StopLocalRecording()
        {
            if (!_isRecordingSessionActive) return;

            _isRecordingSessionActive = false;
            
            if (_waveWriter != null)
            {
                _waveWriter.Dispose();
                _waveWriter = null;
            }
            Log("Local Recording Session Stopped.");
            // Do not affect hardware state
        }

        private void StartRecording()
        {
            if (_waveIn == null)
            {
                _waveIn = new WaveInEvent();
                _waveIn.DeviceNumber = 0; // Default Mic
                _waveIn.WaveFormat = new WaveFormat(16000, 16, 1); // Explicit Mono
                _waveIn.BufferMilliseconds = 100; // 100ms buffers
                _waveIn.DataAvailable += OnAudioDataAvailable;
                _waveIn.StartRecording();
                Log("Audio Recording Started.");
            }
        }

        private void StopRecording()
        {
            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= OnAudioDataAvailable;
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;
                Log("Audio Recording Stopped.");
            }
        }

        private async void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            // 1. Write to Local File if Session Active AND Hardware Running
            if (_isRecordingSessionActive && _waveWriter != null)
            {
                try
                {
                    _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                    if (_waveWriter.Position > _waveWriter.Length) _waveWriter.Flush(); // periodic flush?
                }
                catch (Exception ex)
                {
                    Log($"Error writing to file: {ex.Message}");
                }
            }

            // 2. Stream to WebSocket if Listening
            if (_isListening && _webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                // Prevent concurrent sends (Fix for "There is already one outstanding operation")
                await _sendLock.WaitAsync();
                try
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        if (e.BytesRecorded > 0)
                        {
                             await _webSocket.SendAsync(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded), WebSocketMessageType.Binary, true, CancellationToken.None);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error sending audio: {ex.Message}");
                }
                finally
                {
                    _sendLock.Release();
                }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Log("WebSocket Receiver Closed.");
                        break;
                    }
                    
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleMessage(json);
                }
            }
            catch (Exception ex)
            {
                 if (!token.IsCancellationRequested)
                    Log($"ReceiveLoop Error: {ex.Message}");
            }
        }

        private void HandleMessage(string json)
        {
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("Result", out var result))
                    {
                        if (result.TryGetProperty("Transcript", out var transProp))
                        {
                             string trans = transProp.GetString();
                             bool isFinal = false;
                             if (result.TryGetProperty("Final", out var final)) isFinal = final.GetBoolean();
                             
                             if (!string.IsNullOrWhiteSpace(trans))
                             {
                                 if (isFinal)
                                 {
                                     Log($"FINAL: {trans}");
                                     OnTranscriptReceived?.Invoke(trans);
                                 }
                             }
                        }
                    }
                    
                    if (root.TryGetProperty("Error", out var error))
                    {
                        Log($"Server Error: {error.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error parsing JSON: {ex.Message}.");
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                var requestBody = new
                {
                    AccountCode = AccountCode,
                    AccessKey = AccessKey,
                    UserTag = UserTag,
                    ExpiryInHours = 1
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"https://{ServerDomain}/db/speechapi/createtoken", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(responseString))
                {
                    return doc.RootElement.GetProperty("Data").GetProperty("AccessToken").GetString();
                }
            }
        }

        private void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[Augnito] {message}");
            OnLog?.Invoke(message);
        }

        public void Dispose()
        {
            StopListeningAsync().Wait();
        }
    }
}
