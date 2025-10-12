using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("Pricing")]
    public class Pricing
    {
        [Key]
        [Column("pricingID")]
        [StringLength(15)]
        public string PricingID { get; set; } = string.Empty;

        [Required]
        [Column("priceUnit")]
        [StringLength(15)]
        [Display(Name = "Đơn vị tính")]
        public string PriceUnit { get; set; } = "DAY";

        [Required]
        [Column("price", TypeName = "money")]
        [Display(Name = "Giá")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Required]
        [Column("roomCategoryID")]
        [StringLength(15)]
        public string RoomCategoryID { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("RoomCategoryID")]
        public virtual RoomCategory? RoomCategory { get; set; }
    }
}
