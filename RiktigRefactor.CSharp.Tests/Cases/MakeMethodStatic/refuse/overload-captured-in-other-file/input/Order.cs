class Order
{
    decimal price = 2;

    public decimal /*|*/Total(int qty) => price * qty;

    public static decimal Total(object order, int qty) => -1;
}
