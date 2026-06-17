using FluentValidation;
using ProductService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Application.Validators;

public  class ProductDtoValidator : AbstractValidator<ProductDTO>
{
    public ProductDtoValidator()
    {
        RuleFor(p => p.Name)
       .NotEmpty().WithMessage("Product name is required.")
       .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
       .Must(name => !name.StartsWith(" ")).WithMessage("Name cannot start with a space.");

        RuleFor(p => p.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.")
            .LessThanOrEqualTo(10000).WithMessage("Price cannot exceed 10,000.");

        RuleFor(p => p.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(p => p.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.");
    }
}

