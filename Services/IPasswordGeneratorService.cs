namespace Gestus.Services;

/// <summary>
/// Interface para serviço de geração de senhas seguras
/// </summary>
public interface IPasswordGeneratorService
{
    /// <summary>
    /// Gera uma senha temporária segura
    /// </summary>
    /// <param name="comprimento">Comprimento da senha (padrão: 12)</param>
    /// <param name="incluirCaracteresEspeciais">Se deve incluir caracteres especiais (padrão: true)</param>
    /// <param name="incluirNumeros">Se deve incluir números (padrão: true)</param>
    /// <param name="incluirMaiusculas">Se deve incluir letras maiúsculas (padrão: true)</param>
    /// <param name="incluirMinusculas">Se deve incluir letras minúsculas (padrão: true)</param>
    /// <returns>Senha temporária gerada</returns>
    string GerarSenhaTemporaria(
        int comprimento = 12,
        bool incluirCaracteresEspeciais = true,
        bool incluirNumeros = true,
        bool incluirMaiusculas = true,
        bool incluirMinusculas = true
    );

    /// <summary>
    /// Gera uma senha temporária específica para novos usuários
    /// </summary>
    /// <returns>Senha temporária otimizada para primeiros acessos</returns>
    string GerarSenhaPrimeiroAcesso();

    /// <summary>
    /// Valida se uma senha atende aos critérios mínimos de segurança
    /// </summary>
    /// <param name="senha">Senha a ser validada</param>
    /// <returns>True se a senha é válida, False caso contrário</returns>
    bool ValidarForcaSenha(string senha);

    /// <summary>
    /// Retorna os critérios de senha configurados
    /// </summary>
    /// <returns>Objeto com os critérios de senha</returns>
    object ObterCriteriosSenha();
}