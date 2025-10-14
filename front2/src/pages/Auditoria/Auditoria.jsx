import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import Paginacao from '../../components/Comuns/Paginacao';
import api from '../../services/api';
import './Auditoria.css';

const Auditoria = () => {
  const { temPermissao } = useAutenticacao();
  const [logs, setLogs] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [paginacao, setPaginacao] = useState({
    paginaAtual: 1,
    itensPorPagina: 20,
    totalItens: 0,
    totalPaginas: 0
  });
  const [filtros, setFiltros] = useState({
    usuario: '',
    acao: '',
    recurso: '',
    dataInicio: '',
    dataFim: '',
    ordenarPor: 'dataHora',
    direcaoOrdenacao: 'desc'
  });
  const [filtrosExpandidos, setFiltrosExpandidos] = useState(false);
  const [logSelecionado, setLogSelecionado] = useState(null);
  const [modalDetalhes, setModalDetalhes] = useState(false);

  // Lista de ações disponíveis
  const acoesDisponiveis = [
    'Login',
    'Logout', 
    'Criar',
    'Atualizar',
    'Excluir',
    'Visualizar',
    'Exportar',
    'Importar'
  ];

  // Lista de recursos disponíveis
  const recursosDisponiveis = [
    'Usuario',
    'Papel',
    'Grupo',
    'Aplicacao',
    'Permissao',
    'Configuracao',
    'Sistema'
  ];

  // Carregar logs de auditoria
  const carregarLogs = async (novaPagina = paginacao.paginaAtual) => {
    try {
      setCarregando(true);
      const params = new URLSearchParams({
        pagina: novaPagina,
        itensPorPagina: paginacao.itensPorPagina,
        ordenarPor: filtros.ordenarPor,
        direcaoOrdenacao: filtros.direcaoOrdenacao
      });
      
      if (filtros.usuario) params.append('usuario', filtros.usuario);
      if (filtros.acao) params.append('acao', filtros.acao);
      if (filtros.recurso) params.append('recurso', filtros.recurso);
      if (filtros.dataInicio) params.append('dataInicio', filtros.dataInicio);
      if (filtros.dataFim) params.append('dataFim', filtros.dataFim);
      
      const response = await api.get(`/api/auditoria?${params}`);
      setLogs(response.data.dados || []);
      setPaginacao(prev => ({
        ...prev,
        paginaAtual: response.data.paginaAtual || novaPagina,
        totalItens: response.data.totalItens || 0,
        totalPaginas: response.data.totalPaginas || 0
      }));
    } catch (erro) {
      console.error('Erro ao carregar logs:', erro);
      setLogs([]);
    } finally {
      setCarregando(false);
    }
  };

  useEffect(() => {
    if (temPermissao('Auditoria.Visualizar')) {
      carregarLogs();
    }
  }, [filtros, temPermissao]);

  // Aplicar filtros
  const aplicarFiltros = () => {
    setPaginacao(prev => ({ ...prev, paginaAtual: 1 }));
    carregarLogs(1);
  };

  // Limpar filtros
  const limparFiltros = () => {
    setFiltros({
      usuario: '',
      acao: '',
      recurso: '',
      dataInicio: '',
      dataFim: '',
      ordenarPor: 'dataHora',
      direcaoOrdenacao: 'desc'
    });
  };

  // Abrir modal de detalhes
  const abrirDetalhes = (log) => {
    setLogSelecionado(log);
    setModalDetalhes(true);
  };

  // Fechar modal
  const fecharModal = () => {
    setModalDetalhes(false);
    setLogSelecionado(null);
  };

  // Formatar data/hora
  const formatarDataHora = (dataString) => {
    const data = new Date(dataString);
    return data.toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  };

  // Obter ícone da ação
  const obterIconeAcao = (acao) => {
    const icones = {
      'Login': '🔐',
      'Logout': '🚪',
      'Criar': '➕',
      'Atualizar': '✏️',
      'Excluir': '🗑️',
      'Visualizar': '👁️',
      'Exportar': '📤',
      'Importar': '📥'
    };
    return icones[acao] || '📋';
  };

  // Obter cor da ação
  const obterCorAcao = (acao) => {
    const cores = {
      'Login': 'verde',
      'Logout': 'azul',
      'Criar': 'verde',
      'Atualizar': 'amarelo',
      'Excluir': 'vermelho',
      'Visualizar': 'azul',
      'Exportar': 'roxo',
      'Importar': 'roxo'
    };
    return cores[acao] || 'cinza';
  };

  // Exportar logs (simulação)
  const exportarLogs = () => {
    // Simulação de exportação
    alert('Funcionalidade de exportação será implementada');
  };

  if (!temPermissao('Auditoria.Visualizar')) {
    return (
      <div className="pagina-sem-permissao">
        <h2>Acesso Negado</h2>
        <p>Você não tem permissão para visualizar logs de auditoria.</p>
      </div>
    );
  }

  return (
    <div className="pagina-auditoria">
      {/* Cabeçalho */}
      <div className="cabecalho-pagina">
        <div className="titulo-secao">
          <h1>📋 Auditoria</h1>
          <p>Logs de atividades e eventos do sistema</p>
        </div>
        <div className="acoes-auditoria">
          <button 
            className="btn btn-neutro"
            onClick={exportarLogs}
          >
            📥 Exportar
          </button>
        </div>
      </div>

      {/* Filtros */}
      <div className="filtros-auditoria">
        <div className="card">
          <div className="card-body">
            <div className="cabecalho-filtros">
              <div className="filtros-rapidos">
                <input
                  type="text"
                  placeholder="🔍 Buscar por usuário..."
                  value={filtros.usuario}
                  onChange={(e) => setFiltros(prev => ({ ...prev, usuario: e.target.value }))}
                  className="input-busca-rapida"
                />
                <select 
                  value={filtros.acao}
                  onChange={(e) => setFiltros(prev => ({ ...prev, acao: e.target.value }))}
                  className="select-filtro"
                >
                  <option value="">Todas as ações</option>
                  {acoesDisponiveis.map(acao => (
                    <option key={acao} value={acao}>{acao}</option>
                  ))}
                </select>
              </div>
              
              <button 
                className="btn-expandir-filtros"
                onClick={() => setFiltrosExpandidos(!filtrosExpandidos)}
              >
                {filtrosExpandidos ? '▲ Menos filtros' : '▼ Mais filtros'}
              </button>
            </div>
            
            {filtrosExpandidos && (
              <div className="filtros-avancados">
                <div className="row-filtros">
                  <div className="campo-filtro">
                    <label>Recurso</label>
                    <select 
                      value={filtros.recurso}
                      onChange={(e) => setFiltros(prev => ({ ...prev, recurso: e.target.value }))}
                    >
                      <option value="">Todos os recursos</option>
                      {recursosDisponiveis.map(recurso => (
                        <option key={recurso} value={recurso}>{recurso}</option>
                      ))}
                    </select>
                  </div>
                  <div className="campo-filtro">
                    <label>Data Início</label>
                    <input
                      type="datetime-local"
                      value={filtros.dataInicio}
                      onChange={(e) => setFiltros(prev => ({ ...prev, dataInicio: e.target.value }))}
                    />
                  </div>
                  <div className="campo-filtro">
                    <label>Data Fim</label>
                    <input
                      type="datetime-local"
                      value={filtros.dataFim}
                      onChange={(e) => setFiltros(prev => ({ ...prev, dataFim: e.target.value }))}
                    />
                  </div>
                </div>
                
                <div className="acoes-filtros">
                  <button className="btn btn-secundario" onClick={aplicarFiltros}>🔍 Aplicar Filtros</button>
                  <button className="btn btn-neutro" onClick={limparFiltros}>🗑️ Limpar</button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Lista de Logs */}
      <div className="card">
        <div className="card-body">
          {carregando ? (
            <EstadoCarregamento mensagem="Carregando logs de auditoria..." />
          ) : logs.length > 0 ? (
            <>
              <div className="lista-logs">
                {logs.map((log, index) => (
                  <div 
                    key={log.id || index} 
                    className={`item-log ${obterCorAcao(log.acao)}`}
                    onClick={() => abrirDetalhes(log)}
                  >
                    <div className="icone-log">
                      {obterIconeAcao(log.acao)}
                    </div>
                    
                    <div className="conteudo-log">
                      <div className="linha-principal">
                        <div className="acao-log">
                          <strong>{log.acao}</strong> {log.recurso}
                        </div>
                        <div className="data-log">
                          {formatarDataHora(log.dataHora)}
                        </div>
                      </div>
                      
                      <div className="linha-secundaria">
                        <div className="usuario-log">
                          👤 {log.usuario?.nome || log.usuarioNome || 'Sistema'}
                        </div>
                        <div className="ip-log">
                          🌐 {log.enderecoIp || 'N/A'}
                        </div>
                      </div>
                      
                      {log.observacoes && (
                        <div className="observacoes-log">
                          {log.observacoes}
                        </div>
                      )}
                    </div>
                    
                    <div className="seta-detalhes">▶</div>
                  </div>
                ))}
              </div>
              
              <Paginacao 
                paginaAtual={paginacao.paginaAtual}
                totalPaginas={paginacao.totalPaginas}
                totalItens={paginacao.totalItens}
                onMudarPagina={(pagina) => carregarLogs(pagina)}
              />
            </>
          ) : (
            <div className="sem-dados">
              <span className="icone-vazio">📋</span>
              <h3>Nenhum log encontrado</h3>
              <p>Não foram encontrados logs de auditoria com os filtros aplicados.</p>
              <button className="btn btn-neutro" onClick={limparFiltros}>
                Limpar Filtros
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Modal de Detalhes */}
      {modalDetalhes && logSelecionado && (
        <div className="modal-overlay" onClick={fecharModal}>
          <div className="modal-content modal-log-detalhes" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>🔍 Detalhes do Log</h3>
              <button className="btn-fechar" onClick={fecharModal}>
                ✖️
              </button>
            </div>
            <div className="modal-body">
              <div className="detalhes-grid">
                <div className="detalhe-item">
                  <label>Ação:</label>
                  <span className={`badge-acao ${obterCorAcao(logSelecionado.acao)}`}>
                    {obterIconeAcao(logSelecionado.acao)} {logSelecionado.acao}
                  </span>
                </div>
                
                <div className="detalhe-item">
                  <label>Recurso:</label>
                  <span>{logSelecionado.recurso}</span>
                </div>
                
                <div className="detalhe-item">
                  <label>Usuário:</label>
                  <span>{logSelecionado.usuario?.nomeCompleto || logSelecionado.usuarioNome || 'Sistema'}</span>
                </div>
                
                <div className="detalhe-item">
                  <label>Data/Hora:</label>
                  <span>{formatarDataHora(logSelecionado.dataHora)}</span>
                </div>
                
                <div className="detalhe-item">
                  <label>Endereço IP:</label>
                  <span>{logSelecionado.enderecoIp || 'N/A'}</span>
                </div>
                
                <div className="detalhe-item">
                  <label>User Agent:</label>
                  <span className="user-agent">
                    {logSelecionado.userAgent || 'N/A'}
                  </span>
                </div>
                
                {logSelecionado.observacoes && (
                  <div className="detalhe-item span-full">
                    <label>Observações:</label>
                    <div className="observacoes-completas">
                      {logSelecionado.observacoes}
                    </div>
                  </div>
                )}
                
                {logSelecionado.detalhes && (
                  <div className="detalhe-item span-full">
                    <label>Detalhes Técnicos:</label>
                    <pre className="detalhes-tecnicos">
                      {typeof logSelecionado.detalhes === 'object' 
                        ? JSON.stringify(logSelecionado.detalhes, null, 2)
                        : logSelecionado.detalhes
                      }
                    </pre>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Auditoria;