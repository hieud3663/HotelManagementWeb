using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("RoomCategory")]
    public class RoomCategory
    {
        [Key]
        [Column("roomCategoryID")]
        [StringLength(15)]
        public string RoomCategoryID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên loại phòng không được để trống")]
        [Column("roomCategoryName")]
        [StringLength(50)]
        [Display(Name = "Tên loại phòng")]
        public string RoomCategoryName { get; set; } = string.Empty;

        [Required]
        [Column("numberOfBed")]
        [Display(Name = "Số giường")]
        [Range(1, 10, ErrorMessage = "Số giường phải từ 1 đến 10")]
        public int NumberOfBed { get; set; }

        [Column("isActivate")]
        [StringLength(10)]
        public string IsActivate { get; set; } = "ACTIVATE";

        // Navigation properties
        public virtual ICollection<Room>? Rooms { get; set; }
        public virtual ICollection<Pricing>? Pricings { get; set; }
    }
}
