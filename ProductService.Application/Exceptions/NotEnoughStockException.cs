using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Application.Exceptions;

/// <summary>
/// Exception métier déclenchée lorsque le stock est insuffisant.
/// Vit dans l'Application Layer car c'est une règle applicative.
/// Le middleware global la capture et retourne un 400 Bad Request.
/// </summary>
public class NotEnoughStockException : Exception
{
    public NotEnoughStockException(string message) : base(message) { }
}
