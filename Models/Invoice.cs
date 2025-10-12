using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("Invoice")]
    public class Invoice
    {
        [Key]
        [Column("invoiceID")]
        [StringLength(15)]
        public string InvoiceID { get; set; } = string.Empty;

        [Required]
        [Column("invoiceDate")]
        [Display(Name = "Ngày xuất hóa đơn")]
        public DateTime InvoiceDate { get; set; }

        [Required]
        [Column("roomCharge", TypeName = "decimal(18, 2)")]
        [Display(Name = "Tiền phòng")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền phòng phải >= 0")]
        public decimal RoomCharge { get; set; }

        [Required]
        [Column("servicesCharge", TypeName = "decimal(18, 2)")]
        [Display(Name = "Tiền dịch vụ")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền dịch vụ phải >= 0")]
        public decimal ServicesCharge { get; set; }

        [Column("totalDue", TypeName = "decimal(18, 2)")]
        [Display(Name = "Tổng tiền")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? TotalDue { get; set; }

        [Column("netDue", TypeName = "decimal(18, 2)")]
        [Display(Name = "Tổng tiền sau thuế")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? NetDue { get; set; }

        [Required]
        [Column("reservationFormID")]
        [StringLength(15)]
        public string ReservationFormID { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("ReservationFormID")]
        public virtual ReservationForm? ReservationForm { get; set; }
    }
}
