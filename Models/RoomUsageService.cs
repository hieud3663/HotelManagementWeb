using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("RoomUsageService")]
    public class RoomUsageService
    {
        [Key]
        [Column("roomUsageServiceId")]
        [StringLength(15)]
        public string RoomUsageServiceId { get; set; } = string.Empty;

        [Required]
        [Column("quantity")]
        [Display(Name = "Số lượng")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải >= 1")]
        public int Quantity { get; set; }

        [Required]
        [Column("unitPrice", TypeName = "decimal(18, 2)")]
        [Display(Name = "Đơn giá")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải >= 0")]
        public decimal UnitPrice { get; set; }

        [Column("totalPrice", TypeName = "decimal(18, 2)")]
        [Display(Name = "Thành tiền")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? TotalPrice { get; set; }

        [Required]
        [Column("dateAdded")]
        [Display(Name = "Ngày thêm")]
        public DateTime DateAdded { get; set; }

        [Required]
        [Column("hotelServiceId")]
        [StringLength(15)]
        public string HotelServiceId { get; set; } = string.Empty;

        [Required]
        [Column("reservationFormID")]
        [StringLength(15)]
        public string ReservationFormID { get; set; } = string.Empty;

        [Column("employeeID")]
        [StringLength(15)]
        public string? EmployeeID { get; set; }

        // Navigation properties
        [ForeignKey("HotelServiceId")]
        public virtual HotelService? HotelService { get; set; }

        [ForeignKey("ReservationFormID")]
        public virtual ReservationForm? ReservationForm { get; set; }

        [ForeignKey("EmployeeID")]
        public virtual Employee? Employee { get; set; }
    }
}
