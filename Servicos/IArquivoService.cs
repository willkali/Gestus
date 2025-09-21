namespace Gestus.Services;

public interface IArquivoService
{
    Task<string> SalvarImagemPerfilAsync(IFormFile arquivo, int usuarioId);
    Task<byte[]> ObterImagemPerfilAsync(string caminhoArquivo);
    Task<bool> ExcluirImagemPerfilAsync(string caminhoArquivo);
    bool ValidarImagemPerfil(IFormFile arquivo);
    string GerarUrlSegura(string caminhoArquivo);
}