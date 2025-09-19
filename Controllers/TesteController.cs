using Microsoft.AspNetCore.Mvc;

namespace Gestus.Controllers;

[ApiController]
[Route("api/[controller]")]  // ✅ Corrigido: [controller] é obrigatório pela linguagem
[Produces("application/json")]
public class TesteController : ControllerBase  // ✅ Nome em português seria TesteControlador, mas Controller é obrigatório
{
    /// <summary>
    /// Endpoint de teste para verificar se a API está funcionando
    /// </summary>
    /// <returns>Informações básicas da API</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Obter()  // ✅ Método em português
    {
        return Ok(new
        {
            Mensagem = "Gestus API está funcionando!",
            Timestamp = DateTime.UtcNow,
            Versao = "1.0.0",
            Ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }

    /// <summary>
    /// Endpoint de teste com parâmetro
    /// </summary>
    /// <param name="id">Identificador para teste</param>
    /// <returns>Informações do teste com ID</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ObterPorId(int id)  // ✅ Método em português
    {
        return Ok(new
        {
            Id = id,
            Mensagem = $"Teste com ID: {id}",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Endpoint POST de teste
    /// </summary>
    /// <param name="dados">Dados de teste</param>
    /// <returns>Dados processados</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Criar([FromBody] object dados)  // ✅ Método em português
    {
        return Ok(new
        {
            Mensagem = "Dados recebidos com sucesso",
            DadosRecebidos = dados,
            Timestamp = DateTime.UtcNow
        });
    }
}