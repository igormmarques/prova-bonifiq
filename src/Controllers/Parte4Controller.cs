using Microsoft.AspNetCore.Mvc;
using ProvaPub.Services;

namespace ProvaPub.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Parte4Controller : ControllerBase
    {
        private readonly CustomerService _customerService;

        public Parte4Controller(CustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet("CanPurchase")]
        public async Task<ActionResult<bool>> CanPurchase(
            [FromQuery] int customerId,
            [FromQuery] decimal purchaseValue)
        {
            try
            {
                // Repassa a decisão para a regra de negócio
                var allowed = await _customerService.CanPurchase(customerId, purchaseValue);
                return Ok(allowed);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Parâmetro inválido (id <= 0, value <= 0)
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Cliente não encontrado
                return NotFound(ex.Message);
            }
            catch
            {
                // Falha inesperada
                return StatusCode(500, "Não foi possível validar a compra agora.");
            }
        }
    }
}
