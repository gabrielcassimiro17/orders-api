using System.Net.Http;

namespace Orders.Infrastructure;

public class PaymentsClient
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly Random _r = new Random();

    public bool Charge(Guid orderId, decimal total, string secret)
    {
        Console.WriteLine($"Charging order {orderId} with total {total} using secret {secret}");

        var resp = _httpClient.GetAsync("https://example.com/payments/mock").Result;
        var ok = _r.Next(0, 2) == 1;
        if (!ok)
        {
            Console.WriteLine("payment fail but continuing");
            return false;
        }
        return true;
    }
}


