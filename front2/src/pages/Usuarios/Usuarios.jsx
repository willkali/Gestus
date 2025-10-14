import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import Paginacao from '../../components/Comuns/Paginacao';
import ModalConfirmacao from '../../components/Comuns/ModalConfirmacao';
import api from '../../services/api';
import './Usuarios.css';

const Usuarios = () => {
  const { temPermissao } = useAutenticacao();
  const [usuarios, setUsuarios] = useState([]);
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
    papel: '',
    ordenarPor: 'nome',
    direcaoOrdenacao: 'asc'
  });
  const [papeis, setPapeis] = useState([]);
  const [modalUsuario, setModalUsuario] = useState({ aberto: false, usuario: null, modo: 'criar' });
  const [modalConfirmacao, setModalConfirmacao] = useState({ aberto: false, usuario: null });
  const [formulario, setFormulario] = useState({
    nome: '',
    sobrenome: '',
    email: '',
    senha: '',
    confirmarSenha: '',
    ativo: true,
    papeis: []
  });
  const [errosFormulario, setErrosFormulario] = useState({});
  const [salvandoUsuario, setSalvandoUsuario] = useState(false);

  // Carregar usuários
  const carregarUsuarios = async (novaPagina = paginacao.paginaAtual) => {
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
      if (filtros.papel) params.append('papel', filtros.papel);
      
      const response = await api.get(`/api/usuarios?${params}`);
      setUsuarios(response.data.dados || []);
      setPaginacao(prev => ({
        ...prev,
        paginaAtual: response.data.paginaAtual || novaPagina,
        totalItens: response.data.totalItens || 0,
        totalPaginas: response.data.totalPaginas || 0
      }));
    } catch (erro) {
      console.error('Erro ao carregar usuários:', erro);
      setUsuarios([]);
    } finally {
      setCarregando(false);
    }
  };

  // Carregar papéis
  const carregarPapeis = async () => {
    try {
      const response = await api.get('/api/papeis?pagina=1&itensPorPagina=100');
      setPapeis(response.data.dados || []);
    } catch (erro) {
      console.error('Erro ao carregar papéis:', erro);
    }
  };

  useEffect(() => {
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
    carregarUsuarios(1);
  };

  // Limpar filtros
  const limparFiltros = () => {
    setFiltros({
      busca: '',
      ativo: '',
      papel: '',
      ordenarPor: 'nome',
      direcaoOrdenacao: 'asc'
    });
  };

  // Abrir modal de usuário
  const abrirModalUsuario = (usuario = null, modo = 'criar') => {
    if (modo === 'editar' && usuario) {
      setFormulario({
        nome: usuario.nome || '',
        sobrenome: usuario.sobrenome || '',
        email: usuario.email || '',
        senha: '',
        confirmarSenha: '',
        ativo: usuario.ativo !== undefined ? usuario.ativo : true,
        papeis: usuario.papeis || []
      });
    } else {
      setFormulario({
        nome: '',
        sobrenome: '',
        email: '',
        senha: '',
        confirmarSenha: '',
        ativo: true,
        papeis: []
      });
    }
    setErrosFormulario({});
    setModalUsuario({ aberto: true, usuario, modo });
  };

  // Validar formulário
  const validarFormulario = () => {
    const erros = {};
    
    if (!formulario.nome.trim()) erros.nome = 'Nome é obrigatório';
    if (!formulario.sobrenome.trim()) erros.sobrenome = 'Sobrenome é obrigatório';
    if (!formulario.email.trim()) {
      erros.email = 'Email é obrigatório';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formulario.email)) {
      erros.email = 'Email inválido';
    }
    
    if (modalUsuario.modo === 'criar') {
      if (!formulario.senha) erros.senha = 'Senha é obrigatória';
      else if (formulario.senha.length < 6) erros.senha = 'Senha deve ter pelo menos 6 caracteres';
    }
    
    if (formulario.senha && formulario.senha !== formulario.confirmarSenha) {
      erros.confirmarSenha = 'Senhas não coincidem';
    }
    
    setErrosFormulario(erros);
    return Object.keys(erros).length === 0;
  };

  // Salvar usuário
  const salvarUsuario = async () => {
    if (!validarFormulario()) return;
    
    try {
      setSalvandoUsuario(true);
      const dados = {
        nome: formulario.nome.trim(),
        sobrenome: formulario.sobrenome.trim(),
        email: formulario.email.trim(),
        ativo: formulario.ativo,
        papeis: formulario.papeis
      };
      
      if (formulario.senha) {
        dados.senha = formulario.senha;
      }
      
      if (modalUsuario.modo === 'criar') {
        await api.post('/api/usuarios', dados);
      } else {
        await api.put(`/api/usuarios/${modalUsuario.usuario.id}`, dados);
      }
      
      setModalUsuario({ aberto: false, usuario: null, modo: 'criar' });
      carregarUsuarios();
    } catch (erro) {
      console.error('Erro ao salvar usuário:', erro);
      if (erro.response?.data?.errors) {
        const errosApi = {};
        Object.keys(erro.response.data.errors).forEach(campo => {
          errosApi[campo] = erro.response.data.errors[campo][0];
        });
        setErrosFormulario(errosApi);
      }
    } finally {
      setSalvandoUsuario(false);
    }
  };

  // Confirmar exclusão
  const confirmarExclusao = (usuario) => {
    setModalConfirmacao({ aberto: true, usuario });
  };

  // Excluir usuário
  const excluirUsuario = async () => {
    try {
      await api.delete(`/api/usuarios/${modalConfirmacao.usuario.id}`);
      setModalConfirmacao({ aberto: false, usuario: null });
      carregarUsuarios();
    } catch (erro) {
      console.error('Erro ao excluir usuário:', erro);
    }
  };

  // Alternar status do usuário
  const alternarStatus = async (usuario) => {
    try {
      await api.put(`/api/usuarios/${usuario.id}`, {
        ...usuario,
        ativo: !usuario.ativo
      });
      carregarUsuarios();
    } catch (erro) {
      console.error('Erro ao alterar status:', erro);
    }
  };

  if (!temPermissao('Usuarios.Listar')) {
    return (
      <div className="pagina-sem-permissao">
        <h2>Acesso Negado</h2>
        <p>Você não tem permissão para visualizar usuários.</p>
      </div>
    );
  }

  return (
    <div className="pagina-usuarios">
      {/* Cabeçalho */}
      <div className="cabecalho-pagina">
        <div className="titulo-secao">
          <h1>👥 Usuários</h1>
          <p>Gerenciar usuários do sistema</p>
        </div>
        {temPermissao('Usuarios.Criar') && (
          <button 
            className="btn btn-primario"
            onClick={() => abrirModalUsuario()}
          >
            ➕ Novo Usuário
          </button>
        )}
      </div>

      {/* Filtros */}
      <div className="filtros-usuarios">
        <div className="card">
          <div className="card-body">
            <div className="row-filtros">
              <div className="campo-filtro">
                <input
                  type="text"
                  placeholder="🔍 Buscar por nome ou email..."
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
              <div className="campo-filtro">
                <select 
                  value={filtros.papel}
                  onChange={(e) => setFiltros(prev => ({ ...prev, papel: e.target.value }))}
                  className="select-filtro"
                >
                  <option value="">Todos os papéis</option>
                  {papeis.map(papel => (
                    <option key={papel.id} value={papel.nome}>{papel.nome}</option>
                  ))}
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

      {/* Lista de Usuários */}
      <div className="card">
        <div className="card-body">
          {carregando ? (
            <EstadoCarregamento mensagem="Carregando usuários..." />
          ) : usuarios.length > 0 ? (
            <>
              <div className="tabela-usuarios">
                <table>
                  <thead>
                    <tr>
                      <th>Usuário</th>
                      <th>Email</th>
                      <th>Papéis</th>
                      <th>Status</th>
                      <th>Último Login</th>
                      <th>Ações</th>
                    </tr>
                  </thead>
                  <tbody>
                    {usuarios.map(usuario => (
                      <tr key={usuario.id}>
                        <td>
                          <div className="info-usuario">
                            <div className="avatar-usuario">
                              {usuario.nome?.charAt(0)}{usuario.sobrenome?.charAt(0)}
                            </div>
                            <div className="dados-usuario">
                              <div className="nome-usuario">{usuario.nomeCompleto}</div>
                              <div className="id-usuario">ID: {usuario.id}</div>
                            </div>
                          </div>
                        </td>
                        <td>{usuario.email}</td>
                        <td>
                          <div className="papeis-usuario">
                            {usuario.papeis?.slice(0, 2).map((papel, index) => (
                              <span key={index} className="badge badge-papel">{papel}</span>
                            ))}
                            {usuario.papeis?.length > 2 && (
                              <span className="badge badge-mais">+{usuario.papeis.length - 2}</span>
                            )}
                          </div>
                        </td>
                        <td>
                          <span className={`badge ${usuario.ativo ? 'badge-sucesso' : 'badge-erro'}`}>
                            {usuario.ativo ? '✅ Ativo' : '❌ Inativo'}
                          </span>
                        </td>
                        <td>
                          {usuario.ultimoLogin ? 
                            new Date(usuario.ultimoLogin).toLocaleDateString('pt-BR') : 
                            'Nunca'
                          }
                        </td>
                        <td>
                          <div className="acoes-usuario">
                            {temPermissao('Usuarios.Atualizar') && (
                              <button 
                                className="btn-acao btn-editar"
                                onClick={() => abrirModalUsuario(usuario, 'editar')}
                                title="Editar usuário"
                              >
                                ✏️
                              </button>
                            )}
                            {temPermissao('Usuarios.Atualizar') && (
                              <button 
                                className={`btn-acao ${usuario.ativo ? 'btn-desativar' : 'btn-ativar'}`}
                                onClick={() => alternarStatus(usuario)}
                                title={usuario.ativo ? 'Desativar' : 'Ativar'}
                              >
                                {usuario.ativo ? '🚫' : '✅'}
                              </button>
                            )}
                            {temPermissao('Usuarios.Excluir') && (
                              <button 
                                className="btn-acao btn-excluir"
                                onClick={() => confirmarExclusao(usuario)}
                                title="Excluir usuário"
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
                onMudarPagina={(pagina) => carregarUsuarios(pagina)}
              />
            </>
          ) : (
            <div className="sem-dados">
              <span className="icone-vazio">👥</span>
              <h3>Nenhum usuário encontrado</h3>
              <p>Não há usuários cadastrados ou que correspondam aos filtros aplicados.</p>
              {temPermissao('Usuarios.Criar') && (
                <button 
                  className="btn btn-primario"
                  onClick={() => abrirModalUsuario()}
                >
                  ➕ Cadastrar Primeiro Usuário
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Modal de Usuário */}
      {modalUsuario.aberto && (
        <div className="modal-overlay">
          <div className="modal-content modal-usuario">
            <div className="modal-header">
              <h3>
                {modalUsuario.modo === 'criar' ? '➕ Novo Usuário' : '✏️ Editar Usuário'}
              </h3>
              <button 
                className="btn-fechar"
                onClick={() => setModalUsuario({ aberto: false, usuario: null, modo: 'criar' })}
              >
                ✖️
              </button>
            </div>
            <div className="modal-body">
              <div className="form-row">
                <div className="campo-form">
                  <label>Nome *</label>
                  <input
                    type="text"
                    value={formulario.nome}
                    onChange={(e) => setFormulario(prev => ({ ...prev, nome: e.target.value }))}
                    className={errosFormulario.nome ? 'input-erro' : ''}
                    placeholder="Digite o nome"
                  />
                  {errosFormulario.nome && <span className="erro-campo">{errosFormulario.nome}</span>}
                </div>
                <div className="campo-form">
                  <label>Sobrenome *</label>
                  <input
                    type="text"
                    value={formulario.sobrenome}
                    onChange={(e) => setFormulario(prev => ({ ...prev, sobrenome: e.target.value }))}
                    className={errosFormulario.sobrenome ? 'input-erro' : ''}
                    placeholder="Digite o sobrenome"
                  />
                  {errosFormulario.sobrenome && <span className="erro-campo">{errosFormulario.sobrenome}</span>}
                </div>
              </div>
              
              <div className="campo-form">
                <label>Email *</label>
                <input
                  type="email"
                  value={formulario.email}
                  onChange={(e) => setFormulario(prev => ({ ...prev, email: e.target.value }))}
                  className={errosFormulario.email ? 'input-erro' : ''}
                  placeholder="Digite o email"
                />
                {errosFormulario.email && <span className="erro-campo">{errosFormulario.email}</span>}
              </div>
              
              <div className="form-row">
                <div className="campo-form">
                  <label>{modalUsuario.modo === 'criar' ? 'Senha *' : 'Nova Senha (deixe em branco para manter)'}</label>
                  <input
                    type="password"
                    value={formulario.senha}
                    onChange={(e) => setFormulario(prev => ({ ...prev, senha: e.target.value }))}
                    className={errosFormulario.senha ? 'input-erro' : ''}
                    placeholder="Digite a senha"
                  />
                  {errosFormulario.senha && <span className="erro-campo">{errosFormulario.senha}</span>}
                </div>
                <div className="campo-form">
                  <label>Confirmar Senha</label>
                  <input
                    type="password"
                    value={formulario.confirmarSenha}
                    onChange={(e) => setFormulario(prev => ({ ...prev, confirmarSenha: e.target.value }))}
                    className={errosFormulario.confirmarSenha ? 'input-erro' : ''}
                    placeholder="Confirme a senha"
                  />
                  {errosFormulario.confirmarSenha && <span className="erro-campo">{errosFormulario.confirmarSenha}</span>}
                </div>
              </div>
              
              <div className="campo-form">
                <label>Papéis</label>
                <div className="checkboxes-papeis">
                  {papeis.map(papel => (
                    <label key={papel.id} className="checkbox-papel">
                      <input
                        type="checkbox"
                        checked={formulario.papeis.includes(papel.nome)}
                        onChange={(e) => {
                          if (e.target.checked) {
                            setFormulario(prev => ({ 
                              ...prev, 
                              papeis: [...prev.papeis, papel.nome] 
                            }));
                          } else {
                            setFormulario(prev => ({ 
                              ...prev, 
                              papeis: prev.papeis.filter(p => p !== papel.nome) 
                            }));
                          }
                        }}
                      />
                      {papel.nome}
                    </label>
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
                  Usuário ativo
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <button 
                className="btn btn-secundario"
                onClick={() => setModalUsuario({ aberto: false, usuario: null, modo: 'criar' })}
                disabled={salvandoUsuario}
              >
                Cancelar
              </button>
              <button 
                className="btn btn-primario"
                onClick={salvarUsuario}
                disabled={salvandoUsuario}
              >
                {salvandoUsuario ? 'Salvando...' : (modalUsuario.modo === 'criar' ? 'Criar' : 'Salvar')}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal de Confirmação */}
      <ModalConfirmacao 
        aberto={modalConfirmacao.aberto}
        titulo="Confirmar Exclusão"
        mensagem={`Tem certeza que deseja excluir o usuário "${modalConfirmacao.usuario?.nomeCompleto}"?`}
        onConfirmar={excluirUsuario}
        onCancelar={() => setModalConfirmacao({ aberto: false, usuario: null })}
      />
    </div>
  );
};

export default Usuarios;