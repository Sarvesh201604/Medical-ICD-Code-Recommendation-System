# Integration Guide: ICD Recommender in Sonocare WinForms

This guide shows how to integrate the new FAISS-based ICD recommender into your Sonocare WinForms application.

## Prerequisites

1. **Python installed** on the user's machine (3.8+)
2. **ICD data JSON file** with your medical database
3. **PythonNET** for C# ↔ Python interop

## Setup for Each User

### Step 1: Install Python Dependencies

Create a batch file `setup_icd.bat` in the Sonocare installation folder:

```batch
@echo off
echo Installing ICD Recommender Dependencies...
pip install -r requirements.txt
echo Building FAISS Index from your ICD data...
python prepare_data.py ICD_data.json
python build_index.py
echo Done! Ready to use.
pause
```

Users run this once to setup their environment.

### Step 2: Add to Your C# Project

**Installation via NuGet:**
```
Install-Package pythonnet
```

**Add references:**
```csharp
using Python.Runtime;
```

### Step 3: Initialize at Application Startup

Add this to your `Form1.cs` or `MainForm.cs`:

```csharp
public partial class MainForm : Form
{
    private dynamic _icdRecommender;
    
    public MainForm()
    {
        InitializeComponent();
        InitializeIcdRecommender();
    }
    
    private void InitializeIcdRecommender()
    {
        try
        {
            // Initialize PythonNET
            if (!PythonEngine.IsInitialized)
            {
                PythonEngine.Initialize();
            }
            
            // Load the recommender module
            dynamic sys = Py.Module("sys");
            sys.path.append(".");  // Add current directory to path
            
            _icdRecommender = Py.Module("icd_recommender_service");
            
            // Initialize the recommender
            bool initialized = _icdRecommender.initialize();
            
            if (initialized)
            {
                MessageBox.Show("ICD Recommender initialized successfully!", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to initialize ICD Recommender. " +
                    "Please ensure icd_search.index and icd_metadata.pkl exist.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing ICD Recommender:\n{ex.Message}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
```

### Step 4: Get ICD Recommendations

When users need ICD recommendations (e.g., in `FinalImpressionControl.cs`):

```csharp
private void GetIcdRecommendations(string impression)
{
    try
    {
        if (_icdRecommender == null)
        {
            MessageBox.Show("ICD Recommender not initialized");
            return;
        }
        
        // Call the recommender
        string resultJson = _icdRecommender.get_icd_codes(impression, num_recommendations: 5);
        
        // Parse the JSON response
        var result = JObject.Parse(resultJson);
        
        if ((bool)result["success"])
        {
            var codes = result["codes"];
            
            // Display results (example)
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Recommended ICD Codes:");
            
            foreach (var code in codes)
            {
                string icdCode = (string)code["code"];
                double score = (double)code["similarity_score"];
                
                sb.AppendLine($"• {icdCode} (Similarity: {score:F4})");
            }
            
            // Show in a dialog, listbox, or whatever suits your UI
            MessageBox.Show(sb.ToString(), "ICD Recommendations");
        }
        else
        {
            string error = (string)result["error"];
            MessageBox.Show($"Error: {error}", "Recommendation Failed");
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error getting recommendations:\n{ex.Message}", "Error");
    }
}
```

### Step 5: Integration in Final Impression Control

Example integration in `FinalImpressionControl.cs`:

```csharp
public partial class FinalImpressionControl : UserControl
{
    private dynamic _icdRecommender;
    
    // ... existing code ...
    
    // Wire up the recommender
    public void SetIcdRecommender(dynamic recommender)
    {
        _icdRecommender = recommender;
    }
    
    // Button click to get recommendations
    private void btnGetRecommendations_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(tbImpression.Text))
        {
            MessageBox.Show("Please enter an impression first");
            return;
        }
        
        try
        {
            string resultJson = _icdRecommender.get_icd_codes(
                tbImpression.Text, 
                num_recommendations: 5
            );
            
            var result = JObject.Parse(resultJson);
            
            // Clear existing items
            lvRecommendations.Items.Clear();
            
            if ((bool)result["success"])
            {
                foreach (var code in result["codes"])
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = (string)code["code"];
                    item.SubItems.Add(((double)code["similarity_score"]).ToString("F4"));
                    item.SubItems.Add((string)code["impression"] ?? "");
                    
                    lvRecommendations.Items.Add(item);
                }
                
                lbStatus.Text = $"Found {result["count"]} recommendations";
                lbStatus.ForeColor = Color.Green;
            }
            else
            {
                lbStatus.Text = "Error: " + result["error"];
                lbStatus.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            lbStatus.Text = $"Error: {ex.Message}";
            lbStatus.ForeColor = Color.Red;
        }
    }
}
```

### Step 6: Pass Recommender to Child Forms

In your main form:

```csharp
public partial class MainForm : Form
{
    private dynamic _icdRecommender;
    
    private void OpenFinalImpressionForm()
    {
        FinalImpressionControl finalControl = new FinalImpressionControl();
        
        // Pass the recommender instance
        finalControl.SetIcdRecommender(_icdRecommender);
        
        // Add to form or open dialog
    }
}
```

## Error Handling

Key error codes and solutions:

| Error | Cause | Solution |
|-------|-------|----------|
| "icd_search.index not found" | FAISS index not built | Run `python build_index.py` |
| "Module not found" | Python path not set | Ensure Python files are in app directory |
| "Recommender not initialized" | `initialize()` not called | Call `initialize()` at startup |
| "Out of memory" | Dataset too large | Use fewer `num_recommendations` |

## Testing

Test your integration with this simple C# console app:

```csharp
class Program
{
    static void Main()
    {
        try
        {
            PythonEngine.Initialize();
            dynamic recommender = Py.Module("icd_recommender_service");
            
            recommender.initialize();
            
            string result = recommender.get_icd_codes(
                "normal fetal development with adequate amniotic fluid"
            );
            
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
        }
    }
}
```

## Deployment Checklist

For each Sonocare user deployment:

- [ ] Python 3.8+ installed
- [ ] `icd_recommender_service.py` in app directory
- [ ] `requirements.txt` in app directory
- [ ] User's ICD JSON data in app directory
- [ ] `prepare_data.py` run with user's ICD data
- [ ] `build_index.py` run successfully
- [ ] `icd_search.index` file exists
- [ ] `icd_metadata.pkl` file exists
- [ ] PythonNET installed in C# project
- [ ] `initialize()` called in Sonocare startup
- [ ] Tests pass with sample impressions

## Performance Tips

1. **Cache the recommender**: Initialize once, reuse the same instance
2. **Reduce recommendations**: Use `num_recommendations: 3` for faster results
3. **Async calls**: Consider wrapping in async/await for UI responsiveness

```csharp
// Async example
private async void GetRecommendationsAsync(string impression)
{
    string result = await Task.Run(() => 
        _icdRecommender.get_icd_codes(impression, 5)
    );
    // Process result on UI thread
}
```

## Support

For issues:
1. Check `FAISS_IMPLEMENTATION.md` for detailed documentation
2. Review `example_usage.py` for Python usage patterns
3. Enable debug logging in `icd_recommender_service.py`

---

Happy integrating! 🚀
