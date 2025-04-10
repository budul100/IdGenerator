# IdGenerator

[![NuGet](https://img.shields.io/nuget/v/budul.IdGenerator.svg)](https://www.nuget.org/packages/budul.IdGenerator/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A flexible, thread-safe unique identifier generator for .NET applications. This library provides a simple way to generate readable and predictable identifiers as an alternative to GUIDs when human-readability is important.

## Features

- Type-based prefix generation with intelligent string shrinking
- Custom prefix support
- Thread-safe implementation
- Configurable delimiters and formatting
- Automatic uniqueness handling
- Clean, readable identifiers

## Installation

Install the package via NuGet:

```bash
dotnet add package budul.IdGenerator
```

Or via the NuGet Package Manager:

```powershell
Install-Package budul.IdGenerator
```

## Usage

### Basic Usage

```csharp
// Create a generator with default settings
var generator = new IdGenerator();

// Generate IDs using type prefixes
string userId = generator.Generate<User>("john");            // "usr_john"
string orderId = generator.Generate<Order>("12345");         // "ordr_12345"
string productId = generator.Generate<Product>("keyboard");  // "prdct_keyboard"

// Automatic uniqueness for duplicate base IDs
string userId1 = generator.Generate<User>("john");           // "usr_john_2" (as "usr_john" was already used)
```

### Using Suffixes

```csharp
// Add multiple suffixes
string id = generator.Generate<Order>("main", "2023", "Q2"); // "ordr_main_2023_Q2"
```

### Custom Prefix

```csharp
// Generate IDs with custom prefixes instead of type names
string customId = generator.Generate("custom", "special");   // "custom_special"
```

### Configuration Options

```csharp
// Configure the generator with custom settings
var customGenerator = new IdGenerator(
    delimiter: "-",            // Use hyphen instead of underscore
    avoidCamelCases: true,      // Don't convert type names to camel case
    typePrefixLength: 6,       // Use up to 6 characters for type prefixes
    invalidCharactersPattern: "[^a-z0-9]+" // Only allow lowercase and numbers
);

string id = customGenerator.Generate<Product>("Gaming Keyboard"); // "produc-gamingkeyboard"
```

### String Shrinking Algorithm

The library includes an intelligent string shrinking algorithm that:

1. Removes vowels that aren't at the beginning of words
2. Splits input on non-alphanumeric characters
3. Proportionally allocates space to each part based on original length
4. Maintains readability while reducing length

Examples of type prefix shrinking:

```
"Customer" → "cstmr"
"OrderItem" → "ordrItm"
"ProductCatalog" → "prdctCtlg"
"ShoppingCart" → "shppngCrt"
```

You can also use the string shrinking functionality directly:

```csharp
using IdGenerator.Extensions;

string shortened = "ProductDescription".Shrink(maxLength: 8, avoidCamelCases: false);
// Result: "prdctDsc"
```

### Resetting the Generator

```csharp
// Clear all stored IDs and counters
generator.Reset();
```

## Examples

### Entity IDs in a Database Context

```csharp
public class ApplicationDbContext : DbContext
{
    private readonly IdGenerator _idGenerator = new IdGenerator();
    
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    
    public override int SaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is IEntity entity && string.IsNullOrEmpty(entity.Id))
                {
                    // Generate a unique ID based on the entity type
                    entity.Id = _idGenerator.Generate(entry.Entity.GetType());
                }
            }
        }
        
        return base.SaveChanges();
    }
}
```

### API Resource Identifiers

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IdGenerator _idGenerator = new IdGenerator();
    private readonly ProductRepository _repository;
    
    public ProductsController(ProductRepository repository)
    {
        _repository = repository;
    }
    
    [HttpPost]
    public IActionResult CreateProduct(ProductDto productDto)
    {
        var product = new Product
        {
            Id = _idGenerator.Generate<Product>(productDto.Name),
            Name = productDto.Name,
            // ...
        };
        
        _repository.Add(product);
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
    
    [HttpGet("{id}")]
    public IActionResult GetProduct(string id)
    {
        // ...
    }
}
```

## Performance Considerations

The IdGenerator is designed to be efficient and thread-safe. It uses concurrent collections to store identifier counts and used IDs, making it suitable for multi-threaded environments like web servers.

The string shrinking algorithm has been optimized for performance with compiled regex patterns and efficient character allocation.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.