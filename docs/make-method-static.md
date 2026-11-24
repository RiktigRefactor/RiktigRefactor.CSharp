# Make method static

Make method static turns an instance method into a static method on the same type, with the receiver as a new first parameter, and rewrites every call.

```csharp
// Before
public decimal Total(int qty) => price * qty;
var t = order.Total(3);

// After
public static decimal Total(Order order, int qty) => order.price * qty;
var t = Order.Total(order, 3);
```

A struct receiver is passed by `ref`, or by `in` when the method is `readonly` or the struct is a `readonly struct`.
It refuses when it can't prove that every call keeps its behavior.

## Command

Make the method at line 5, column 20 static, naming its receiver parameter `self`:

```shell
riktig-cs make-method-static --workspace App.sln --file src/Order.cs --at 5:20 --receiver-name self
```

See [the command line](command-line.md) for the options every command accepts.
