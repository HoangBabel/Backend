using Backend.Models;

namespace Backend.Helpers
{
    public class RentalSettlementHelper
    {
        public static int ComputeLateDays(DateTime expectedEnd, DateTime returnedAt)
        {
            if (returnedAt <= expectedEnd) return 0;
            return (int)Math.Ceiling((returnedAt - expectedEnd).TotalDays);
        }

        /// Tính tổng phí trễ = sum(item.LateFeePerUnitAtBooking * lateDays)
        public static decimal ComputeTotalLateFee(Rental rental, DateTime returnedAt)
        {
            int lateDays = ComputeLateDays(rental.EndDate, returnedAt);
            if (lateDays <= 0) return 0m;

            decimal sum = 0m;
            foreach (var item in rental.Items)
            {
                if (item.LateFeePerUnitAtBooking is null) continue;
                sum += item.LateFeePerUnitAtBooking.Value * lateDays;
            }
            return Math.Round(sum, 2, MidpointRounding.AwayFromZero);
        }
    }
}
