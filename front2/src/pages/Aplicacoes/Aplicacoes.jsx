import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import Paginacao from '../../components/Comuns/Paginacao';
import ModalConfirmacao from '../../components/Comuns/ModalConfirmacao';
import api from '../../services/api';
import './Aplicacoes.css';

const Aplicacoes = () => {
  const { temPermissao } = useAutenticacao();
  const [aplicacoes, setAplicacoes] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [paginacao, setPaginacao] = useState({
    paginaAtual: 1,
    itensPorPagina: 12,
    totalItens: 0,
    totalPaginas: 0
  });
  const [filtros, setFiltros] = useState({
    busca: '',
    ativo: '',
    ordenarPor: 'nome',
    direcaoOrdenacao: 'asc'
  });
  const [modalAplicacao, setModalAplicacao] = useState({ aberto: false, aplicacao: null, modo: 'criar' });
  const [modalConfirmacao, setModalConfirmacao] = useState({ aberto: false, aplicacao: null });
  const [formulario, setFormulario] = useState({
    nome: '',
    descricao: '',
    url: '',
    icone: '',
    ativo: true,
    versao: '',
    desenvolvedor: ''
  });
  const [errosFormulario, setErrosFormulario] = useState({});
  const [salvandoAplicacao, setSalvandoAplicacao] = useState(false);

  // Carregar aplicações
  const carregarAplicacoes = async (novaPagina = paginacao.paginaAtual) => {
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
      
      const response = await api.get(`/api/aplicacoes?${params}`);
      setAplicacoes(response.data.dados || []);
      setPaginacao(prev => ({
        ...prev,
        paginaAtual: response.data.paginaAtual || novaPagina,
        totalItens: response.data.totalItens || 0,
        totalPaginas: response.data.totalPaginas || 0
      }));
    } catch (erro) {
      console.error('Erro ao carregar aplicações:', erro);
      setAplicacoes([]);
    } finally {
      setCarregando(false);
    }
  };

  useEffect(() => {
    if (temPermissao('Aplicacoes.Listar')) {
      carregarAplicacoes();
    }
  }, [filtros, temPermissao]);

  // Aplicar filtros
  const aplicarFiltros = () => {
    setPaginacao(prev => ({ ...prev, paginaAtual: 1 }));
    carregarAplicacoes(1);
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

  // Abrir modal de aplicação
  const abrirModalAplicacao = (aplicacao = null, modo = 'criar') => {
    if (modo === 'editar' && aplicacao) {
      setFormulario({
        nome: aplicacao.nome || '',
        descricao: aplicacao.descricao || '',
        url: aplicacao.url || '',
        icone: aplicacao.icone || '',
        ativo: aplicacao.ativo !== undefined ? aplicacao.ativo : true,
        versao: aplicacao.versao || '',
        desenvolvedor: aplicacao.desenvolvedor || ''
      });
    } else {
      setFormulario({
        nome: '',
        descricao: '',
        url: '',
        icone: '',
        ativo: true,
        versao: '',
        desenvolvedor: ''
      });
    }
    setErrosFormulario({});
    setModalAplicacao({ aberto: true, aplicacao, modo });
  };

  // Validar formulário
  const validarFormulario = () => {
    const erros = {};
    
    if (!formulario.nome.trim()) erros.nome = 'Nome é obrigatório';
    else if (formulario.nome.length < 2) erros.nome = 'Nome deve ter pelo menos 2 caracteres';
    
    if (!formulario.descricao.trim()) erros.descricao = 'Descrição é obrigatória';
    
    if (formulario.url && !/^https?:\/\/.+/.test(formulario.url)) {
      erros.url = 'URL deve começar com http:// ou https://';
    }
    
    setErrosFormulario(erros);
    return Object.keys(erros).length === 0;
  };

  // Salvar aplicação
  const salvarAplicacao = async () => {
    if (!validarFormulario()) return;
    
    try {
      setSalvandoAplicacao(true);
      const dados = {
        nome: formulario.nome.trim(),
        descricao: formulario.descricao.trim(),
        url: formulario.url.trim(),
        icone: formulario.icone.trim(),
        ativo: formulario.ativo,
        versao: formulario.versao.trim(),
        desenvolvedor: formulario.desenvolvedor.trim()
      };
      
      if (modalAplicacao.modo === 'criar') {
        await api.post('/api/aplicacoes', dados);
      } else {
        await api.put(`/api/aplicacoes/${modalAplicacao.aplicacao.id}`, dados);
      }
      
      setModalAplicacao({ aberto: false, aplicacao: null, modo: 'criar' });
      carregarAplicacoes();
    } catch (erro) {
      console.error('Erro ao salvar aplicação:', erro);
      if (erro.response?.data?.errors) {
        const errosApi = {};
        Object.keys(erro.response.data.errors).forEach(campo => {
          errosApi[campo] = erro.response.data.errors[campo][0];
        });
        setErrosFormulario(errosApi);
      }
    } finally {
      setSalvandoAplicacao(false);
    }
  };

  // Confirmar exclusão
  const confirmarExclusao = (aplicacao) => {
    setModalConfirmacao({ aberto: true, aplicacao });
  };

  // Excluir aplicação
  const excluirAplicacao = async () => {
    try {
      await api.delete(`/api/aplicacoes/${modalConfirmacao.aplicacao.id}`);
      setModalConfirmacao({ aberto: false, aplicacao: null });
      carregarAplicacoes();
    } catch (erro) {
      console.error('Erro ao excluir aplicação:', erro);
    }
  };

  // Alternar status da aplicação
  const alternarStatus = async (aplicacao) => {
    try {
      await api.put(`/api/aplicacoes/${aplicacao.id}`, {
        ...aplicacao,
        ativo: !aplicacao.ativo
      });
      carregarAplicacoes();
    } catch (erro) {
      console.error('Erro ao alterar status:', erro);
    }
  };

  // Renderizar ícone da aplicação
  const renderizarIcone = (aplicacao) => {
    if (aplicacao.icone) {
      if (aplicacao.icone.startsWith('http')) {
        return <img src={aplicacao.icone} alt={aplicacao.nome} className="icone-aplicacao-img" />;
      }
      return <span className="icone-aplicacao-emoji">{aplicacao.icone}</span>;
    }
    return <span className="icone-aplicacao-default">📱</span>;
  };

  if (!temPermissao('Aplicacoes.Listar')) {
    return (
      <div className="pagina-sem-permissao">
        <h2>Acesso Negado</h2>
        <p>Você não tem permissão para visualizar aplicações.</p>
      </div>
    );
  }

  return (
    <div className="pagina-aplicacoes">
      {/* Cabeçalho */}
      <div className="cabecalho-pagina">
        <div className="titulo-secao">
          <h1>📱 Aplicações</h1>
          <p>Gerenciar aplicações e sistemas integrados</p>
        </div>
        {temPermissao('Aplicacoes.Criar') && (
          <button 
            className="btn btn-primario"
            onClick={() => abrirModalAplicacao()}
          >
            ➕ Nova Aplicação
          </button>
        )}
      </div>

      {/* Filtros */}
      <div className="filtros-aplicacoes">
        <div className="card">
          <div className="card-body">
            <div className="row-filtros">
              <div className="campo-filtro">
                <input
                  type="text"
                  placeholder="🔍 Buscar aplicação..."
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
                  <option value="true">Ativas</option>
                  <option value="false">Inativas</option>
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

      {/* Grid de Aplicações */}
      <div className="card">
        <div className="card-body">
          {carregando ? (
            <EstadoCarregamento mensagem="Carregando aplicações..." />
          ) : aplicacoes.length > 0 ? (
            <>
              <div className="grid-aplicacoes">
                {aplicacoes.map(aplicacao => (
                  <div key={aplicacao.id} className={`card-aplicacao ${!aplicacao.ativo ? 'inativa' : ''}`}>
                    <div className="cabecalho-aplicacao">
                      <div className="icone-aplicacao">
                        {renderizarIcone(aplicacao)}
                      </div>
                      <div className="acoes-aplicacao">
                        {aplicacao.url && (
                          <a 
                            href={aplicacao.url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="btn-acao btn-acessar"
                            title="Acessar aplicação"
                          >
                            🔗
                          </a>
                        )}
                        {temPermissao('Aplicacoes.Atualizar') && (
                          <button 
                            className="btn-acao btn-editar"
                            onClick={() => abrirModalAplicacao(aplicacao, 'editar')}
                            title="Editar aplicação"
                          >
                            ✏️
                          </button>
                        )}
                        {temPermissao('Aplicacoes.Atualizar') && (
                          <button 
                            className={`btn-acao ${aplicacao.ativo ? 'btn-desativar' : 'btn-ativar'}`}
                            onClick={() => alternarStatus(aplicacao)}
                            title={aplicacao.ativo ? 'Desativar' : 'Ativar'}
                          >
                            {aplicacao.ativo ? '🚫' : '✅'}
                          </button>
                        )}
                        {temPermissao('Aplicacoes.Excluir') && (
                          <button 
                            className="btn-acao btn-excluir"
                            onClick={() => confirmarExclusao(aplicacao)}
                            title="Excluir aplicação"
                          >
                            🗑️
                          </button>
                        )}
                      </div>
                    </div>
                    
                    <div className="conteudo-aplicacao">
                      <h3 className="nome-aplicacao">{aplicacao.nome}</h3>
                      <p className="descricao-aplicacao">{aplicacao.descricao}</p>
                      
                      <div className="status-aplicacao">
                        <span className={`badge ${aplicacao.ativo ? 'badge-sucesso' : 'badge-erro'}`}>
                          {aplicacao.ativo ? '✅ Ativa' : '❌ Inativa'}
                        </span>
                        {aplicacao.versao && (
                          <span className="badge badge-versao">v{aplicacao.versao}</span>
                        )}
                      </div>
                      
                      {aplicacao.desenvolvedor && (
                        <div className="desenvolvedor-aplicacao">
                          <span className="label-dev">👨‍💻 Desenvolvedor:</span>
                          <span className="nome-dev">{aplicacao.desenvolvedor}</span>
                        </div>
                      )}
                      
                      <div className="info-adicional">
                        <div className="info-item">
                          <span className="label">ID:</span>
                          <span className="valor">{aplicacao.id}</span>
                        </div>
                        {aplicacao.dataCriacao && (
                          <div className="info-item">
                            <span className="label">Criado:</span>
                            <span className="valor">
                              {new Date(aplicacao.dataCriacao).toLocaleDateString('pt-BR')}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
              
              <Paginacao 
                paginaAtual={paginacao.paginaAtual}
                totalPaginas={paginacao.totalPaginas}
                totalItens={paginacao.totalItens}
                onMudarPagina={(pagina) => carregarAplicacoes(pagina)}
              />
            </>
          ) : (
            <div className="sem-dados">
              <span className="icone-vazio">📱</span>
              <h3>Nenhuma aplicação encontrada</h3>
              <p>Não há aplicações cadastradas ou que correspondam aos filtros aplicados.</p>
              {temPermissao('Aplicacoes.Criar') && (
                <button 
                  className="btn btn-primario"
                  onClick={() => abrirModalAplicacao()}
                >
                  ➕ Cadastrar Primeira Aplicação
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Modal de Aplicação */}
      {modalAplicacao.aberto && (
        <div className="modal-overlay">
          <div className="modal-content modal-aplicacao">
            <div className="modal-header">
              <h3>
                {modalAplicacao.modo === 'criar' ? '➕ Nova Aplicação' : '✏️ Editar Aplicação'}
              </h3>
              <button 
                className="btn-fechar"
                onClick={() => setModalAplicacao({ aberto: false, aplicacao: null, modo: 'criar' })}
              >
                ✖️
              </button>
            </div>
            <div className="modal-body">
              <div className="form-row">
                <div className="campo-form">
                  <label>Nome da Aplicação *</label>
                  <input
                    type="text"
                    value={formulario.nome}
                    onChange={(e) => setFormulario(prev => ({ ...prev, nome: e.target.value }))}
                    className={errosFormulario.nome ? 'input-erro' : ''}
                    placeholder="Digite o nome da aplicação"
                  />
                  {errosFormulario.nome && <span className="erro-campo">{errosFormulario.nome}</span>}
                </div>
                <div className="campo-form">
                  <label>Versão</label>
                  <input
                    type="text"
                    value={formulario.versao}
                    onChange={(e) => setFormulario(prev => ({ ...prev, versao: e.target.value }))}
                    placeholder="1.0.0"
                  />
                </div>
              </div>
              
              <div className="campo-form">
                <label>Descrição *</label>
                <textarea
                  value={formulario.descricao}
                  onChange={(e) => setFormulario(prev => ({ ...prev, descricao: e.target.value }))}
                  className={errosFormulario.descricao ? 'input-erro' : ''}
                  placeholder="Descreva o propósito e funcionalidades da aplicação"
                  rows={3}
                />
                {errosFormulario.descricao && <span className="erro-campo">{errosFormulario.descricao}</span>}
              </div>
              
              <div className="form-row">
                <div className="campo-form">
                  <label>URL de Acesso</label>
                  <input
                    type="url"
                    value={formulario.url}
                    onChange={(e) => setFormulario(prev => ({ ...prev, url: e.target.value }))}
                    className={errosFormulario.url ? 'input-erro' : ''}
                    placeholder="https://exemplo.com"
                  />
                  {errosFormulario.url && <span className="erro-campo">{errosFormulario.url}</span>}
                </div>
                <div className="campo-form">
                  <label>Desenvolvedor</label>
                  <input
                    type="text"
                    value={formulario.desenvolvedor}
                    onChange={(e) => setFormulario(prev => ({ ...prev, desenvolvedor: e.target.value }))}
                    placeholder="Nome do desenvolvedor ou equipe"
                  />
                </div>
              </div>
              
              <div className="campo-form">
                <label>Ícone</label>
                <div className="campo-icone">
                  <input
                    type="text"
                    value={formulario.icone}
                    onChange={(e) => setFormulario(prev => ({ ...prev, icone: e.target.value }))}
                    placeholder="📱 (emoji) ou URL da imagem"
                  />
                  <div className="preview-icone">
                    {formulario.icone ? (
                      formulario.icone.startsWith('http') ? (
                        <img src={formulario.icone} alt="Preview" className="preview-img" />
                      ) : (
                        <span className="preview-emoji">{formulario.icone}</span>
                      )
                    ) : (
                      <span className="preview-placeholder">📱</span>
                    )}
                  </div>
                </div>
                <small className="ajuda-campo">
                  Use um emoji ou URL de uma imagem para representar a aplicação
                </small>
              </div>
              
              <div className="campo-form">
                <label className="checkbox-ativo">
                  <input
                    type="checkbox"
                    checked={formulario.ativo}
                    onChange={(e) => setFormulario(prev => ({ ...prev, ativo: e.target.checked }))}
                  />
                  Aplicação ativa
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <button 
                className="btn btn-secundario"
                onClick={() => setModalAplicacao({ aberto: false, aplicacao: null, modo: 'criar' })}
                disabled={salvandoAplicacao}
              >
                Cancelar
              </button>
              <button 
                className="btn btn-primario"
                onClick={salvarAplicacao}
                disabled={salvandoAplicacao}
              >
                {salvandoAplicacao ? 'Salvando...' : (modalAplicacao.modo === 'criar' ? 'Criar' : 'Salvar')}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal de Confirmação */}
      <ModalConfirmacao 
        aberto={modalConfirmacao.aberto}
        titulo="Confirmar Exclusão"
        mensagem={`Tem certeza que deseja excluir a aplicação "${modalConfirmacao.aplicacao?.nome}"? Esta ação não pode ser desfeita.`}
        onConfirmar={excluirAplicacao}
        onCancelar={() => setModalConfirmacao({ aberto: false, aplicacao: null })}
      />
    </div>
  );
};

export default Aplicacoes;