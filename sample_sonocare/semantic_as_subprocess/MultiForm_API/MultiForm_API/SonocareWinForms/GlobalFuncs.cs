using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SonocareWinForms // Ensure namespace matches project
{
    public static class GlobalFuncs
    {
        public static Dictionary<string, Dictionary<string, List<string>>> allforms = new Dictionary<string, Dictionary<string, List<string>>>();

        public static void InitializeVoiceCommands()
        {
            try
            {
                // Try multiple locations for VoiceFieldMapper.json
                string[] searchPaths = new[]
                {
                    "VoiceFieldMapper.json", // Current directory (when running from bin/Debug)
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VoiceFieldMapper.json"), // App directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "VoiceFieldMapper.json") // Relative to source
                };

                string voiceMapperPath = null;
                foreach (var path in searchPaths)
                {
                    string fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        voiceMapperPath = fullPath;
                        break;
                    }
                }

                if (voiceMapperPath != null)
                {
                    string json = File.ReadAllText(voiceMapperPath);
                    var allForms1 = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(json);
                    if (allForms1 != null)
                    {
                        allforms = allForms1;
                    }
                    Console.WriteLine($"[GlobalFuncs] VoiceFieldMapper.json loaded from: {voiceMapperPath}");
                }
                else
                {
                    Console.WriteLine("[GlobalFuncs] VoiceFieldMapper.json not found in any search path.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GlobalFuncs] Error loading VoiceFieldMapper.json: {ex.Message}");
            }
        }
    }
}
