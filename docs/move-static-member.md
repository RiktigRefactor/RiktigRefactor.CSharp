# Move static member

Move static member moves a static method (including an extension method), field, property, event or constant to another class, struct or record in the same project, and updates every reference.

References written through the source type, a subclass, or `using static`, and unqualified ones inside the source, become `Target.Member`.
Names in the moved declaration that were found through the source's scope are qualified, such as `Source.Rate`.
It refuses when it can't prove that the move keeps behavior, including static initialization order.

## Command

Move the static member at line 8, column 27 to `Shop.Pricing`:

```shell
riktig-cs move-static-member --workspace App.sln --file src/Order.cs --at 8:27 --target Shop.Pricing
```

See [the command line](command-line.md) for the options every command accepts.
