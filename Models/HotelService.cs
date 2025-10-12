using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("HotelService")]
    public class HotelService
    {
        [Key]
        [Column("hotelServiceId")]
        [StringLength(15)]
        public string HotelServiceId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
        [Column("serviceName")]
        [StringLength(50)]
        [Display(Name = "Tên dịch vụ")]
        public string ServiceName { get; set; } = string.Empty;

        [Required]
        [Column("description")]
        [StringLength(255)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column("servicePrice", TypeName = "money")]
        [Display(Name = "Giá dịch vụ")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá dịch vụ phải lớn hơn hoặc bằng 0")]
        public decimal ServicePrice { get; set; }

        [Column("serviceCategoryID")]
        [StringLength(15)]
        public string? ServiceCategoryID { get; set; }

        [Column("isActivate")]
        [StringLength(10)]
        public string IsActivate { get; set; } = "ACTIVATE";

        // Navigation properties
        [ForeignKey("ServiceCategoryID")]
        public virtual ServiceCategory? ServiceCategory { get; set; }
        public virtual ICollection<RoomUsageService>? RoomUsageServices { get; set; }
    }
}
