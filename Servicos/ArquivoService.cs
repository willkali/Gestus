using System.Security.Cryptography;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Gestus.Services;

public class ArquivoService : IArquivoService
{
    private readonly ILogger<ArquivoService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _diretorioUploads;
    private readonly byte[] _chaveEncriptacao;

    public ArquivoService(ILogger<ArquivoService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _diretorioUploads = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "perfis");
        
        // Criar diretório se não existir
        Directory.CreateDirectory(_diretorioUploads);
        
        // Chave de encriptação (em produção, usar Key Vault ou similar)
        var chave = _configuration.GetValue<string>("Security:FileEncryptionKey") ?? "GestusDefaultKey123!@#";
        _chaveEncriptacao = SHA256.HashData(Encoding.UTF8.GetBytes(chave));
    }

    public async Task<string> SalvarImagemPerfilAsync(IFormFile arquivo, int usuarioId)
    {
        try
        {
            if (!ValidarImagemPerfil(arquivo))
            {
                throw new ArgumentException("Arquivo de imagem inválido");
            }

            // Gerar nome único e seguro
            var extensao = Path.GetExtension(arquivo.FileName).ToLower();
            var nomeArquivo = $"perfil_{usuarioId}_{Guid.NewGuid()}{extensao}";
            var caminhoCompleto = Path.Combine(_diretorioUploads, nomeArquivo);

            // Processar e redimensionar imagem
            using var stream = arquivo.OpenReadStream();
            using var imagem = await Image.LoadAsync(stream);
            
            // Redimensionar para 400x400 mantendo proporção
            imagem.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(400, 400),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            // Salvar temporariamente
            var tempPath = Path.GetTempFileName();
            await imagem.SaveAsJpegAsync(tempPath);

            // Encriptar e salvar
            var bytesImagem = await File.ReadAllBytesAsync(tempPath);
            var bytesEncriptados = EncriptarBytes(bytesImagem);
            await File.WriteAllBytesAsync(caminhoCompleto, bytesEncriptados);

            // Limpar arquivo temporário
            File.Delete(tempPath);

            _logger.LogInformation("Imagem de perfil salva: {NomeArquivo} para usuário {UsuarioId}", nomeArquivo, usuarioId);
            
            return nomeArquivo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar imagem de perfil para usuário {UsuarioId}", usuarioId);
            throw;
        }
    }

    public async Task<byte[]> ObterImagemPerfilAsync(string caminhoArquivo)
    {
        try
        {
            var caminhoCompleto = Path.Combine(_diretorioUploads, caminhoArquivo);
            
            if (!File.Exists(caminhoCompleto))
            {
                throw new FileNotFoundException("Imagem não encontrada");
            }

            var bytesEncriptados = await File.ReadAllBytesAsync(caminhoCompleto);
            var bytesDescriptografados = DescriptografarBytes(bytesEncriptados);
            
            return bytesDescriptografados;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter imagem de perfil: {Caminho}", caminhoArquivo);
            throw;
        }
    }

    // ✅ CORRIGIDO: Método assíncrono implementado
    public async Task<bool> ExcluirImagemPerfilAsync(string caminhoArquivo)
    {
        try
        {
            var caminhoCompleto = Path.Combine(_diretorioUploads, caminhoArquivo);
            
            if (File.Exists(caminhoCompleto))
            {
                await Task.Run(() => File.Delete(caminhoCompleto));
                _logger.LogInformation("Imagem de perfil excluída: {Caminho}", caminhoArquivo);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir imagem de perfil: {Caminho}", caminhoArquivo);
            return false;
        }
    }

    public bool ValidarImagemPerfil(IFormFile arquivo)
    {
        // Verificar se é arquivo válido
        if (arquivo == null || arquivo.Length == 0)
        {
            return false;
        }

        // Verificar tamanho (máximo 5MB)
        if (arquivo.Length > 5 * 1024 * 1024)
        {
            return false;
        }

        // Verificar extensão
        var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extensao = Path.GetExtension(arquivo.FileName).ToLower();
        
        if (!extensoesPermitidas.Contains(extensao))
        {
            return false;
        }

        // Verificar tipo MIME
        var tiposPermitidos = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!tiposPermitidos.Contains(arquivo.ContentType.ToLower()))
        {
            return false;
        }

        return true;
    }

    public string GerarUrlSegura(string caminhoArquivo)
    {
        // Gerar token temporário para acesso seguro
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var dados = $"{caminhoArquivo}:{timestamp}";
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(dados + _chaveEncriptacao)));
        
        return $"/api/usuarios/perfil/imagem/{caminhoArquivo}?t={timestamp}&h={Uri.EscapeDataString(hash)}";
    }

    private byte[] EncriptarBytes(byte[] dados)
    {
        using var aes = Aes.Create();
        aes.Key = _chaveEncriptacao;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        
        // Escrever IV no início
        ms.Write(aes.IV, 0, aes.IV.Length);
        
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(dados, 0, dados.Length);
        cs.FlushFinalBlock();
        
        return ms.ToArray();
    }

    private byte[] DescriptografarBytes(byte[] dadosEncriptados)
    {
        using var aes = Aes.Create();
        aes.Key = _chaveEncriptacao;
        
        // Extrair IV do início
        var iv = new byte[16];
        Array.Copy(dadosEncriptados, 0, iv, 0, 16);
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(dadosEncriptados, 16, dadosEncriptados.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var resultado = new MemoryStream();
        
        cs.CopyTo(resultado);
        return resultado.ToArray();
    }
}