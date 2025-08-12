namespace Orders.Domain;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}


