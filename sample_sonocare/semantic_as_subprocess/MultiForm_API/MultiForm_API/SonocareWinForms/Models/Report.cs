using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SonocareWinForms.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }
        
        public int PatientId { get; set; }
        
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        public string VisitDate { get; set; } = string.Empty;
        
        public string? BPD { get; set; }
        public string? HC { get; set; }
        public string? AC { get; set; }
        public string? FL { get; set; }
        public string? FHR { get; set; }
        public string? GA { get; set; }
        public string? Placenta { get; set; }
        public string? AFI { get; set; }
        public string? Presentation { get; set; }
        public string? EFW { get; set; }
        public string? Comments { get; set; }
        
        // New Fields
        public string? BiometryHistory { get; set; }
        public string? ScanType { get; set; }
        public string? Gender { get; set; }
    }
}
