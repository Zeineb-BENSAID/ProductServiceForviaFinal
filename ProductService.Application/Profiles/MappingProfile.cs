using AutoMapper;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Application.Profiles;

/// <summary>
/// Profile AutoMapper : définit les conversions entre entités et DTOs.
/// ReverseMap() permet la conversion dans les deux sens automatiquement.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Product ↔ ProductDto (bidirectionnel)
        CreateMap<Product, ProductDTO>().ReverseMap();
    }
}
