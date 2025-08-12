using Orders.Domain;

namespace Orders.Infrastructure;

public class OrdersRepository
{
    private static List<Order> _data = new List<Order>();

    static OrdersRepository()
    {
        var now = DateTime.Now.AddDays(-2);
        _data.Add(new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Qty = 1,
            UnitPrice = 12.34m,
            DiscountPercent = 5,
            Status = "pending",
            CreatedAt = now
        });
        _data.Add(new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Qty = 3,
            UnitPrice = 9.99m,
            DiscountPercent = 0,
            Status = "paid",
            CreatedAt = now.AddDays(-1)
        });
    }

    public IEnumerable<Order> GetAll(string status, int page, int pageSize)
    {
        IEnumerable<Order> q = _data;
        if (!string.IsNullOrEmpty(status))
        {
            q = q.Where(x => (x.Status ?? "pending") == status);
        }
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        return q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    public Order GetById(Guid id)
    {
        return _data.FirstOrDefault(x => x.Id == id);
    }

    public void Update(Order o)
    {
        var i = _data.FindIndex(x => x.Id == o.Id);
        if (i < 0)
        {
            throw new Exception("not found");
        }
        _data[i] = o;
    }

    public List<Order> Raw() => _data;
}


