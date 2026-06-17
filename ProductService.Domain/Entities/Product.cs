using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Domain.Entities; 

public class Product
{
    public Guid Id { get; private set; }
    //[StringLenth(50,ErrorMessage(""))]
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public string Description { get; private set; }
    public int Stock { get; private set; }

    // Constructeur privé : force l'utilisation de la méthode factory
    private Product() { }

    /// <summary>
    /// Méthode factory : point d'entrée unique pour créer un Product valide.
    /// Les invariants sont vérifiés ici, garantissant qu'un Product invalide ne peut pas exister.
    /// </summary>
    public static Product Create(string name, decimal price, string description, int stock = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));

        if (price <= 0)
            throw new ArgumentException("Price must be positive.", nameof(price));

        if (stock < 0)
            throw new ArgumentException("Stock cannot be negative.", nameof(stock));

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Description = description ?? string.Empty,
            Stock = stock
        };
    }

    /// <summary>
    /// Méthode de mise à jour : contrôle les modifications autorisées.
    /// </summary>
    public void Update(string name, decimal price, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));

        if (price <= 0)
            throw new ArgumentException("Price must be positive.", nameof(price));

        Name = name;
        Price = price;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Règle métier : décrémenter le stock uniquement si suffisant.
    /// </summary>
    public void DecrementStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        if (quantity > Stock)
            throw new InvalidOperationException($"Not enough stock. Available: {Stock}, Requested: {quantity}");

        Stock -= quantity;
    }
}

