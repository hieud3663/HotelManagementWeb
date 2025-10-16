using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("HistoryCheckOut")]
    public class HistoryCheckOut
    {
        [Key]
        [Column("historyCheckOutID")]
        [StringLength(15)]
        public string HistoryCheckOutID { get; set; } = string.Empty;

        [Required]
        [Column("checkOutDate")]
        [Display(Name = "Ngày giờ check-out")]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Column("reservationFormID")]
        [StringLength(15)]
        public string ReservationFormID { get; set; } = string.Empty;

        [Column("employeeID")]
        [StringLength(15)]
        public string? EmployeeID { get; set; }

        [Column("invoiceID")]
        [StringLength(15)]
        public string? InvoiceID { get; set; }

        // Navigation properties
        [ForeignKey("ReservationFormID")]
        public virtual ReservationForm? ReservationForm { get; set; }

        [ForeignKey("EmployeeID")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("InvoiceID")]
        public virtual Invoice? Invoice { get; set; }
    }
}
