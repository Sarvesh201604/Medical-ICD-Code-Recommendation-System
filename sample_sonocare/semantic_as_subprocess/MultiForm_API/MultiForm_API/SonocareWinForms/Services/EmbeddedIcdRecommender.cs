using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SonocareWinForms.Services
{
    /// <summary>
    /// Embedded ICD Recommender using subprocess + FAISS (No HTTP, No Ports, No Python.NET DLL!)
    /// Calls Python functions via command-line subprocess from the deepseek venv
    /// Uses FAISS for semantic similarity search instead of ChromaDB
    /// </summary>
    public class EmbeddedIcdRecommender
    {
        private static EmbeddedIcdRecommender _instance;
        private static readonly object _lockObject = new object();
        private static readonly System.Threading.SemaphoreSlim _pythonCallSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        private bool _isInitialized = false;
        private string _pythonExePath;
        private readonly string GROQ_ICD_PATH = GetGroqIcdPath();
        private readonly string DEEPSEEK_VENV_PATH = GetDeepseekVenvPath();
        private readonly string LOG_FILE = Path.Combine(Path.GetTempPath(), "sonocare_icd_init.log");

        private static string GetDeepseekVenvPath()
        {
            // Navigate from executable location to Fine_tune folder, then to trial/trial/deepseek
            string exePath = AppDomain.CurrentDomain.BaseDirectory; // bin\Debug\net48\
            string fineTuneDir = GetGroqIcdPath();
            fineTuneDir = Path.GetDirectoryName(fineTuneDir); // Go from Groq_ICD_new to Fine_tune
            fineTuneDir = Path.GetDirectoryName(fineTuneDir);
            string venvPath = Path.Combine(fineTuneDir, "trial", "trial", "deepseek");
            return Path.GetFullPath(venvPath);
        }

        private static string GetGroqIcdPath()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory; // bin\Debug\net48\
            // Navigate up: net48 -> Debug -> bin -> SonocareWinForms -> MultiForm_API -> MultiForm_API -> Groq_ICD
            string groqPath = Path.Combine(exePath, "..", "..", "..", "..", "..", "..");
            groqPath = Path.GetFullPath(groqPath);
            // groqPath should now be at Groq_ICD directory
            return groqPath;
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(LOG_FILE, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
                Console.WriteLine(message);
            }
            catch { }
        }

        public static EmbeddedIcdRecommender Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new EmbeddedIcdRecommender();
                        }
                    }
                }
                return _instance;
            }
        }

        private EmbeddedIcdRecommender()
        {
        }

        public bool Initialize()
        {
            if (_isInitialized)
            {
                Log("[EMB] Already initialized");
                return true;
            }

            try
            {
                Log("\n========== EMBEDDED ICD RECOMMENDER INIT ==========");
                Log($"[EMB] Using subprocess mode (no Python.NET DLL required)");
                Log($"[EMB] Target Groq_ICD directory: {GROQ_ICD_PATH}");
                Log($"[EMB] Deepseek venv: {DEEPSEEK_VENV_PATH}");

                // Find python.exe in the deepseek venv
                string pythonExe = Path.Combine(DEEPSEEK_VENV_PATH, "Scripts", "python.exe");
                if (!File.Exists(pythonExe))
                {
                    Log($"[EMB] ERROR: Python not found at: {pythonExe}");
                    return false;
                }
                _pythonExePath = pythonExe;
                Log($"[EMB] OK Found python.exe");

                // Verify required files
                Log($"[EMB] Checking files...");
                
                string icdRecommenderFile = Path.Combine(GROQ_ICD_PATH, "icd_recommender_service.py");
                string faissIndex = Path.Combine(GROQ_ICD_PATH, "icd_search.index");
                string faissMetadata = Path.Combine(GROQ_ICD_PATH, "icd_metadata.pkl");

                if (!File.Exists(icdRecommenderFile))
                {
                    Log($"[EMB] ERROR Missing: icd_recommender_service.py");
                    return false;
                }
                Log("[EMB] OK icd_recommender_service.py found");

                if (!File.Exists(faissIndex))
                {
                    Log($"[EMB] ERROR Missing: icd_search.index");
                    return false;
                }
                Log("[EMB] OK FAISS index found");

                if (!File.Exists(faissMetadata))
                {
                    Log($"[EMB] ERROR Missing: icd_metadata.pkl");
                    return false;
                }
                Log("[EMB] OK FAISS metadata found");

                // Test Python import via subprocess
                Log("[EMB] Testing Python import...");
                string testScript = $@"
import sys
sys.path.insert(0, r'{GROQ_ICD_PATH}')
import icd_recommender_service
icd_recommender_service.initialize()
print('OK')
";
                
                string result = RunPythonScriptWithStdin(testScript, "", timeout: 120000);
                if (!result.Contains("OK") && !result.Contains("initialized"))
                {
                    Log($"[EMB] ERROR Python test failed: {result.Substring(0, Math.Min(200, result.Length))}");
                    return false;
                }
                Log("[EMB] OK Python import successful");

                _isInitialized = true;
                Log("[EMB] ✅ SUCCESS! ICD Recommender Ready!");
                Log("========== INITIALIZATION COMPLETE ==========\n");
                return true;
            }
            catch (Exception ex)
            {
                Log($"\n[EMB] ❌ INITIALIZATION FAILED");
                Log($"[EMB] Exception: {ex.Message}");
                return false;
            }
        }

        public async Task<List<IcdCode>> GetRecommendationsAsync(
            string query,
            string category = "both",
            System.Threading.CancellationToken cancellationToken = default)
        {
            var result = new List<IcdCode>();

            if (!_isInitialized)
            {
                Log("[EMB] ERROR Not initialized");
                return result;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                Log("[EMB] ERROR Query is empty");
                return result;
            }

            await _pythonCallSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            
            try
            {
                // Extract impression from JSON if needed
                string processedQuery = query;
                if (query.StartsWith("{") && query.Contains("Impression"))
                {
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(query))
                        {
                            if (doc.RootElement.TryGetProperty("Impression", out var impProp))
                            {
                                processedQuery = impProp.GetString() ?? query;
                            }
                        }
                    }
                    catch { }
                }

                Log($"[EMB] Query: {processedQuery.Substring(0, Math.Min(50, processedQuery.Length))}...");

                // Call Python via subprocess using stdin (much safer than -c with escaping)
                string pythonCode = $@"
