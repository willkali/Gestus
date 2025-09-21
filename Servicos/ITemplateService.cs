using Gestus.DTOs.Sistema;

namespace Gestus.Services;

public interface ITemplateService
{
    Task<ValidacaoTemplateResponse> ValidarTemplateAsync(ValidarTemplateRequest request);
    Task<List<TipoTemplateResponse>> ObterTiposTemplateAsync();
    Task<TemplatePersonalizadoResponse> CriarTemplateAsync(CriarTemplateRequest request, int usuarioId);
    Task<TemplatePersonalizadoResponse?> AtualizarTemplateAsync(int templateId, CriarTemplateRequest request, int usuarioId);
    Task<bool> ExcluirTemplateAsync(int templateId, int usuarioId);
    Task<List<TemplatePersonalizadoResponse>> ListarTemplatesAsync(string? tipo = null, bool? ativo = null);
    Task<string> GerarPreviewAsync(string tipo, string template, Dictionary<string, string>? valores = null);
    Task InicializarTemplatesPadraoAsync();
}