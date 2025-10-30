    using Backend.Data;
    using Backend.DTOs;
    using Backend.Models;
    using Microsoft.EntityFrameworkCore;
    using static Backend.Helpers.DateTimeHelper; // nếu bạn muốn dùng helper TZ VN
    using static Backend.Helpers.VoucherCalculator;
    using static Backend.Helpers.VoucherValidator;

namespace Backend.Services;

    public interface ICheckoutService
    {
        Task<CheckoutOrderResponse> CheckoutOrderAsync(int userId, CheckoutOrderRequest req, CancellationToken ct);
        Task<CheckoutRentalResponse> CheckoutRentalByDaysAsync(CheckoutRentalByDaysRequest req, CancellationToken ct = default);
        Task<CheckoutRentalResponse> CheckoutRentalByDatesAsync(CheckoutRentalByDatesRequest req, CancellationToken ct = default);
    }

    public class CheckoutService : ICheckoutService
    {
        private readonly AppDbContext _context;

        public CheckoutService(AppDbContext context)
        {
            _context = context;
        }

    //public async Task<CheckoutOrderResponse> CheckoutOrderAsync(CheckoutOrderRequest req, CancellationToken ct = default)
    //{
    //    var cart = await _context.Carts
    //        .Include(c => c.Items)
    //        .ThenInclude(i => i.Product)
    //        .FirstOrDefaultAsync(c => c.UserId == req.UserId && !c.IsCheckedOut, ct);

    //    if (cart == null || !cart.Items.Any())
    //        throw new InvalidOperationException("Giỏ hàng trống hoặc không tồn tại.");

    //    // Tính tổng + trừ tồn
    //    decimal total = 0m;

    //    await using var tx = await _context.Database.BeginTransactionAsync(ct);
    //    try
    //    {
    //        foreach (var ci in cart.Items)
    //        {
    //            if (ci.Quantity <= 0)
    //                throw new InvalidOperationException($"Số lượng không hợp lệ cho sản phẩm Id={ci.ProductId}.");

    //            if (ci.Product == null)
    //                throw new InvalidOperationException($"Sản phẩm Id={ci.ProductId} không tồn tại.");

    //            if (ci.Product.Quantity < ci.Quantity)
    //                throw new InvalidOperationException($"Sản phẩm '{ci.Product.Name}' không đủ tồn.");

    //            total += ci.UnitPrice * ci.Quantity;

    //            // Trừ tồn theo số lượng mua
    //            ci.Product.Quantity -= ci.Quantity;
    //        }

    //        var order = new Order
    //        {
    //            UserId = req.UserId,
    //            TotalAmount = total,
    //            ShippingAddress = req.ShippingAddress,
    //            PaymentMethod = req.PaymentMethod,
    //            Status = OrderStatus.Pending,
    //            Items = new List<OrderItem>()
    //        };

    //        foreach (var ci in cart.Items)
    //        {
    //            order.Items.Add(new OrderItem
    //            {
    //                ProductId = ci.ProductId,
    //                Quantity = ci.Quantity,
    //                UnitPrice = ci.UnitPrice
    //            });
    //        }

    //        cart.IsCheckedOut = true;
    //        cart.UpdatedAt = DateTime.UtcNow;

    //        _context.Orders.Add(order);
    //        await _context.SaveChangesAsync(ct);
    //        await tx.CommitAsync(ct);

    //        return new CheckoutOrderResponse
    //        {
    //            OrderId = order.Id,
    //            TotalAmount = order.TotalAmount,
    //            Message = "Đặt hàng thành công!"
    //        };
    //    }
    //    catch
    //    {
    //        await tx.RollbackAsync(ct);
    //        throw;
    //    }
    //}
    public async Task<CheckoutOrderResponse> CheckoutOrderAsync(int userId, CheckoutOrderRequest req, CancellationToken ct)
    {
        // 1️⃣ Lấy giỏ hàng
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Giỏ hàng trống hoặc không tồn tại.");

        // 2️⃣ Tính tổng
        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        // 3️⃣ Áp dụng voucher nếu có
        Vouncher? voucher = null;
        decimal discount = 0m;

        if (!string.IsNullOrWhiteSpace(req.VoucherCode))
        {
            var code = req.VoucherCode.Trim();
            voucher = await _context.Vounchers.FirstOrDefaultAsync(v => v.Code == code, ct);
            if (voucher == null)
                throw new InvalidOperationException("Mã voucher không tồn tại.");

            if (!IsUsable(voucher, subtotal))
                throw new InvalidOperationException("Voucher không còn hiệu lực hoặc không đạt điều kiện.");

            discount = CalcDiscount(voucher, subtotal);
        }

        var finalAmount = subtotal - discount;
        if (finalAmount < 0) finalAmount = 0;

        using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                ShippingAddress = req.ShippingAddress,
                PaymentMethod = req.PaymentMethod,
                Status = OrderStatus.Pending,
                TotalAmount = subtotal,
                DiscountAmount = discount,
                FinalAmount = finalAmount,
                VoucherId = voucher?.Id,
                Voucher = voucher,
                VoucherCodeSnapshot = voucher?.Code
            };

            foreach (var item in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            cart.IsCheckedOut = true;

            if (voucher != null)
            {
                voucher.CurrentUsageCount += 1;
                voucher.UsedAt = DateTime.UtcNow;
                if (voucher.MaxUsageCount > 0 && voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                    voucher.IsValid = false;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new CheckoutOrderResponse
            {
                Message = "Đặt hàng thành công.",
                OrderId = order.Id,
                Subtotal = subtotal,
                Discount = discount,
                FinalAmount = finalAmount,
                PaymentMethod = req.PaymentMethod,
                VoucherCode = voucher?.Code
            };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    public async Task<CheckoutRentalResponse> CheckoutRentalByDaysAsync(CheckoutRentalByDaysRequest req, CancellationToken ct = default)
        {
            if (req.Items == null || req.Items.Count == 0)
                throw new InvalidOperationException("Đơn thuê phải có ít nhất 1 sản phẩm.");

            var now = DateTime.UtcNow;

            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var rental = new Rental
                {
                    UserId = req.UserId,
                    Status = RentalStatus.Pending,
                    StartDate = now,
                    Items = new List<RentalItem>()
                };

                foreach (var x in req.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.IdProduct == x.ProductId, ct);
                    if (product == null)
                        throw new InvalidOperationException($"Sản phẩm Id={x.ProductId} không tồn tại.");

                    if (product.Quantity <= 0)
                        throw new InvalidOperationException($"Sản phẩm '{product.Name}' đã hết hàng.");

                    if (x.RentalDays <= 0)
                        throw new InvalidOperationException($"RentalDays phải > 0 (ProductId={x.ProductId}).");

                    var pricePerDay = (x.PricePerDay.HasValue && x.PricePerDay.Value > 0) ? x.PricePerDay.Value : product.Price;

                    rental.Items.Add(new RentalItem
                    {
                        ProductId = x.ProductId,
                        RentalDays = x.RentalDays,
                        PricePerDay = pricePerDay,
                        SubTotal = pricePerDay * x.RentalDays
                    });

                    // Trừ tồn mỗi item = 1 đơn vị
                    product.Quantity -= 1;
                }

                rental.TotalPrice = rental.Items.Sum(i => i.SubTotal);
                var maxDays = rental.Items.Max(i => i.RentalDays);
                rental.EndDate = rental.StartDate.AddDays(maxDays);

                _context.Rentals.Add(rental);
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new CheckoutRentalResponse
                {
                    RentalId = rental.Id,
                    RentalDays = maxDays,
                    Message = "Tạo đơn thuê thành công"
                };
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<CheckoutRentalResponse> CheckoutRentalByDatesAsync(CheckoutRentalByDatesRequest req, CancellationToken ct = default)
        {
            if (req.Items == null || req.Items.Count == 0)
                throw new InvalidOperationException("Đơn thuê phải có ít nhất 1 sản phẩm.");

            // Bạn có thể dùng TZ Việt Nam nếu muốn validate theo ngày địa phương:
            // var tz = GetVietNamTz();
            // var startLocal = TimeZoneInfo.ConvertTimeFromUtc(req.StartDateUtc, tz).Date;
            // var endLocal = TimeZoneInfo.ConvertTimeFromUtc(req.EndDateUtc, tz).Date;

            var startUtc = DateTime.SpecifyKind(req.StartDateUtc, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(req.EndDateUtc, DateTimeKind.Utc);

            if (endUtc <= startUtc)
                throw new InvalidOperationException("EndDateUtc phải sau StartDateUtc.");

            var rentalDays = (int)Math.Ceiling((endUtc - startUtc).TotalDays);
            if (rentalDays < 1) rentalDays = 1;

            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var rental = new Rental
                {
                    UserId = req.UserId,
                    Status = RentalStatus.Pending,
                    StartDate = startUtc,
                    EndDate = endUtc,
                    Items = new List<RentalItem>()
                };

                foreach (var x in req.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.IdProduct == x.ProductId, ct);
                    if (product == null)
                        throw new InvalidOperationException($"Sản phẩm Id={x.ProductId} không tồn tại.");

                    if (product.Quantity <= 0)
                        throw new InvalidOperationException($"Sản phẩm '{product.Name}' đã hết hàng.");

                    var pricePerDay = (x.PricePerDay.HasValue && x.PricePerDay.Value > 0)
                                      ? x.PricePerDay.Value
                                      : product.Price;

                    rental.Items.Add(new RentalItem
                    {
                        ProductId = x.ProductId,
                        RentalDays = rentalDays,
                        PricePerDay = pricePerDay,
                        SubTotal = pricePerDay * rentalDays
                    });

                    product.Quantity -= 1;
                }

                rental.TotalPrice = rental.Items.Sum(i => i.SubTotal);

                _context.Rentals.Add(rental);
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new CheckoutRentalResponse
                {
                    RentalId = rental.Id,
                    RentalDays = rentalDays,
                    Message = "Tạo đơn thuê thành công"
                };
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }


