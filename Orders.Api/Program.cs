using Orders.Api;
using Orders.Infrastructure;
using Orders.Domain;

#pragma warning disable CS8618
#pragma warning disable CS8602

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5083");
var app = builder.Build();

var svc = new OrdersService();

app.Urls.Add("http://127.0.0.1:5083");

app.MapGet("/orders", (HttpContext ctx) =>
{
    var status = ctx.Request.Query["status"].FirstOrDefault();
    int? page = null; int? pageSize = null;
    if (int.TryParse(ctx.Request.Query["page"], out var p)) page = p;
    if (int.TryParse(ctx.Request.Query["pageSize"], out var ps)) pageSize = ps;
    var tenant = ctx.Request.Headers["X-Tenant"].FirstOrDefault();
    return Results.Ok(svc.DoIt(status, page, pageSize, tenant));
});

app.MapGet("/orders/{id}", (Guid id, HttpContext ctx) =>
{
    var tenant = ctx.Request.Headers["X-Tenant"].FirstOrDefault();
    var r = svc.Run2(id, tenant);
    if (r is int code && code == 404) return Results.NotFound("not here");
    return Results.Ok(r);
});

app.MapPut("/orders/{id}", (Guid id, HttpContext ctx) =>
{
    var tenant = ctx.Request.Headers["X-Tenant"].FirstOrDefault();
    using var sr = new StreamReader(ctx.Request.Body);
    var body = sr.ReadToEnd();
    try
    {
        var doc = System.Text.Json.JsonDocument.Parse(body);
        int? qty = doc.RootElement.TryGetProperty("qty", out var q) ? q.GetInt32() : null;
        decimal? price = doc.RootElement.TryGetProperty("unitPrice", out var up) ? up.GetDecimal() : null;
        decimal? discount = doc.RootElement.TryGetProperty("discountPercent", out var dp) ? dp.GetDecimal() : null;
        string status = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : null;
        var res = svc.Update(id, qty, price, discount, status, tenant);
        if (res is string s && s == "fail") return Results.BadRequest(new { error = s });
        return Results.Ok(res);
    }
    catch
    {
        return Results.BadRequest("invalid json");
    }
});

app.MapPost("/orders/checkout/{id}", (Guid id, HttpContext ctx) =>
{
    var tenant = ctx.Request.Headers["X-Tenant"].FirstOrDefault();
    try
    {
        var r = svc.Checkout(id, tenant);
        if (r is int c && c == 404) return Results.NotFound();
        return Results.Ok(r);
    }
    catch (Exception ex)
    {
        Console.WriteLine("checkout error: " + ex.Message);
        // inconsistent
        return Results.BadRequest("oops");
    }
});

app.Run();
