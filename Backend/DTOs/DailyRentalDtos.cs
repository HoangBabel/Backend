using Backend.Models;

namespace Backend.DTOs
{
    public class DailyRentalDtos
    {
        public record QuoteDailyRequestDto(int ProductId, DateTime StartDate, DateTime EndDate);
        public record QuoteDailyResponseDto(int Days, decimal PricePerDay, int AppliedThresholdDays, decimal? Deposit, decimal Subtotal);

        public record CreateDailyRentalRequestDto(int ProductId, DateTime StartDate, DateTime EndDate);
        public record CreateDailyRentalResponseDto(int RentalId, decimal TotalPrice, decimal DepositPaid, decimal FinalAmountToPay, RentalStatus Status);
    }
}
