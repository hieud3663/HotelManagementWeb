-- Script tạo tài khoản Admin cho hệ thống
USE HotelManagement;
GO

-- Xóa user cũ nếu tồn tại
DELETE FROM [User] WHERE username = 'admin';
GO

-- Tạo user admin mới
-- Password: admin
-- BCrypt hash của password "admin"
INSERT INTO [User] (userID, employeeID, username, passwordHash, role, isActivate)
VALUES ('USER-000000', 'EMP-000000', 'admin', '$2a$12$xcYMa7QynFUQXOLWdzH34uLa054xjxJdTnj71/qXmUgqP9YFMw6TK', 'ADMIN', 'ACTIVATE');
GO

-- Tạo thêm user nhân viên mẫu
-- Password: 123456
INSERT INTO [User] (userID, employeeID, username, passwordHash, role, isActivate)
VALUES ('USER-000002', 'EMP-000001', 'employee', '$2a$12$jeTXbZHye2jv3/kUJaSv.eGSGNG/pGLJYo2s632mUp/wY16kvnJ52', 'EMPLOYEE', 'ACTIVATE');
GO

PRINT 'Tạo tài khoản thành công!';
PRINT 'Admin - Username: admin, Password: admin';
PRINT 'Employee - Username: employee, Password: 123456';
GO
