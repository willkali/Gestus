using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Gestus.Dados;
using Gestus.Modelos;

namespace Gestus.Services;

public interface IChaveVersaoService
{
    Task<string> EncriptarComVersaoAsync(string texto, string contexto = "Email");
    Task<string> DescriptografarComVersaoAsync(string textoEncriptado, string contexto = "Email");
    Task<int> CriarNovaVersaoChaveAsync(string contexto, DateTime? dataExpiracao = null);
    Task<bool> DesativarChaveAntigaAsync(string contexto, int versaoParaManter = 2);
    Task<ChaveInfo> ObterChaveAtivaAsync(string contexto);
    Task<ChaveInfo?> ObterChavePorVersaoAsync(string contexto, int versao);
}

public class ChaveInfo
{
    public int Id { get; set; }
    public int Versao { get; set; }
    public string Chave { get; set; } = string.Empty;
    public bool Ativa { get; set; }
    public DateTime? DataExpiracao { get; set; }
}

public class ChaveVersaoService : IChaveVersaoService
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<ChaveVersaoService> _logger;
    private readonly IConfiguration _configuration;
    private readonly byte[] _masterKey; // Chave mestra para encriptar outras chaves

    public ChaveVersaoService(
        GestusDbContexto context,
        ILogger<ChaveVersaoService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        
        // Chave mestra (nunca muda, vem do ambiente/vault)
        var masterKeyString = Environment.GetEnvironmentVariable("GESTUS_MASTER_KEY") 
                             ?? _configuration.GetValue<string>("Security:MasterKey") 
                             ?? "GestusMasterKey2024!@#$%";
        _masterKey = SHA256.HashData(Encoding.UTF8.GetBytes(masterKeyString));
    }

    public async Task<string> EncriptarComVersaoAsync(string texto, string contexto = "Email")
    {
        try
        {
            var chaveInfo = await ObterChaveAtivaAsync(contexto);
            var chaveBytes = Convert.FromBase64String(chaveInfo.Chave);
            
            using var aes = Aes.Create();
            aes.Key = chaveBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            
            // ✅ FORMATO: [VERSAO:4bytes][IV:16bytes][DADOS_ENCRIPTADOS]
            var versaoBytes = BitConverter.GetBytes(chaveInfo.Versao);
            ms.Write(versaoBytes, 0, 4);
            ms.Write(aes.IV, 0, aes.IV.Length);
            
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs, Encoding.UTF8);
            
            sw.Write(texto);
            sw.Flush();
            cs.FlushFinalBlock();
            
            var resultado = Convert.ToBase64String(ms.ToArray());
            
            // Log da operação
            await LogOperacaoAsync(chaveInfo.Id, "Encriptar", contexto, null, true);
            
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao encriptar com versão - Contexto: {Contexto}", contexto);
            throw;
        }
    }

    public async Task<string> DescriptografarComVersaoAsync(string textoEncriptado, string contexto = "Email")
    {
        try
        {
            var dadosEncriptados = Convert.FromBase64String(textoEncriptado);
            
            // ✅ EXTRAIR VERSÃO DOS PRIMEIROS 4 BYTES
            var versaoBytes = new byte[4];
            Array.Copy(dadosEncriptados, 0, versaoBytes, 0, 4);
            var versao = BitConverter.ToInt32(versaoBytes, 0);
            
            _logger.LogDebug("🔍 Descriptografando com versão de chave: {Versao}", versao);
            
            // ✅ BUSCAR CHAVE PELA VERSÃO ESPECÍFICA
            var chaveInfo = await ObterChavePorVersaoAsync(contexto, versao);
            if (chaveInfo == null)
            {
                throw new InvalidOperationException($"Chave versão {versao} não encontrada para contexto {contexto}");
            }

            var chaveBytes = Convert.FromBase64String(chaveInfo.Chave);
            
            using var aes = Aes.Create();
            aes.Key = chaveBytes;
            
            // ✅ EXTRAIR IV (PULA OS 4 BYTES DA VERSÃO)
            var iv = new byte[16];
            Array.Copy(dadosEncriptados, 4, iv, 0, 16);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(dadosEncriptados, 20, dadosEncriptados.Length - 20); // Pula versão + IV
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            
            var resultado = sr.ReadToEnd();
            
            // Log da operação
            await LogOperacaoAsync(chaveInfo.Id, "Descriptografar", contexto, null, true);
            
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao descriptografar com versão - Contexto: {Contexto}", contexto);
            await LogOperacaoAsync(0, "Descriptografar", contexto, ex.Message, false);
            throw;
        }
    }

    public async Task<ChaveInfo> ObterChaveAtivaAsync(string contexto)
    {
        var chave = await _context.Set<ChaveEncriptacao>()
            .Where(c => c.Nome == contexto && c.Ativa)
            .Where(c => c.DataExpiracao == null || c.DataExpiracao > DateTime.UtcNow)
            .OrderByDescending(c => c.Versao)
            .FirstOrDefaultAsync();

        if (chave == null)
        {
            _logger.LogWarning("⚠️ Nenhuma chave ativa encontrada para {Contexto}. Criando nova...", contexto);
            var novaVersao = await CriarNovaVersaoChaveAsync(contexto);
            return await ObterChaveAtivaAsync(contexto); // Recursão para obter a recém-criada
        }

        var chaveDescriptografada = DescriptografarChave(chave.ChaveEncriptada);
        
        return new ChaveInfo
        {
            Id = chave.Id,
            Versao = chave.Versao,
            Chave = chaveDescriptografada,
            Ativa = chave.Ativa,
            DataExpiracao = chave.DataExpiracao
        };
    }

    public async Task<ChaveInfo?> ObterChavePorVersaoAsync(string contexto, int versao)
    {
        var chave = await _context.Set<ChaveEncriptacao>()
            .Where(c => c.Nome == contexto && c.Versao == versao)
            .FirstOrDefaultAsync();

        if (chave == null)
        {
            return null;
        }

        var chaveDescriptografada = DescriptografarChave(chave.ChaveEncriptada);
        
        return new ChaveInfo
        {
            Id = chave.Id,
            Versao = chave.Versao,
            Chave = chaveDescriptografada,
            Ativa = chave.Ativa,
            DataExpiracao = chave.DataExpiracao
        };
    }

    public async Task<int> CriarNovaVersaoChaveAsync(string contexto, DateTime? dataExpiracao = null)
    {
        try
        {
            // Obter próxima versão
            var ultimaVersao = await _context.Set<ChaveEncriptacao>()
                .Where(c => c.Nome == contexto)
                .MaxAsync(c => (int?)c.Versao) ?? 0;

            var novaVersao = ultimaVersao + 1;

            // Gerar nova chave
            using var rng = RandomNumberGenerator.Create();
            var chaveBytes = new byte[32]; // AES-256
            rng.GetBytes(chaveBytes);
            var chaveBase64 = Convert.ToBase64String(chaveBytes);

            // Encriptar com master key
            var chaveEncriptada = EncriptarChave(chaveBase64);

            var novaChaveEntidade = new ChaveEncriptacao
            {
                Nome = contexto,
                Versao = novaVersao,
                ChaveEncriptada = chaveEncriptada,
                Ativa = true,
                DataExpiracao = dataExpiracao,
                Observacoes = $"Chave gerada automaticamente - Versão {novaVersao}"
            };

            _context.Set<ChaveEncriptacao>().Add(novaChaveEntidade);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Nova chave criada - Contexto: {Contexto}, Versão: {Versao}", contexto, novaVersao);

            return novaVersao;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar nova versão de chave - Contexto: {Contexto}", contexto);
            throw;
        }
    }

    public async Task<bool> DesativarChaveAntigaAsync(string contexto, int versaoParaManter = 2)
    {
        try
        {
            var chavesAntigas = await _context.Set<ChaveEncriptacao>()
                .Where(c => c.Nome == contexto && c.Ativa)
                .OrderByDescending(c => c.Versao)
                .Skip(versaoParaManter) // Manter as X versões mais recentes
                .ToListAsync();

            foreach (var chave in chavesAntigas)
            {
                chave.Ativa = false;
                chave.DataDesativacao = DateTime.UtcNow;
                chave.Observacoes += " | Desativada automaticamente por rotação";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ {Count} chaves antigas desativadas - Contexto: {Contexto}", 
                chavesAntigas.Count, contexto);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao desativar chaves antigas - Contexto: {Contexto}", contexto);
            return false;
        }
    }

    private string EncriptarChave(string chave)
    {
        using var aes = Aes.Create();
        aes.Key = _masterKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        
        ms.Write(aes.IV, 0, aes.IV.Length);
        
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs, Encoding.UTF8);
        
        sw.Write(chave);
        sw.Flush();
        cs.FlushFinalBlock();
        
        return Convert.ToBase64String(ms.ToArray());
    }

    private string DescriptografarChave(string chaveEncriptada)
    {
        var dadosEncriptados = Convert.FromBase64String(chaveEncriptada);
        
        using var aes = Aes.Create();
        aes.Key = _masterKey;
        
        var iv = new byte[16];
        Array.Copy(dadosEncriptados, 0, iv, 0, 16);
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(dadosEncriptados, 16, dadosEncriptados.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        
        return sr.ReadToEnd();
    }

    private async Task LogOperacaoAsync(int chaveId, string operacao, string contexto, string? erro, bool sucesso)
    {
        try
        {
            var log = new LogUsoChave
            {
                ChaveEncriptacaoId = chaveId,
                Operacao = operacao,
                Contexto = contexto,
                Sucesso = sucesso,
                MensagemErro = erro
            };

            _context.Set<LogUsoChave>().Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao registrar log de uso de chave");
        }
    }
}