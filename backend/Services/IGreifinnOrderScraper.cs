using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface IGreifinnOrderScraper
{
    Task<GreifinnOrderListDto> GetOrdersAsync(
        string? phoneNumber = null,
        string? customerName = null,
        string? customerAddress = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? locationId = null,
        int? deliveryMethodId = null,
        int? paymentMethodId = null,
        decimal? totalPrice = null,
        string? externalId = null,
        int? statusId = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<GreifinnOrderDetailDto> GetOrderDetailAsync(string orderId, CancellationToken cancellationToken = default);
}

