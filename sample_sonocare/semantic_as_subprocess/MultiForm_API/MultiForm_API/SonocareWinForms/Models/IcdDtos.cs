using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SonocareWinForms.Models
{
    public class IcdPredictionRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }
    }

    public class IcdCodeResult
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class IcdPredictionResponse
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("icd_codes")]
        public List<IcdCodeResult> IcdCodes { get; set; } = new List<IcdCodeResult>();

        [JsonPropertyName("context")]
        public string? Context { get; set; }
    }
}
