using System.ComponentModel.DataAnnotations;

namespace ProvaPub.Models
{
    public class RandomNumber
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int Number { get; set; }
        [Required]
        // Guardar o horário que foi gerado
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
