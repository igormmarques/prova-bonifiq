using Microsoft.AspNetCore.Mvc;
using ProvaPub.Models;
using ProvaPub.Services;

namespace ProvaPub.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Parte3Controller : ControllerBase
    {
        private readonly OrderService _orderService;

        public Parte3Controller(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("orders")]
        public async Task<ActionResult<Order>> PlaceOrder(
            [FromQuery] string paymentMethod,
            [FromQuery] decimal paymentValue,
            [FromQuery] int customerId,
            CancellationToken ct)
        {
            try
            {
                var order = await _orderService.PayOrder(paymentMethod, paymentValue, customerId, ct);

                // Converte OrderDate para UTC-3 antes de retornar
                order.OrderDate = ConvertUtcToBrazil(order.OrderDate);

                return Ok(order);
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocorreu um erro ao processar o pagamento.");
            }
        }

        private static DateTime ConvertUtcToBrazil(DateTime utc)
        {
            // Garante que a data seja tratada como UTC
            if (utc.Kind != DateTimeKind.Utc)
                utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

            try
            {
                // Windows
                var tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            }
            catch
            {
                try
                {
                    //Validação casom seja outro TimeZoneId
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                    return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                }
                catch
                {
                    // Se não encontrar o fuso, retorna UTC mesmo
                    return utc;
                }
            }
        }
    }
}
