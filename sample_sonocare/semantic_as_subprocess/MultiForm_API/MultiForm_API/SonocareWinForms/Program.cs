using System;
using System.Windows.Forms;
using SonocareWinForms.Services;
using System.Diagnostics;
using System.IO;

namespace SonocareWinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize SQLitePCL
            SQLitePCL.Batteries.Init();

            // Initialize embedded ICD recommender (Python + LangChain)
            Console.WriteLine("\n========== SONOCARE ICD INITIALIZATION ==========");
            Console.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"Exe Location: {System.Reflection.Assembly.GetExecutingAssembly().Location}");
            
            try
            {
                Console.WriteLine("\n[MAIN] Initializing embedded ICD recommender...");
                var icdClient = EmbeddedIcdApiClient.Instance;
                Console.WriteLine("[MAIN] ✓ ICD API Client instance created");
                
                bool initSuccess = icdClient.Initialize();
                Console.WriteLine($"[MAIN] ✓ ICD API Client initialization: {(initSuccess ? "SUCCESS" : "FAILED")}");
                
                if (!initSuccess)
                {
                    Console.WriteLine("\n[MAIN] ⚠️  WARNING: ICD initialization failed!");
                    Console.WriteLine("[MAIN] Make sure:");
                    Console.WriteLine("[MAIN]   ✓ Python 3.11+ is installed and in PATH");
                    Console.WriteLine("[MAIN]   ✓ All packages installed: pip install -r requirements.txt");
                    Console.WriteLine("[MAIN]   ✓ Files are in the correct location (Groq_ICD_new/Groq_ICD)");
                    
                    MessageBox.Show(
                        "Warning: ICD recommendation system failed to initialize.\n\n" +
                        "The app will continue but ICD recommendations will not work.\n\n" +
                        "Make sure:\n" +
                        "1. Python 3.11+ is installed\n" +
                        "2. All requirements are installed: pip install -r requirements.txt\n" +
                        "3. Files are in the correct location (Groq_ICD_new/Groq_ICD)",
                        "ICD Initialization Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    Console.WriteLine("[MAIN] ✓✓✓ ICD Recommender ready to use! ✓✓✓");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[MAIN] ✗ EXCEPTION during initialization: {ex.GetType().Name}");
                Console.WriteLine($"[MAIN] Message: {ex.Message}");
                Console.WriteLine($"[MAIN] Stack: {ex.StackTrace}");
                
                MessageBox.Show(
                    $"Error initializing ICD recommender:\n{ex.GetType().Name}\n{ex.Message}\n\n" +
                    "Check console output for details.",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            Console.WriteLine("\nStarting Sonocare application...\n");
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
