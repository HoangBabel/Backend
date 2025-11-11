using Backend.Models;

namespace Backend.DTOs
{
    public class DailyRentalDtos
    {
        public record QuoteDailyRequestDto(int ProductId, DateTime StartDate, DateTime EndDate);
        public record QuoteDailyResponseDto(int Days, decimal PricePerDay, int AppliedThresholdDays, decimal? Deposit, decimal Subtotal);
        public record CreateDailyRentalRequestDto(
            int ProductId,
            DateTime StartDate,
            DateTime EndDate,
           /* string ShippingAddress, */// ✅ Thêm field bắt buộc
            int Quantity = 1

            // Thông tin shipping (optional)
            //int? ToProvinceId = null,
            //string? ToProvinceName = null,
            //int? ToDistrictId = null,
            //string? ToDistrictName = null,
            //string? ToWardCode = null,
            //string? ToWardName = null,
            //int? Weight = null,
            //int? Length = null,
            //int? Width = null,
            //int? Height = null,
        );

        public record CreateDailyRentalResponseDto(int RentalId, decimal TotalPrice, decimal DepositPaid, decimal FinalAmountToPay, RentalStatus Status);
    }
}
