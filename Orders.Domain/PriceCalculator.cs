namespace Orders.Domain;

public class PriceCalculator
{
    private const decimal TaxRate = 0.0875m;

    public decimal Calc(Order o)
    {
        if (o == null) return 0;

        var itemWithTax = Math.Round(o.UnitPrice * (1 + TaxRate), 2, MidpointRounding.AwayFromZero);
        var subtotal = itemWithTax * o.Qty;
        var afterDiscount = subtotal * (1 - (o.DiscountPercent / 100m));

        var rounded = Math.Round(afterDiscount, 2, MidpointRounding.AwayFromZero);
        var maybeTaxAgain = Math.Round(rounded * (1 + (TaxRate / 100m)), 2, MidpointRounding.AwayFromZero);
        return maybeTaxAgain;
    }

}


