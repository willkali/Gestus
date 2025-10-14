import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import Paginacao from '../../components/Comuns/Paginacao';
import ModalConfirmacao from '../../components/Comuns/ModalConfirmacao';
import api from '../../services/api';
import './Grupos.css';

const Grupos = () => {
  const { temPermissao } = useAutenticacao();
  const [grupos, setGrupos] = useState([]);
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
  const [usuarios, setUsuarios] = useState([]);
  const [papeis, setPapeis] = useState([]);
  const [modalGrupo, setModalGrupo] = useState({ aberto: false, grupo: null, modo: 'criar' });
  const [modalMembros, setModalMembros] = useState({ aberto: false, grupo: null });
  const [modalConfirmacao, setModalConfirmacao] = useState({ aberto: false, grupo: null });
  const [formulario, setFormulario] = useState({
    nome: '',
    descricao: '',
    ativo: true,
    usuarios: [],
    papeis: []
  });
  const [errosFormulario, setErrosFormulario] = useState({});
  const [salvandoGrupo, setSalvandoGrupo] = useState(false);
  const [abaSelecionada, setAbaSelecionada] = useState('usuarios');

  // Carregar grupos
  const carregarGrupos = async (novaPagina = paginacao.paginaAtual) => {
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
      
      const response = await api.get(`/api/grupos?${params}`);
      setGrupos(response.data.dados || []);
      setPaginacao(prev => ({
        ...prev,
        paginaAtual: response.data.paginaAtual || novaPagina,
        totalItens: response.data.totalItens || 0,
        totalPaginas: response.data.totalPaginas || 0
      }));
    } catch (erro) {
      console.error('Erro ao carregar grupos:', erro);
      setGrupos([]);
    } finally {
      setCarregando(false);
    }
  };

  // Carregar usuários disponíveis
  const carregarUsuarios = async () => {
    try {
      const response = await api.get('/api/usuarios?pagina=1&itensPorPagina=1000&ativo=true');
      setUsuarios(response.data.dados || []);
    } catch (erro) {
      console.error('Erro ao carregar usuários:', erro);
    }
  };

  // Carregar papéis disponíveis
  const carregarPapeis = async () => {
    try {
      const response = await api.get('/api/papeis?pagina=1&itensPorPagina=100&ativo=true');
      setPapeis(response.data.dados || []);
    } catch (erro) {
      console.error('Erro ao carregar papéis:', erro);
    }
  };

  useEffect(() => {
    if (temPermissao('Grupos.Listar')) {
      carregarGrupos();
    }
    if (temPermissao('Usuarios.Listar')) {
      carregarUsuarios();
    }
    if (temPermissao('Papeis.Listar')) {
      carregarPapeis();
    }
  }, [filtros, temPermissao]);

  // Aplicar filtros
  const aplicarFiltros = () => {
    setPaginacao(prev => ({ ...prev, paginaAtual: 1 }));
    carregarGrupos(1);
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

  // Abrir modal de grupo
  const abrirModalGrupo = (grupo = null, modo = 'criar') => {
    if (modo === 'editar' && grupo) {
      setFormulario({
        nome: grupo.nome || '',
        descricao: grupo.descricao || '',
        ativo: grupo.ativo !== undefined ? grupo.ativo : true,
        usuarios: grupo.usuarios?.map(u => u.id) || [],
        papeis: grupo.papeis?.map(p => p.id) || []
      });
    } else {
      setFormulario({
        nome: '',
        descricao: '',
        ativo: true,
        usuarios: [],
        papeis: []
      });
    }
    setErrosFormulario({});
    setAbaSelecionada('usuarios');
    setModalGrupo({ aberto: true, grupo, modo });
  };

  // Abrir modal de membros
  const abrirModalMembros = (grupo) => {
    setModalMembros({ aberto: true, grupo });
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

  // Salvar grupo
  const salvarGrupo = async () => {
    if (!validarFormulario()) return;
    
    try {
      setSalvandoGrupo(true);
      const dados = {
        nome: formulario.nome.trim(),
        descricao: formulario.descricao.trim(),
        ativo: formulario.ativo,
        usuarios: formulario.usuarios,
        papeis: formulario.papeis
      };
      
      if (modalGrupo.modo === 'criar') {
        await api.post('/api/grupos', dados);
      } else {
        await api.put(`/api/grupos/${modalGrupo.grupo.id}`, dados);
      }
      
      setModalGrupo({ aberto: false, grupo: null, modo: 'criar' });
      carregarGrupos();
    } catch (erro) {
      console.error('Erro ao salvar grupo:', erro);
      if (erro.response?.data?.errors) {
        const errosApi = {};
        Object.keys(erro.response.data.errors).forEach(campo => {
          errosApi[campo] = erro.response.data.errors[campo][0];
        });
        setErrosFormulario(errosApi);
      }
    } finally {
      setSalvandoGrupo(false);
    }
  };

  // Confirmar exclusão
  const confirmarExclusao = (grupo) => {
    setModalConfirmacao({ aberto: true, grupo });
  };

  // Excluir grupo
  const excluirGrupo = async () => {
    try {
      await api.delete(`/api/grupos/${modalConfirmacao.grupo.id}`);
      setModalConfirmacao({ aberto: false, grupo: null });
      carregarGrupos();
    } catch (erro) {
      console.error('Erro ao excluir grupo:', erro);
    }
  };

  // Alternar status do grupo
  const alternarStatus = async (grupo) => {
    try {
      await api.put(`/api/grupos/${grupo.id}`, {
        ...grupo,
        ativo: !grupo.ativo
      });
      carregarGrupos();
    } catch (erro) {
      console.error('Erro ao alterar status:', erro);
    }
  };

  if (!temPermissao('Grupos.Listar')) {
    return (
      <div className="pagina-sem-permissao">
        <h2>Acesso Negado</h2>
        <p>Você não tem permissão para visualizar grupos.</p>
      </div>
    );
  }

  return (
    <div className="pagina-grupos">
      {/* Cabeçalho */}
      <div className="cabecalho-pagina">
        <div className="titulo-secao">
          <h1>👨‍👩‍👧‍👦 Grupos</h1>
          <p>Gerenciar grupos de usuários e papéis</p>
        </div>
        {temPermissao('Grupos.Criar') && (
          <button 
            className="btn btn-primario"
            onClick={() => abrirModalGrupo()}
          >
            ➕ Novo Grupo
          </button>
        )}
      </div>

      {/* Filtros */}
      <div className="filtros-grupos">
        <div className="card">
          <div className="card-body">
            <div className="row-filtros">
              <div className="campo-filtro">
                <input
                  type="text"
                  placeholder="🔍 Buscar grupo..."
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

      {/* Lista de Grupos */}
      <div className="card">
        <div className="card-body">
          {carregando ? (
            <EstadoCarregamento mensagem="Carregando grupos..." />
          ) : grupos.length > 0 ? (
            <>
              <div className="tabela-grupos">
                <table>
                  <thead>
                    <tr>
                      <th>Grupo</th>
                      <th>Usuários</th>
                      <th>Papéis</th>
                      <th>Status</th>
                      <th>Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {grupos.map(grupo => (
                      <tr key={grupo.id}>
                        <td>
                          <div className="info-grupo">
                            <div className="icone-grupo">
                              👨‍👩‍👧‍👦
                            </div>
                            <div className="dados-grupo">
                              <div className="nome-grupo">{grupo.nome}</div>
                              <div className="descricao-grupo">{grupo.descricao}</div>
                              <div className="id-grupo">ID: {grupo.id}</div>
                            </div>
                          </div>
                        </td>
                        <td>
                          <div className="membros-grupo">
                            <div className="contador-membros">
                              <span className="numero">{grupo.usuarios?.length || 0}</span>
                              <span className="label">usuários</span>
                            </div>
                            {grupo.usuarios?.length > 0 && (
                              <div className="preview-membros">
                                {grupo.usuarios.slice(0, 3).map((usuario, index) => (
                                  <div key={index} className="avatar-membro">
                                    {usuario.nome?.charAt(0)}{usuario.sobrenome?.charAt(0)}
                                  </div>
                                ))}
                                {grupo.usuarios.length > 3 && (
                                  <div className="avatar-mais">+{grupo.usuarios.length - 3}</div>
                                )}
                              </div>
                            )}
                          </div>
                        </td>
                        <td>
                          <div className="papeis-grupo">
                            {grupo.papeis?.slice(0, 2).map((papel, index) => (
                              <span key={index} className="badge badge-papel">{papel.nome}</span>
                            ))}
                            {grupo.papeis?.length > 2 && (
                              <span className="badge badge-mais">+{grupo.papeis.length - 2}</span>
                            )}
                            {(!grupo.papeis || grupo.papeis.length === 0) && (
                              <span className="sem-papeis">Nenhum papel</span>
                            )}
                          </div>
                        </td>
                        <td>
                          <span className={`badge ${grupo.ativo ? 'badge-sucesso' : 'badge-erro'}`}>
                            {grupo.ativo ? '✅ Ativo' : '❌ Inativo'}
                          </span>
                        </td>
                        <td>
                          <div className="acoes-grupo">
                            {temPermissao('Grupos.Visualizar') && (
                              <button 
                                className="btn-acao btn-visualizar"
                                onClick={() => abrirModalMembros(grupo)}
                                title="Ver membros"
                              >
                                👥
                              </button>
                            )}
                            {temPermissao('Grupos.Atualizar') && (
                              <button 
                                className="btn-acao btn-editar"
                                onClick={() => abrirModalGrupo(grupo, 'editar')}
                                title="Editar grupo"
                              >
                                ✏️
                              </button>
                            )}
                            {temPermissao('Grupos.Atualizar') && (
                              <button 
                                className={`btn-acao ${grupo.ativo ? 'btn-desativar' : 'btn-ativar'}`}
                                onClick={() => alternarStatus(grupo)}
                                title={grupo.ativo ? 'Desativar' : 'Ativar'}
                              >
                                {grupo.ativo ? '🚫' : '✅'}
                              </button>
                            )}
                            {temPermissao('Grupos.Excluir') && (
                              <button 
                                className="btn-acao btn-excluir"
                                onClick={() => confirmarExclusao(grupo)}
                                title="Excluir grupo"
                              >
                                🗑️
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              
              <Paginacao 
                paginaAtual={paginacao.paginaAtual}
                totalPaginas={paginacao.totalPaginas}
                totalItens={paginacao.totalItens}
                onMudarPagina={(pagina) => carregarGrupos(pagina)}
              />
            </>
          ) : (
            <div className="sem-dados">
              <span className="icone-vazio">👨‍👩‍👧‍👦</span>
              <h3>Nenhum grupo encontrado</h3>
              <p>Não há grupos cadastrados ou que correspondam aos filtros aplicados.</p>
              {temPermissao('Grupos.Criar') && (
                <button 
                  className="btn btn-primario"
                  onClick={() => abrirModalGrupo()}
                >
                  ➕ Criar Primeiro Grupo
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Modal de Grupo */}
      {modalGrupo.aberto && (
        <div className="modal-overlay">
          <div className="modal-content modal-grupo">
            <div className="modal-header">
              <h3>
                {modalGrupo.modo === 'criar' ? '➕ Novo Grupo' : '✏️ Editar Grupo'}
              </h3>
              <button 
                className="btn-fechar"
                onClick={() => setModalGrupo({ aberto: false, grupo: null, modo: 'criar' })}
              >
                ✖️
              </button>
            </div>
            <div className="modal-body">
              <div className="campo-form">
                <label>Nome do Grupo *</label>
                <input
                  type="text"
                  value={formulario.nome}
                  onChange={(e) => setFormulario(prev => ({ ...prev, nome: e.target.value }))}
                  className={errosFormulario.nome ? 'input-erro' : ''}
                  placeholder="Digite o nome do grupo"
                />
                {errosFormulario.nome && <span className="erro-campo">{errosFormulario.nome}</span>}
              </div>
              
              <div className="campo-form">
                <label>Descrição *</label>
                <textarea
                  value={formulario.descricao}
                  onChange={(e) => setFormulario(prev => ({ ...prev, descricao: e.target.value }))}
                  className={errosFormulario.descricao ? 'input-erro' : ''}
                  placeholder="Descreva o propósito deste grupo"
                  rows={3}
                />
                {errosFormulario.descricao && <span className="erro-campo">{errosFormulario.descricao}</span>}
              </div>
              
              {/* Abas */}
              <div className="abas-grupo">
                <div className="cabecalhos-abas">
                  <button 
                    className={`aba-cabecalho ${abaSelecionada === 'usuarios' ? 'ativa' : ''}`}
                    onClick={() => setAbaSelecionada('usuarios')}
                  >
                    👥 Usuários ({formulario.usuarios.length})
                  </button>
                  <button 
                    className={`aba-cabecalho ${abaSelecionada === 'papeis' ? 'ativa' : ''}`}
                    onClick={() => setAbaSelecionada('papeis')}
                  >
                    🎭 Papéis ({formulario.papeis.length})
                  </button>
                </div>
                
                <div className="conteudo-abas">
                  {abaSelecionada === 'usuarios' && (
                    <div className="aba-usuarios">
                      <div className="lista-selecao">
                        {usuarios.map(usuario => (
                          <label key={usuario.id} className="item-selecao">
                            <input
                              type="checkbox"
                              checked={formulario.usuarios.includes(usuario.id)}
                              onChange={(e) => {
                                if (e.target.checked) {
                                  setFormulario(prev => ({ 
                                    ...prev, 
                                    usuarios: [...prev.usuarios, usuario.id] 
                                  }));
                                } else {
                                  setFormulario(prev => ({ 
                                    ...prev, 
                                    usuarios: prev.usuarios.filter(id => id !== usuario.id) 
                                  }));
                                }
                              }}
                            />
                            <div className="avatar-selecao">
                              {usuario.nome?.charAt(0)}{usuario.sobrenome?.charAt(0)}
                            </div>
                            <div className="info-selecao">
                              <div className="nome-selecao">{usuario.nomeCompleto}</div>
                              <div className="email-selecao">{usuario.email}</div>
                            </div>
                          </label>
                        ))}
                      </div>
                    </div>
                  )}
                  
                  {abaSelecionada === 'papeis' && (
                    <div className="aba-papeis">
                      <div className="lista-selecao">
                        {papeis.map(papel => (
                          <label key={papel.id} className="item-selecao">
                            <input
                              type="checkbox"
                              checked={formulario.papeis.includes(papel.id)}
                              onChange={(e) => {
                                if (e.target.checked) {
                                  setFormulario(prev => ({ 
                                    ...prev, 
                                    papeis: [...prev.papeis, papel.id] 
                                  }));
                                } else {
                                  setFormulario(prev => ({ 
                                    ...prev, 
                                    papeis: prev.papeis.filter(id => id !== papel.id) 
                                  }));
                                }
                              }}
                            />
                            <div className="icone-papel">🎭</div>
                            <div className="info-selecao">
                              <div className="nome-selecao">{papel.nome}</div>
                              <div className="descricao-selecao">{papel.descricao}</div>
                            </div>
                          </label>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              </div>
              
              <div className="campo-form">
                <label className="checkbox-ativo">
                  <input
                    type="checkbox"
                    checked={formulario.ativo}
                    onChange={(e) => setFormulario(prev => ({ ...prev, ativo: e.target.checked }))}
                  />
                  Grupo ativo
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <button 
                className="btn btn-secundario"
                onClick={() => setModalGrupo({ aberto: false, grupo: null, modo: 'criar' })}
                disabled={salvandoGrupo}
              >
                Cancelar
              </button>
              <button 
                className="btn btn-primario"
                onClick={salvarGrupo}
                disabled={salvandoGrupo}
              >
                {salvandoGrupo ? 'Salvando...' : (modalGrupo.modo === 'criar' ? 'Criar' : 'Salvar')}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal de Membros */}
      {modalMembros.aberto && (
        <div className="modal-overlay">
          <div className="modal-content modal-membros">
            <div className="modal-header">
              <h3>👥 Membros do Grupo: {modalMembros.grupo?.nome}</h3>
              <button 
                className="btn-fechar"
                onClick={() => setModalMembros({ aberto: false, grupo: null })}
              >
                ✖️
              </button>
            </div>
            <div className="modal-body">
              <div className="info-grupo-modal">
                <p><strong>Descrição:</strong> {modalMembros.grupo?.descricao}</p>
                <p><strong>Status:</strong> 
                  <span className={`badge ${modalMembros.grupo?.ativo ? 'badge-sucesso' : 'badge-erro'}`}>
                    {modalMembros.grupo?.ativo ? 'Ativo' : 'Inativo'}
                  </span>
                </p>
              </div>
              
              {/* Usuários do Grupo */}
              {modalMembros.grupo?.usuarios && modalMembros.grupo.usuarios.length > 0 ? (
                <div className="secao-membros">
                  <h4>👥 Usuários ({modalMembros.grupo.usuarios.length})</h4>
                  <div className="lista-membros">
                    {modalMembros.grupo.usuarios.map(usuario => (
                      <div key={usuario.id} className="item-membro">
                        <div className="avatar-membro-modal">
                          {usuario.nome?.charAt(0)}{usuario.sobrenome?.charAt(0)}
                        </div>
                        <div className="info-membro">
                          <div className="nome-membro">{usuario.nomeCompleto}</div>
                          <div className="email-membro">{usuario.email}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <div className="sem-membros">
                  <span className="icone-vazio">👥</span>
                  <p>Este grupo não possui usuários.</p>
                </div>
              )}
              
              {/* Papéis do Grupo */}
              {modalMembros.grupo?.papeis && modalMembros.grupo.papeis.length > 0 ? (
                <div className="secao-papeis">
                  <h4>🎭 Papéis ({modalMembros.grupo.papeis.length})</h4>
                  <div className="lista-papeis-modal">
                    {modalMembros.grupo.papeis.map(papel => (
                      <div key={papel.id} className="item-papel-modal">
                        <div className="icone-papel-modal">🎭</div>
                        <div className="info-papel-modal">
                          <div className="nome-papel-modal">{papel.nome}</div>
                          <div className="descricao-papel-modal">{papel.descricao}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <div className="sem-papeis-modal">
                  <span className="icone-vazio">🎭</span>
                  <p>Este grupo não possui papéis atribuídos.</p>
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
        mensagem={`Tem certeza que deseja excluir o grupo "${modalConfirmacao.grupo?.nome}"? Esta ação não pode ser desfeita.`}
        onConfirmar={excluirGrupo}
        onCancelar={() => setModalConfirmacao({ aberto: false, grupo: null })}
      />
    </div>
  );
};

export default Grupos;