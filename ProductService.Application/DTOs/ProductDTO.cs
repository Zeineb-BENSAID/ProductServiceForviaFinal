using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProductService.Application.DTOs;

public class ProductDTO
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 10000, ErrorMessage = "Price must be between 0.01 and 10,000.")]
    public decimal Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    public int Stock { get; set; }
}
//1-Sirine Ben Younes
//2-Sami Naifar
//3-Ahmed Ben Cheikh
//4-Baha Sallami
//5-Amal Daoud
//6-Molka Kardoun
//7-Mohamed Ben Rabie
//8-Melek Hajri




