using AutoMapper;
using Moq;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Application.Services;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Test
{
    public class PSUnitTest
    {
        private readonly Mock<IGenericRepository<Product>> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly IProductService _service;

        public PSUnitTest()
        {
            _repositoryMock = new Mock<IGenericRepository<Product>>();
            _mapperMock = new Mock<IMapper>();
            // ✅ Fix 1: Injection correcte des dépendances
            _service = new ProductService.Application.Services.ProductService(
                _repositoryMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedProducts()
        {

            var products = new List<Product>
            {
                Product.Create( "Test",100, "Desc",2 )
            };
            // ✅ Fix 2: Casse uniforme ProductDto (pas ProductDTO)
            var productDtos = new List<ProductDTO>
            {
                new ProductDTO { Name = "Test", Description = "Desc" ,Price=100,Stock=2}
            };

            _repositoryMock
                .Setup(r => r.GetAllAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(),
                    It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>>()))
                .ReturnsAsync(products);

            _mapperMock
                .Setup(m => m.Map<IEnumerable<ProductDTO>>(products))
                .Returns(productDtos);

            var result = await _service.GetAllAsync(1, 10);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Test", result.First().Name);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMappedProduct()
        {
            var id = Guid.NewGuid();
            var product = Product.Create("Test",580, "Desc");
            var productDto = new ProductDTO { Name = "Test", Description = "Desc",Price=580 };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(product);

            _mapperMock
                .Setup(m => m.Map<ProductDTO>(product))
                .Returns(productDto);

            var result = await _service.GetByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task CreateAsync_AddsProductAndReturnsDto()
        {
            var productDto = new ProductDTO { Name = "Test", Description = "Desc",Price=200};
            var product = Product.Create("Test", 200, "Desc");

            _mapperMock
                .Setup(m => m.Map<Product>(productDto))
                .Returns(product);

            _repositoryMock
                .Setup(r => r.AddAsync(product))
                .Returns(Task.CompletedTask);

            _mapperMock
                .Setup(m => m.Map<ProductDTO>(product))
                .Returns(productDto);

            var result = await _service.CreateAsync(productDto);

            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesExistingProduct()
        {
            var id = Guid.NewGuid();
            var productDto = new ProductDTO { Name = "Updated", Description = "Updated Desc",Price=200,Stock=80 };
            var existingProduct = Product.Create("Old",200, "Old Desc",80);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(existingProduct);

            // ✅ Fix 3: Ajout du retour .Returns(existingProduct) sur le Map de mise à jour
            _mapperMock
                .Setup(m => m.Map(productDto, existingProduct))
                .Returns(existingProduct);

            _repositoryMock
                .Setup(r => r.UpdateAsync(existingProduct))
                .Returns(Task.CompletedTask);

            await _service.UpdateAsync(id, productDto);

            _repositoryMock.Verify(r => r.UpdateAsync(existingProduct), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_DeletesExistingProduct()
        {
            var id = Guid.NewGuid();
            // ✅ Fix 4: new Product { ... } au lieu de ProductService { ... }
            var product = Product.Create("Test",230, "Desc");

            _repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(product);

            _repositoryMock
                .Setup(r => r.DeleteAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            await _service.DeleteAsync(id);

            _repositoryMock.Verify(r => r.DeleteAsync(product), Times.Once);
        }
    }
}