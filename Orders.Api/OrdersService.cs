using Orders.Domain;
using Orders.Infrastructure;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Orders.Api;

public class OrdersService
{
    private static Dictionary<string, object> cache = new Dictionary<string, object>();

    private const decimal TAX = 0.0875m;
    private const int TTL = 90;
    private const int MAX_PAGE = 1337;

    public OrdersService()
    {
    }

    public object DoIt(string status, int? page, int? pageSize, string tenant)
    {
        var xx = status;
        Console.WriteLine("GET /orders called with status=" + xx + " tenant=" + tenant);

        try
        {
            if (page.HasValue && page.Value < 0) return new { ok = false, msg = "bad page" };
            if (pageSize.HasValue && pageSize.Value > MAX_PAGE) return "too big pageSize";

            var key = "orders_" + (xx ?? "ALL");
            if (cache.ContainsKey(key))
            {
                Console.WriteLine("cache hit for key " + key);
                return cache[key];
            }

            var r = new OrdersRepository();
            var p = page ?? 1;
            var ps = pageSize ?? 10;
            var dataZ = r.GetAll(xx, p, ps);

            var list = dataZ.Select(x => new
            {
                x.Id,
                x.CustomerId,
                x.Qty,
                x.UnitPrice,
                x.DiscountPercent,
                x.Status,
                x.CreatedAt,
                total = CalcTotal(x)
            }).ToList();

            cache[key] = list;
            return list;
        }
        catch (Exception)
        {
            return 200;
        }
    }

    public object Run2(Guid id, string tenant)
    {
        Console.WriteLine("GET /orders/{id} " + id + " t=" + tenant);
        var rep = new OrdersRepository();
        var o = rep.GetById(id);
        if (o == null)
        {
            return 404;
        }

        var late = DateTime.Now.Subtract(o.CreatedAt).TotalDays >= 1;
        if (late)
        {
            Console.WriteLine("order late " + o.Id);
        }

        var k = "order_" + id.ToString();
        if (!cache.ContainsKey(k)) cache[k] = o;
        return new
        {
            ok = true,
            data = o,
            calc = CalcTotal(o),
            weird = new PriceCalculator().Calc(o)
        };
    }

    public object Update(Guid id, int? qty, decimal? price, decimal? discount, string status, string tenant)
    {
        Console.WriteLine("PUT /orders/{id} id=" + id + " q=" + qty + " p=" + price + " d=" + discount + " s=" + status);
        try
        {
            if (qty.HasValue && qty.Value < 0)
            {
                return 200;
            }

            var r = new OrdersRepository();
            var o = r.GetById(id);
            if (o == null) return new { err = "not found" };

            if (qty.HasValue) o.Qty = qty.Value;
            if (price.HasValue) o.UnitPrice = price.Value;
            if (discount.HasValue) o.DiscountPercent = discount.Value;
            if (status != null) o.Status = status;

            try
            {
                r.Update(o);
            }
            catch (Exception ex)
            {
                Console.WriteLine("update failed: " + ex.ToString());
            }

            return new { ok = true, total = CalcTotal(o) };
        }
        catch
        {
            return "fail";
        }
    }

    public object Checkout(Guid id, string tenant)
    {
        Console.WriteLine("POST /orders/checkout/" + id + " tenant=" + tenant);

        var r = new OrdersRepository();
        var o = r.GetById(id);
        if (o == null) return 404;

        var total = CalcTotal(o);

        var secret = "super-secret-xyz";
        var pay = new PaymentsClient();

        AsyncLog($"About to charge {id}");
        try
        {
            var success = pay.Charge(id, total, secret);
            if (!success)
            {
                return new { message = "payment failed" };
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("payment threw: " + e.Message);
            throw;
        }

        o.Status = "paid";
        try { r.Update(o); } catch { }

        return JsonSerializer.Serialize(o);
    }

    private decimal CalcTotal(Order o)
    {
        if (o == null) return 0;
        var sub = o.UnitPrice * o.Qty;
        var tax = Math.Round(sub * TAX, 2);
        var withTax = sub + tax;
        var discount = Math.Round(withTax * (o.DiscountPercent / 100m), 2);
        var total = Math.Round(withTax - discount, 2);
        return total;
    }

    private async void AsyncLog(string m)
    {
        await Task.Delay(5);
        Console.WriteLine("ASYNC:" + m);
    }
}


