// Domain/Entities/ShippingAddress.cs
namespace ECommerce.OrderService.Domain.Entities;

public class ShippingAddress
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Line1 { get; private set; } = string.Empty;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = "India";

    public Order Order { get; private set; } = null!;

    private ShippingAddress() { }

    public static ShippingAddress Create(Guid orderId,
        string fullName, string phone, string line1,
        string city, string state, string postalCode,
        string? line2 = null, string country = "India")
        => new()
        {
            OrderId = orderId,
            FullName = fullName,
            Phone = phone,
            Line1 = line1,
            Line2 = line2,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country
        };
}