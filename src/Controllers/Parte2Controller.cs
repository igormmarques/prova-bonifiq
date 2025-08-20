// Controllers/Parte2Controller.cs
using Microsoft.AspNetCore.Mvc;
using ProvaPub.Models;
using ProvaPub.Services;

namespace ProvaPub.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Parte2Controller : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly CustomerService _customerService;

        // Injeção de dependência
        public Parte2Controller(ProductService productService, CustomerService customerService)
        {
            _productService = productService;
            _customerService = customerService;
        }

        [HttpGet("products")]
        public async Task<ActionResult<PagedResult<Product>>> ListProducts([FromQuery] int page = 1, CancellationToken ct = default)
        {
            var result = await _productService.ListProductsAsync(page, 10, ct);

            if (result.TotalItems > 0 && result.Page > result.TotalPages)
                return NotFound("Página fora do intervalo.");

            return Ok(result);
        }

        [HttpGet("customers")]
        public async Task<ActionResult<PagedResult<Customer>>> ListCustomers([FromQuery] int page = 1, CancellationToken ct = default)
        {
            var result = await _customerService.ListCustomersAsync(page, 10, ct);

            if (result.TotalItems > 0 && result.Page > result.TotalPages)
                return NotFound("Página fora do intervalo.");

            return Ok(result);
        }
    }
}
