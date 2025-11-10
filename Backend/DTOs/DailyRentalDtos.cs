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
            int Quantity = 1 // mặc định thuê 1 sản phẩm
        );

        public record CreateDailyRentalResponseDto(int RentalId, decimal TotalPrice, decimal DepositPaid, decimal FinalAmountToPay, RentalStatus Status);
    }
}
