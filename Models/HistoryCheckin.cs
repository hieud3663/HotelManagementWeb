using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("HistoryCheckin")]
    public class HistoryCheckin
    {
        [Key]
        [Column("historyCheckInID")]
        [StringLength(15)]
        public string HistoryCheckInID { get; set; } = string.Empty;

        [Required]
        [Column("checkInDate")]
        [Display(Name = "Ngày giờ check-in")]
        public DateTime CheckInDate { get; set; }

        [Required]
        [Column("reservationFormID")]
        [StringLength(15)]
        public string ReservationFormID { get; set; } = string.Empty;

        [Column("employeeID")]
        [StringLength(15)]
        public string? EmployeeID { get; set; }

        // Navigation properties
        [ForeignKey("ReservationFormID")]
        public virtual ReservationForm? ReservationForm { get; set; }

        [ForeignKey("EmployeeID")]
        public virtual Employee? Employee { get; set; }
    }
}
