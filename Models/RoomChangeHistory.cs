using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("RoomChangeHistory")]
    public class RoomChangeHistory
    {
        [Key]
        [Column("roomChangeHistoryID")]
        [StringLength(15)]
        public string RoomChangeHistoryID { get; set; } = string.Empty;

        [Required]
        [Column("dateChanged")]
        [Display(Name = "Ngày đổi phòng")]
        public DateTime DateChanged { get; set; }

        [Required]
        [Column("roomID")]
        [StringLength(15)]
        public string RoomID { get; set; } = string.Empty;

        [Required]
        [Column("reservationFormID")]
        [StringLength(15)]
        public string ReservationFormID { get; set; } = string.Empty;

        [Column("employeeID")]
        [StringLength(15)]
        public string? EmployeeID { get; set; }

        // Navigation properties
        [ForeignKey("RoomID")]
        public virtual Room? Room { get; set; }

        [ForeignKey("ReservationFormID")]
        public virtual ReservationForm? ReservationForm { get; set; }

        [ForeignKey("EmployeeID")]
        public virtual Employee? Employee { get; set; }
    }
}
