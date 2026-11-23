public class Order
{
    public int Quantity = 4;

    /// <summary>Like <see cref="Order.Double"/>.</summary>
    public int Double() => this.Quantity * 2;
}
