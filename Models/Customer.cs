using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("Customer")]
    public class Customer
    {
        [Key]
        [Column("customerID")]
        [StringLength(15)]
        public string CustomerID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Column("fullName")]
        [StringLength(50)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Column("phoneNumber")]
        [StringLength(10, ErrorMessage = "Số điện thoại phải đúng định dạng")]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Column("email")]
        [StringLength(50)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Column("address")]
        [StringLength(100, ErrorMessage = "Địa chỉ không được vượt quá 100 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Required]
        [Column("gender")]
        [StringLength(6)]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; } = "MALE";

        [Required(ErrorMessage = "Số CMND/CCCD không được để trống")]
        [Column("idCardNumber")]
        [StringLength(12)]
        [Display(Name = "Số CMND/CCCD")]
        public string IdCardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        [Column("dob")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime Dob { get; set; }

        [Column("isActivate")]
        [StringLength(10)]
        public string IsActivate { get; set; } = "ACTIVATE";

        // Navigation properties
        public virtual ICollection<ReservationForm>? ReservationForms { get; set; }
    }
}
