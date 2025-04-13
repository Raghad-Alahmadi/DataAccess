using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Product { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        // Navigation property
        public virtual User User { get; set; }
    }
}
