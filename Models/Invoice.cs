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

        [Required]
        [Column("taxRate")]
        [Display(Name = "Thuế suất (%)")]
        [Range(0, 100, ErrorMessage = "Thuế suất phải từ 0 đến 100")]
        public decimal TaxRate { get; set; }

        [Column("roomBookingDeposit", TypeName = "decimal(18, 2)")]
        [Display(Name = "Tiền đặt cọc")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền đặt cọc phải >= 0")]
        public decimal RoomBookingDeposit { get; set; }

        [Column("netDue", TypeName = "decimal(18, 2)")]
        [Display(Name = "Tổng tiền sau thuế")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? NetDue { get; set; }

        [Column("totalAmount", TypeName = "decimal(18, 2)")]
        [Display(Name = "Tổng tiền")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? TotalAmount { get; set; }

        [Required]
        [Column("isPaid")]
        [Display(Name = "Đã thanh toán")]
        public bool IsPaid { get; set; } = false;

        [Column("paymentDate")]
        [Display(Name = "Ngày thanh toán")]
        public DateTime? PaymentDate { get; set; }

        [Column("paymentMethod")]
        [StringLength(20)]
        [Display(Name = "Phương thức thanh toán")]
        public string? PaymentMethod { get; set; }

        [Column("checkoutType")]
        [StringLength(20)]
        [Display(Name = "Loại checkout")]
        public string? CheckoutType { get; set; }

        [Required]
        [Column("reservationFormID")]
        [StringLength(15)]
        public string ReservationFormID { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("ReservationFormID")]
        public virtual ReservationForm? ReservationForm { get; set; }

        // Helper properties for views
        [NotMapped]
        public decimal TaxAmount => TotalDue.HasValue ? TotalDue.Value * (TaxRate / 100) : 0;

        [NotMapped]
        public decimal Deposit => RoomBookingDeposit;        
        
        
        [Column("amountPaid", TypeName = "decimal(18,2)")]
        [Display(Name = "Số tiền khách đã trả")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn hoặc bằng 0")]
        public decimal AmountPaid { get; set; }
    }
}