import sys
import json
sys.path.insert(0, r'{GROQ_ICD_PATH}')
import icd_recommender_service

# Read query from stdin
query = sys.stdin.read().strip()
try:
    result = icd_recommender_service.get_icd_codes(query, 5)
    print(result)
except Exception as e:
    print(json.dumps({{'error': str(e)}}))
";

                string jsonResult = RunPythonScriptWithStdin(pythonCode, processedQuery, timeout: 60000);
                Log($"[EMB] Received {jsonResult.Length} chars from Python");

                // Parse JSON response
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(jsonResult))
                    {
                        var root = doc.RootElement;
                        
                        if (root.TryGetProperty("error", out _))
                        {
                            Log($"[EMB] ERROR: Python error");
                            return result;
                        }

                        if (root.TryGetProperty("codes", out var codesElement) && codesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var codeElement in codesElement.EnumerateArray())
                            {
                                try
                                {
                                    var code = new IcdCode
                                    {
                                        Code = codeElement.GetProperty("code").GetString() ?? "",
                                        Description = codeElement.GetProperty("description").GetString() ?? ""
                                    };
                                    result.Add(code);
                                }
                                catch (Exception parseEx)
                                {
                                    Log($"[EMB] Failed to parse code: {parseEx.Message}");
                                }
                            }
                        }

                        Log($"[EMB] ✅ Retrieved {result.Count} recommendations");
                    }
                }
                catch (JsonException jsonEx)
                {
                    Log($"[EMB] JSON parse error: {jsonEx.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log($"[EMB] ERROR: {ex.Message}");
                return result;
            }
            finally
            {
                _pythonCallSemaphore.Release();
            }
        }

        private string RunPythonScriptWithStdin(string pythonCode, string stdin, int timeout = 30000)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = _pythonExePath;
                    process.StartInfo.Arguments = "-c \"" + pythonCode.Replace("\"", "\\\"") + "\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                    process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

                    process.Start();

                    // Write query to stdin
                    process.StandardInput.WriteLine(stdin);
                    process.StandardInput.Close();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!process.WaitForExit(timeout))
                    {
                        process.Kill();
                        Log($"[EMB] ERROR: Python timeout after {timeout}ms");
                        return "ERROR: Timeout";
                    }

                    if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                    {
                        Log($"[EMB] Python stderr (sample): {error.Substring(0, Math.Min(100, error.Length))}");
                    }

                    // Filter output - only keep lines that look like JSON
                    // Python may print warnings/logging before and after the JSON
                    output = output.Trim();
                    
                    // Look for the start of JSON response: look for `{"success"` or just first `{`
                    int firstBrace = output.IndexOf("{\"success\"");
                    if (firstBrace < 0)
                    {
                        firstBrace = output.IndexOf('{');
                    }
                    
                    if (firstBrace >= 0)
                    {
                        output = output.Substring(firstBrace);
                        
                        // Now find the end of the JSON - find where the root object closes
                        // Count braces to find where depth returns to 0
                        int depth = 0;
                        int endPos = -1;
                        for (int i = 0; i < output.Length; i++)
                        {
                            if (output[i] == '{') depth++;
                            else if (output[i] == '}')
                            {
                                depth--;
                                if (depth == 0)
                                {
                                    endPos = i + 1;  // Include the closing }
                                    break;
                                }
                            }
                        }
                        
                        if (endPos > 0)
                        {
                            output = output.Substring(0, endPos);
                            Log($"[EMB] EXTRACTED JSON ({output.Length} chars): {output.Substring(0, Math.Min(80, output.Length))}...");
                        }
                        else
                        {
                            Log($"[EMB] WARNING: Could not find end of JSON object");
                        }
                    }
                    else
                    {
                        Log($"[EMB] ERROR: No JSON response found in output");
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                Log($"[EMB] Failed to run Python: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        public bool IsInitialized => _isInitialized;
    }

    /// <summary>
    /// Simple ICD Code model
    /// </summary>
    public class IcdCode
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }
}
