using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ConfirmationReceipt")]
    public class ConfirmationReceipt
    {
        [Key]
        [Column("receiptID")]
        [Display(Name = "Mã phiếu")]
        public string ReceiptID { get; set; } = null!;

        [Column("receiptType")]
        [Display(Name = "Loại phiếu")]
        public string ReceiptType { get; set; } = null!; // "RESERVATION" or "CHECKIN"

        [Column("issueDate")]
        [Display(Name = "Ngày phát hành")]
        public DateTime IssueDate { get; set; }

        [Column("reservationFormID")]
        [Display(Name = "Mã đặt phòng")]
        public string? ReservationFormID { get; set; }

        [Column("invoiceID")]
        [Display(Name = "Mã hóa đơn")]
        public string? InvoiceID { get; set; }

        [Column("customerName")]
        [Display(Name = "Tên khách hàng")]
        [StringLength(50)]
        public string CustomerName { get; set; } = null!;

        [Column("customerPhone")]
        [Display(Name = "Số điện thoại")]
        [StringLength(10)]
        public string CustomerPhone { get; set; } = null!;

        [Column("customerEmail")]
        [Display(Name = "Email")]
        [StringLength(50)]
        public string? CustomerEmail { get; set; }

        [Column("roomID")]
        [Display(Name = "Mã phòng")]
        [StringLength(15)]
        public string RoomID { get; set; } = null!;

        [Column("roomCategoryName")]
        [Display(Name = "Loại phòng")]
        [StringLength(50)]
        public string RoomCategoryName { get; set; } = null!;

        [Column("checkInDate")]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; }

        [Column("checkOutDate")]
        [Display(Name = "Ngày trả phòng")]
        public DateTime? CheckOutDate { get; set; }

        [Column("priceUnit")]
        [Display(Name = "Đơn vị giá")]
        [StringLength(15)]
        public string? PriceUnit { get; set; }

        [Column("unitPrice")]
        [Display(Name = "Đơn giá")]
        public decimal? UnitPrice { get; set; }

        [Column("deposit")]
        [Display(Name = "Tiền đặt cọc")]
        public decimal? Deposit { get; set; }

        [Column("totalAmount")]
        [Display(Name = "Tổng tiền")]
        public decimal? TotalAmount { get; set; }

        [Column("employeeName")]
        [Display(Name = "Nhân viên")]
        [StringLength(50)]
        public string? EmployeeName { get; set; }

        [Column("notes")]
        [Display(Name = "Ghi chú")]
        [StringLength(500)]
        public string? Notes { get; set; }

        [Column("qrCode")]
        [Display(Name = "QR Code")]
        [StringLength(200)]
        public string? QrCode { get; set; }

        // Navigation properties
        [ForeignKey("ReservationFormID")]
        public virtual ReservationForm? ReservationForm { get; set; }

        [ForeignKey("InvoiceID")]
        public virtual Invoice? Invoice { get; set; }
    }
}
