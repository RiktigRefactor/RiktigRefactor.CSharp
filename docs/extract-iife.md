# Extract IIFE

Extract IIFE (also called extract static lambda) wraps a selected expression, or whole statements of one block, in an immediately-invoked `static` lambda.

```csharp
// Before
var total = price * qty;

// After
var total = ((Func<decimal, int, decimal>)(static (price, qty) => price * qty))(price, qty);
```

Variables from outside become parameters, and `this` becomes a parameter named `@this`.
Variables the statements assign and the code after them reads, or declare and the code after them uses, are returned in a tuple and assigned at the call site.
It refuses when it can't prove that wrapping keeps behavior.
After extracting, it checks that the result compiles and that every name binds as before.

## Command

Preview an Extract IIFE of the text from line 12, column 19 up to (not including) column 30:

```shell
riktig-cs extract-iife --workspace App.sln --file src/Foo.cs --from 12:19 --to 12:30
```

See [the command line](command-line.md) for the options every command accepts.
