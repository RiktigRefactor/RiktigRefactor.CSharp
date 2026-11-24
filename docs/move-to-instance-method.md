# Move static method to instance method

Move static method to instance method is the inverse of Make method static: a parameter of a class type becomes the receiver, the method moves into that class, and every call passes the argument as the receiver.

```csharp
// Before
public static class Pricing { public static decimal Total(Order order, int qty) => order.Price * qty; }
var t = Pricing.Total(order, 3);

// After
public class Order { public decimal Total(int qty) => this.Price * qty; }
var t = order.Total(3);
```

It refuses when it can't prove that every call keeps its behavior, including when a receiver might be null or would be evaluated earlier.

## Command

Make the static method at line 3, column 27 an instance method on its `order` parameter's type:

```shell
riktig-cs move-to-instance-method --workspace App.sln --file src/Pricing.cs --at 3:27 --receiver order
```

See [the command line](command-line.md) for the options every command accepts.
