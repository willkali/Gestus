namespace Gestus.DTOs.Grupo;

/// <summary>
/// Informações de um grupo específico para o usuário
/// </summary>
public class GrupoDoUsuario
{
    public int GrupoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Tipo { get; set; }
    public DateTime DataAdesao { get; set; }
    public bool Ativo { get; set; }
    public bool GrupoAtivo { get; set; }
    public int TotalMembrosGrupo { get; set; }
    public string? FuncaoNoGrupo { get; set; }
    public DateTime? DataUltimaAtividade { get; set; }
}