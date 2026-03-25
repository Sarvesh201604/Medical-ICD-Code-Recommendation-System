using System.ComponentModel.DataAnnotations;

namespace SonocareWinForms.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string IdNumber { get; set; } = string.Empty;
    }
}
