using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HotelManagement.Data
{
    /// <summary>
    /// Model to receive sp_CreateReservation result
    /// </summary>
    public class ReservationResult
    {
        public string ReservationFormID { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string RoomID { get; set; } = string.Empty;
        public string RoomCategoryName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public double RoomBookingDeposit { get; set; }
        public int DaysBooked { get; set; }
    }

    /// <summary>
    /// Model to receive sp_QuickCheckin result
    /// </summary>
    public class CheckInResult
    {
        public string ReservationFormID { get; set; } = string.Empty;
        public string HistoryCheckInID { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public string RoomID { get; set; } = string.Empty;
        public string CheckinStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model to receive sp_QuickCheckout result
    /// </summary>
    public class CheckOutResult
    {
        public string ReservationFormID { get; set; } = string.Empty;
        public string HistoryCheckOutID { get; set; } = string.Empty;
        public DateTime CheckOutDate { get; set; }
        public decimal RoomCharge { get; set; }
        public decimal ServicesCharge { get; set; }
        public decimal TotalDue { get; set; }
        public decimal NetDue { get; set; }
        public string CheckoutStatus { get; set; } = string.Empty;
    }

    public static class DatabaseExtensions
    {
        /// <summary>
        /// Execute stored procedure sp_CreateReservation
        /// Returns result set with reservation details
        /// </summary>
        public static async Task<ReservationResult?> CreateReservationSP(
            this HotelManagementContext context,
            DateTime checkInDate,
            DateTime checkOutDate,
            string roomID,
            string customerID,
            string employeeID,
            double roomBookingDeposit)
        {
            var parameters = new[]
            {
                new SqlParameter("@checkInDate", checkInDate),
                new SqlParameter("@checkOutDate", checkOutDate),
                new SqlParameter("@roomID", roomID),
                new SqlParameter("@customerID", customerID),
                new SqlParameter("@employeeID", employeeID),
                new SqlParameter("@roomBookingDeposit", roomBookingDeposit)
            };

            var result = await context.Database
                .SqlQueryRaw<ReservationResult>(
                    "EXEC sp_CreateReservation @checkInDate, @checkOutDate, @roomID, @customerID, @employeeID, @roomBookingDeposit",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_QuickCheckin
        /// Automatically generates historyCheckInID
        /// </summary>
        public static async Task<CheckInResult?> CheckInRoomSP(
            this HotelManagementContext context,
            string reservationFormID,
            string employeeID)
        {
            var parameters = new[]
            {
                new SqlParameter("@reservationFormID", reservationFormID),
                new SqlParameter("@employeeID", employeeID)
            };

            var result = await context.Database
                .SqlQueryRaw<CheckInResult>(
                    "EXEC sp_QuickCheckin @reservationFormID, @employeeID",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Execute stored procedure sp_QuickCheckout
        /// Automatically generates historyCheckOutID and invoiceID
        /// </summary>
        public static async Task<CheckOutResult?> CheckOutRoomSP(
            this HotelManagementContext context,
            string reservationFormID,
            string employeeID)
        {
            var parameters = new[]
            {
                new SqlParameter("@reservationFormID", reservationFormID),
                new SqlParameter("@employeeID", employeeID)
            };

            var result = await context.Database
                .SqlQueryRaw<CheckOutResult>(
                    "EXEC sp_QuickCheckout @reservationFormID, @employeeID",
                    parameters)
                .ToListAsync();

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Generate ID using database function fn_GenerateID
        /// </summary>
        public static async Task<string> GenerateID(
            this HotelManagementContext context,
            string prefix,
            string tableName,
            int padLength = 6)
        {
            var sql = $"SELECT dbo.fn_GenerateID('{prefix}', '{tableName}', '', {padLength}) AS Value";
            
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            var result = await command.ExecuteScalarAsync();
            
            return result?.ToString() ?? string.Empty;
        }
    }
}
