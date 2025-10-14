import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import Paginacao from '../../components/Comuns/Paginacao';
import ModalConfirmacao from '../../components/Comuns/ModalConfirmacao';
import api from '../../services/api';
import './Papeis.css';

const Papeis = () => {
  const { temPermissao } = useAutenticacao();
  const [papeis, setPapeis] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [paginacao, setPaginacao] = useState({
    paginaAtual: 1,
    itensPorPagina: 10,
    totalItens: 0,
    totalPaginas: 0
  });
  const [filtros, setFiltros] = useState({
    busca: '',
    ativo: '',
    ordenarPor: 'nome',
    direcaoOrdenacao: 'asc'
  });
  const [permissoes, setPermissoes] = useState([]);
  const [modalPapel, setModalPapel] = useState({ aberto: false, papel: null, modo: 'criar' });
  const [modalPermissoes, setModalPermissoes] = useState({ aberto: false, papel: null });
  const [modalConfirmacao, setModalConfirmacao] = useState({ aberto: false, papel: null });
  const [formulario, setFormulario] = useState({
    nome: '',
    descricao: '',
    ativo: true,
    permissoes: []
  });
  const [errosFormulario, setErrosFormulario] = useState({});
  const [salvandoPapel, setSalvandoPapel] = useState(false);
  const [permissoesExpandidas, setPermissoesExpandidas] = useState({});

  // Carregar papéis
  const carregarPapeis = async (novaPagina = paginacao.paginaAtual) => {
    try {
      setCarregando(true);
      const params = new URLSearchParams({
        pagina: novaPagina,
        itensPorPagina: paginacao.itensPorPagina,
        ordenarPor: filtros.ordenarPor,
        direcaoOrdenacao: filtros.direcaoOrdenacao
      });
      
      if (filtros.busca) params.append('busca', filtros.busca);
      if (filtros.ativo !== '') params.append('ativo', filtros.ativo);
      
      const response = await api.get(`/api/papeis?${params}`);
      setPapeis(response.data.dados || []);
      setPaginacao(prev => ({
        ...prev,
        paginaAtual: response.data.paginaAtual || novaPagina,
        totalItens: response.data.totalItens || 0,
        totalPaginas: response.data.totalPaginas || 0
      }));
    } catch (erro) {
      console.error('Erro ao carregar papéis:', erro);
      setPapeis([]);
    } finally {
      setCarregando(false);
    }
  };

  // Carregar permissões disponíveis
  const carregarPermissoes = async () => {
    try {
      const response = await api.get('/api/permissoes');
      setPermissoes(response.data || []);
    } catch (erro) {
      console.error('Erro ao carregar permissões:', erro);
    }
  };

  useEffect(() => {
    if (temPermissao('Papeis.Listar')) {
      carregarPapeis();
    }
    if (temPermissao('Permissoes.Listar')) {
      carregarPermissoes();
    }
  }, [filtros, temPermissao]);

  // Aplicar filtros
  const aplicarFiltros = () => {
    setPaginacao(prev => ({ ...prev, paginaAtual: 1 }));
    carregarPapeis(1);
  };

  // Limpar filtros
  const limparFiltros = () => {
    setFiltros({
      busca: '',
      ativo: '',
      ordenarPor: 'nome',
      direcaoOrdenacao: 'asc'
    });
  };

  // Abrir modal de papel
  const abrirModalPapel = (papel = null, modo = 'criar') => {
    if (modo === 'editar' && papel) {
      setFormulario({
        nome: papel.nome || '',
        descricao: papel.descricao || '',
        ativo: papel.ativo !== undefined ? papel.ativo : true,
        permissoes: papel.permissoes || []
      });
    } else {
      setFormulario({
        nome: '',
        descricao: '',
        ativo: true,
        permissoes: []
      });
    }
    setErrosFormulario({});
    setModalPapel({ aberto: true, papel, modo });
  };

  // Abrir modal de permissões
  const abrirModalPermissoes = (papel) => {
    setModalPermissoes({ aberto: true, papel });
  };

  // Validar formulário
  const validarFormulario = () => {
    const erros = {};
    
    if (!formulario.nome.trim()) erros.nome = 'Nome é obrigatório';
    else if (formulario.nome.length < 3) erros.nome = 'Nome deve ter pelo menos 3 caracteres';
    
    if (!formulario.descricao.trim()) erros.descricao = 'Descrição é obrigatória';
    
    setErrosFormulario(erros);
    return Object.keys(erros).length === 0;
  };

  // Salvar papel
  const salvarPapel = async () => {
    if (!validarFormulario()) return;
    
    try {
      setSalvandoPapel(true);
      const dados = {
        nome: formulario.nome.trim(),
        descricao: formulario.descricao.trim(),
        ativo: formulario.ativo,
        permissoes: formulario.permissoes
      };
      
      if (modalPapel.modo === 'criar') {
        await api.post('/api/papeis', dados);
      } else {
        await api.put(`/api/papeis/${modalPapel.papel.id}`, dados);
      }
      
      setModalPapel({ aberto: false, papel: null, modo: 'criar' });
      carregarPapeis();
    } catch (erro) {
      console.error('Erro ao salvar papel:', erro);
      if (erro.response?.data?.errors) {
        const errosApi = {};
        Object.keys(erro.response.data.errors).forEach(campo => {
          errosApi[campo] = erro.response.data.errors[campo][0];
        });
        setErrosFormulario(errosApi);
      }
    } finally {
      setSalvandoPapel(false);
    }
  };

  // Confirmar exclusão
  const confirmarExclusao = (papel) => {
    setModalConfirmacao({ aberto: true, papel });
  };

  // Excluir papel
  const excluirPapel = async () => {
    try {
      await api.delete(`/api/papeis/${modalConfirmacao.papel.id}`);
      setModalConfirmacao({ aberto: false, papel: null });
      carregarPapeis();
    } catch (erro) {
      console.error('Erro ao excluir papel:', erro);
    }
  };

  // Alternar status do papel
  const alternarStatus = async (papel) => {
    try {
      await api.put(`/api/papeis/${papel.id}`, {
        ...papel,
        ativo: !papel.ativo
      });
      carregarPapeis();
    } catch (erro) {
      console.error('Erro ao alterar status:', erro);
    }
  };

  // Agrupar permissões por recurso
  const agruparPermissoesPorRecurso = (permissoes) => {
    const grupos = {};
    permissoes.forEach(permissao => {
      const [recurso] = permissao.split('.');
      if (!grupos[recurso]) grupos[recurso] = [];
      grupos[recurso].push(permissao);
    });
    return grupos;
  };

  // Alternar expansão de permissões
  const alternarExpansao = (recurso) => {
    setPermissoesExpandidas(prev => ({
      ...prev,
      [recurso]: !prev[recurso]
    }));
  };

  if (!temPermissao('Papeis.Listar')) {
    return (
      <div className="pagina-sem-permissao">
        <h2>Acesso Negado</h2>
        <p>Você não tem permissão para visualizar papéis.</p>
      </div>
    );
  }

  return (
    <div className="pagina-papeis">
      {/* Cabeçalho */}
      <div className="cabecalho-pagina">
        <div className="titulo-secao">
          <h1>🎭 Papéis</h1>
          <p>Gerenciar papéis e permissões do sistema</p>
        </div>
        {temPermissao('Papeis.Criar') && (
          <button 
            className="btn btn-primario"
            onClick={() => abrirModalPapel()}
          >
            ➕ Novo Papel
          </button>
        )}
      </div>

      {/* Filtros */}
      <div className="filtros-papeis">
        <div className="card">
          <div className="card-body">
            <div className="row-filtros">
              <div className="campo-filtro">
                <input
                  type="text"
                  placeholder="🔍 Buscar papel..."
                  value={filtros.busca}
                  onChange={(e) => setFiltros(prev => ({ ...prev, busca: e.target.value }))}
                  className="input-busca"
                />
              </div>
              <div className="campo-filtro">
                <select 
                  value={filtros.ativo}
                  onChange={(e) => setFiltros(prev => ({ ...prev, ativo: e.target.value }))}
                  className="select-filtro"
                >
                  <option value="">Todos os status</option>
                  <option value="true">Ativos</option>
                  <option value="false">Inativos</option>
                </select>
              </div>
              <div className="acoes-filtro">
                <button className="btn btn-secundario" onClick={aplicarFiltros}>🔍 Filtrar</button>
                <button className="btn btn-neutro" onClick={limparFiltros}>🗑️ Limpar</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Lista de Papéis */}
      <div className="card">
        <div className="card-body">
          {carregando ? (
            <EstadoCarregamento mensagem="Carregando papéis..." />
          ) : papeis.length > 0 ? (
            <>
              <div className="grid-papeis">
                {papeis.map(papel => (
                  <div key={papel.id} className={`card-papel ${!papel.ativo ? 'inativo' : ''}`}>
                    <div className="cabecalho-papel">
                      <div className="info-papel">
                        <h3 className="nome-papel">{papel.nome}</h3>
                        <p className="descricao-papel">{papel.descricao}</p>
                        <span className={`badge ${papel.ativo ? 'badge-sucesso' : 'badge-erro'}`}>
                          {papel.ativo ? '✅ Ativo' : '❌ Inativo'}
                        </span>
                      </div>
                      <div className="acoes-papel">
                        {temPermissao('Papeis.Visualizar') && (
                          <button 
                            className="btn-acao btn-visualizar"
                            onClick={() => abrirModalPermissoes(papel)}
                            title="Ver permissões"
                          >
                            🔍
                          </button>
                        )}
                        {temPermissao('Papeis.Atualizar') && (
                          <button 
                            className="btn-acao btn-editar"
                            onClick={() => abrirModalPapel(papel, 'editar')}
                            title="Editar papel"
                          >
                            ✏️
                          </button>
                        )}
                        {temPermissao('Papeis.Atualizar') && (
                          <button 
                            className={`btn-acao ${papel.ativo ? 'btn-desativar' : 'btn-ativar'}`}
                            onClick={() => alternarStatus(papel)}
                            title={papel.ativo ? 'Desativar' : 'Ativar'}
                          >
                            {papel.ativo ? '🚫' : '✅'}
                          </button>
                        )}
                        {temPermissao('Papeis.Excluir') && (
                          <button 
                            className="btn-acao btn-excluir"
                            onClick={() => confirmarExclusao(papel)}
                            title="Excluir papel"
                          >
                            🗑️
                          </button>
                        )}
                      </div>
                    </div>
                    
                    <div className="detalhes-papel">
                      <div className="estatistica">
                        <span className="numero">{papel.permissoes?.length || 0}</span>
                        <span className="label">Permissões</span>
                      </div>
                      <div className="estatistica">
                        <span className="numero">{papel.totalUsuarios || 0}</span>
                        <span className="label">Usuários</span>
                      </div>
                      <div className="estatistica">
                        <span className="numero">ID: {papel.id}</span>
                        <span className="label">Identificador</span>
                      </div>
                    </div>
                    
                    {papel.permissoes && papel.permissoes.length > 0 && (
                      <div className="preview-permissoes">
                        <strong>Permissões:</strong>
                        <div className="badges-permissoes">
                          {papel.permissoes.slice(0, 3).map((permissao, index) => (
                            <span key={index} className="badge badge-permissao">
                              {permissao}
                            </span>
                          ))}
                          {papel.permissoes.length > 3 && (
                            <span className="badge badge-mais">+{papel.permissoes.length - 3}</span>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                ))}
              </div>
              
              <Paginacao 
                paginaAtual={paginacao.paginaAtual}
                totalPaginas={paginacao.totalPaginas}
                totalItens={paginacao.totalItens}
                onMudarPagina={(pagina) => carregarPapeis(pagina)}
              />
            </>
          ) : (
            <div className="sem-dados">
              <span className="icone-vazio">🎭</span>
              <h3>Nenhum papel encontrado</h3>
              <p>Não há papéis cadastrados ou que correspondam aos filtros aplicados.</p>
              {temPermissao('Papeis.Criar') && (
                <button 
                  className="btn btn-primario"
                  onClick={() => abrirModalPapel()}
                >
                  ➕ Criar Primeiro Papel
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Modal de Papel */}
      {modalPapel.aberto && (
        <div className="modal-overlay">
          <div className="modal-content modal-papel">
            <div className="modal-header">
              <h3>
                {modalPapel.modo === 'criar' ? '➕ Novo Papel' : '✏️ Editar Papel'}
              </h3>
              <button 
                className="btn-fechar"
                onClick={() => setModalPapel({ aberto: false, papel: null, modo: 'criar' })}
              >
                ✖️
              </button>
            </div>
            <div className="modal-body">
              <div className="campo-form">
                <label>Nome do Papel *</label>
                <input
                  type="text"
                  value={formulario.nome}
                  onChange={(e) => setFormulario(prev => ({ ...prev, nome: e.target.value }))}
                  className={errosFormulario.nome ? 'input-erro' : ''}
                  placeholder="Digite o nome do papel"
                />
                {errosFormulario.nome && <span className="erro-campo">{errosFormulario.nome}</span>}
              </div>
              
              <div className="campo-form">
                <label>Descrição *</label>
                <textarea
                  value={formulario.descricao}
                  onChange={(e) => setFormulario(prev => ({ ...prev, descricao: e.target.value }))}
                  className={errosFormulario.descricao ? 'input-erro' : ''}
                  placeholder="Descreva as responsabilidades deste papel"
                  rows={3}
                />
                {errosFormulario.descricao && <span className="erro-campo">{errosFormulario.descricao}</span>}
              </div>
              
              <div className="campo-form">
                <label>Permissões</label>
                <div className="permissoes-papel">
                  {Object.entries(agruparPermissoesPorRecurso(permissoes)).map(([recurso, permissoesRecurso]) => (
                    <div key={recurso} className="grupo-permissoes">
                      <div 
                        className="cabecalho-grupo"
                        onClick={() => alternarExpansao(recurso)}
                      >
                        <span className="icone-grupo">
                          {permissoesExpandidas[recurso] ? '▼' : '▶'}
                        </span>
                        <strong>{recurso}</strong>
                        <span className="contador-grupo">({permissoesRecurso.length})</span>
                      </div>
                      {permissoesExpandidas[recurso] && (
                        <div className="lista-permissoes">
                          {permissoesRecurso.map(permissao => (
                            <label key={permissao} className="checkbox-permissao">
                              <input
                                type="checkbox"
                                checked={formulario.permissoes.includes(permissao)}
                                onChange={(e) => {
                                  if (e.target.checked) {
                                    setFormulario(prev => ({ 
                                      ...prev, 
                                      permissoes: [...prev.permissoes, permissao] 
                                    }));
                                  } else {
                                    setFormulario(prev => ({ 
                                      ...prev, 
                                      permissoes: prev.permissoes.filter(p => p !== permissao) 
                                    }));
                                  }
                                }}
                              />
                              {permissao.split('.')[1]}
                            </label>
                          ))}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
              
              <div className="campo-form">
                <label className="checkbox-ativo">
                  <input
                    type="checkbox"
                    checked={formulario.ativo}
                    onChange={(e) => setFormulario(prev => ({ ...prev, ativo: e.target.checked }))}
                  />
                  Papel ativo
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <button 
                className="btn btn-secundario"
                onClick={() => setModalPapel({ aberto: false, papel: null, modo: 'criar' })}
                disabled={salvandoPapel}
              >
                Cancelar
              </button>
              <button 
                className="btn btn-primario"
                onClick={salvarPapel}
                disabled={salvandoPapel}
              >
                {salvandoPapel ? 'Salvando...' : (modalPapel.modo === 'criar' ? 'Criar' : 'Salvar')}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal de Permissões */}
      {modalPermissoes.aberto && (
        <div className="modal-overlay">
          <div className="modal-content modal-permissoes">
            <div className="modal-header">
              <h3>🔍 Permissões do Papel: {modalPermissoes.papel?.nome}</h3>
              <button 
                className="btn-fechar"
                onClick={() => setModalPermissoes({ aberto: false, papel: null })}
              >
                ✖️
              </button>
            </div>
            <div className="modal-body">
              <div className="info-papel-modal">
                <p><strong>Descrição:</strong> {modalPermissoes.papel?.descricao}</p>
                <p><strong>Status:</strong> 
                  <span className={`badge ${modalPermissoes.papel?.ativo ? 'badge-sucesso' : 'badge-erro'}`}>
                    {modalPermissoes.papel?.ativo ? 'Ativo' : 'Inativo'}
                  </span>
                </p>
              </div>
              
              {modalPermissoes.papel?.permissoes && modalPermissoes.papel.permissoes.length > 0 ? (
                <div className="permissoes-detalhes">
                  {Object.entries(agruparPermissoesPorRecurso(modalPermissoes.papel.permissoes)).map(([recurso, permissoesRecurso]) => (
                    <div key={recurso} className="grupo-permissoes-modal">
                      <h4>📋 {recurso}</h4>
                      <div className="lista-permissoes-modal">
                        {permissoesRecurso.map(permissao => (
                          <span key={permissao} className="badge badge-permissao-modal">
                            {permissao}
                          </span>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="sem-permissoes">
                  <span className="icone-vazio">🚫</span>
                  <p>Este papel não possui permissões atribuídas.</p>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Modal de Confirmação */}
      <ModalConfirmacao 
        aberto={modalConfirmacao.aberto}
        titulo="Confirmar Exclusão"
        mensagem={`Tem certeza que deseja excluir o papel "${modalConfirmacao.papel?.nome}"? Esta ação não pode ser desfeita.`}
        onConfirmar={excluirPapel}
        onCancelar={() => setModalConfirmacao({ aberto: false, papel: null })}
      />
    </div>
  );
};

export default Papeis;