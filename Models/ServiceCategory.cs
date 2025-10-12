using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("ServiceCategory")]
    public class ServiceCategory
    {
        [Key]
        [Column("serviceCategoryID")]
        [StringLength(15)]
        public string ServiceCategoryID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên loại dịch vụ không được để trống")]
        [Column("serviceCategoryName")]
        [StringLength(50)]
        [Display(Name = "Tên loại dịch vụ")]
        public string ServiceCategoryName { get; set; } = string.Empty;

        [Column("isActivate")]
        [StringLength(10)]
        public string IsActivate { get; set; } = "ACTIVATE";

        // Navigation properties
        public virtual ICollection<HotelService>? HotelServices { get; set; }
    }
}
