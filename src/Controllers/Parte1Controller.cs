using Microsoft.AspNetCore.Mvc;
using ProvaPub.Services;

namespace ProvaPub.Controllers
{
    /// <summary>
    /// Controller da Parte 1
    /// Corrigido para gerar sempre números diferentes
    /// e salvar valores únicos no banco.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class Parte1Controller : ControllerBase
    {
        private readonly RandomService _randomService;

        public Parte1Controller(RandomService randomService)
        {
            _randomService = randomService;
        }

        [HttpGet]
        public async Task<ActionResult<int>> Index(CancellationToken ct)
        {
            try
            {
                // Chama o serviço para gerar e salvar número único
                var value = await _randomService.GetRandomAsync(ct);
                return Ok(value);
            }
            catch
            {
                // Caso não seja possível gerar, retorna erro 500
                return StatusCode(500, "Não foi possível gerar um número único agora. Tente novamente.");
            }
        }
    }
}
