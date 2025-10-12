using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        [Column("userID")]
        [StringLength(15)]
        public string UserID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã nhân viên không được để trống")]
        [Column("employeeID")]
        [StringLength(15)]
        public string EmployeeID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [Column("username")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [Column("passwordHash")]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Column("role")]
        [StringLength(10)]
        public string Role { get; set; } = "EMPLOYEE";

        [Column("isActivate")]
        [StringLength(10)]
        public string IsActivate { get; set; } = "ACTIVATE";

        // Navigation properties
        [ForeignKey("EmployeeID")]
        public virtual Employee Employee { get; set; } = null!;
        
    }
}
