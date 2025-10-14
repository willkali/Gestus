import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import api from '../../services/api';
import './MeuPerfil.css';

const MeuPerfil = () => {
  const { usuario, atualizarUsuario } = useAutenticacao();
  const [carregando, setCarregando] = useState(false);
  const [salvando, setSalvando] = useState(false);
  const [abaAtiva, setAbaAtiva] = useState('dados');
  const [formulario, setFormulario] = useState({
    nome: '',
    sobrenome: '',
    email: '',
    telefone: '',
    dataNascimento: '',
    biografia: ''
  });
  const [senhaFormulario, setSenhaFormulario] = useState({
    senhaAtual: '',
    novaSenha: '',
    confirmarSenha: ''
  });
  const [preferencias, setPreferencias] = useState({
    tema: 'sistema',
    idioma: 'pt-BR',
    notificacoes: {
      email: true,
      push: false,
      sistema: true
    },
    privacidade: {
      perfilPublico: false,
      mostrarEmail: false,
      mostrarTelefone: false
    }
  });
  const [erros, setErros] = useState({});
  const [sucesso, setSucesso] = useState('');

  // Carregar dados do usuário
  useEffect(() => {
    if (usuario) {
      setFormulario({
        nome: usuario.nome || '',
        sobrenome: usuario.sobrenome || '',
        email: usuario.email || '',
        telefone: usuario.telefone || '',
        dataNascimento: usuario.dataNascimento || '',
        biografia: usuario.biografia || ''
      });
    }
  }, [usuario]);

  // Validar formulário
  const validarFormulario = () => {
    const novosErros = {};

    if (!formulario.nome.trim()) {
      novosErros.nome = 'Nome é obrigatório';
    }

    if (!formulario.sobrenome.trim()) {
      novosErros.sobrenome = 'Sobrenome é obrigatório';
    }

    if (!formulario.email.trim()) {
      novosErros.email = 'Email é obrigatório';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formulario.email)) {
      novosErros.email = 'Email inválido';
    }

    if (formulario.telefone && !/^\(\d{2}\)\s\d{4,5}-\d{4}$/.test(formulario.telefone)) {
      novosErros.telefone = 'Formato: (99) 99999-9999';
    }

    setErros(novosErros);
    return Object.keys(novosErros).length === 0;
  };

  // Validar senha
  const validarSenha = () => {
    const novosErros = {};

    if (!senhaFormulario.senhaAtual) {
      novosErros.senhaAtual = 'Senha atual é obrigatória';
    }

    if (!senhaFormulario.novaSenha) {
      novosErros.novaSenha = 'Nova senha é obrigatória';
    } else if (senhaFormulario.novaSenha.length < 6) {
      novosErros.novaSenha = 'Nova senha deve ter pelo menos 6 caracteres';
    }

    if (senhaFormulario.novaSenha !== senhaFormulario.confirmarSenha) {
      novosErros.confirmarSenha = 'Senhas não coincidem';
    }

    setErros(novosErros);
    return Object.keys(novosErros).length === 0;
  };

  // Salvar dados pessoais
  const salvarDados = async () => {
    if (!validarFormulario()) return;

    try {
      setSalvando(true);
      setErros({});

      const response = await api.put('/api/usuarios/perfil', {
        nome: formulario.nome.trim(),
        sobrenome: formulario.sobrenome.trim(),
        email: formulario.email.trim(),
        telefone: formulario.telefone.trim(),
        dataNascimento: formulario.dataNascimento,
        biografia: formulario.biografia.trim()
      });

      // Atualizar dados no contexto
      atualizarUsuario(response.data);
      
      setSucesso('Dados atualizados com sucesso!');
      setTimeout(() => setSucesso(''), 3000);

    } catch (erro) {
      console.error('Erro ao salvar dados:', erro);
      
      if (erro.response?.data?.errors) {
        setErros(erro.response.data.errors);
      } else {
        setErros({ geral: erro.response?.data?.mensagem || 'Erro ao salvar dados' });
      }
    } finally {
      setSalvando(false);
    }
  };

  // Alterar senha
  const alterarSenha = async () => {
    if (!validarSenha()) return;

    try {
      setSalvando(true);
      setErros({});

      await api.put('/api/usuarios/senha', {
        senhaAtual: senhaFormulario.senhaAtual,
        novaSenha: senhaFormulario.novaSenha
      });

      setSenhaFormulario({
        senhaAtual: '',
        novaSenha: '',
        confirmarSenha: ''
      });

      setSucesso('Senha alterada com sucesso!');
      setTimeout(() => setSucesso(''), 3000);

    } catch (erro) {
      console.error('Erro ao alterar senha:', erro);
      setErros({ 
        senhaAtual: erro.response?.data?.mensagem || 'Erro ao alterar senha'
      });
    } finally {
      setSalvando(false);
    }
  };

  // Salvar preferências
  const salvarPreferencias = async () => {
    try {
      setSalvando(true);
      
      await api.put('/api/usuarios/preferencias', preferencias);
      
      setSucesso('Preferências salvas com sucesso!');
      setTimeout(() => setSucesso(''), 3000);

    } catch (erro) {
      console.error('Erro ao salvar preferências:', erro);
      setErros({ geral: 'Erro ao salvar preferências' });
    } finally {
      setSalvando(false);
    }
  };

  // Formatação de telefone
  const formatarTelefone = (valor) => {
    const numeros = valor.replace(/\D/g, '');
    if (numeros.length <= 11) {
      return numeros.replace(/(\d{2})(\d{4,5})(\d{4})/, '($1) $2-$3');
    }
    return valor;
  };

  const obterIniciais = (nome, sobrenome) => {
    const primeiraLetra = nome ? nome.charAt(0).toUpperCase() : '';
    const segundaLetra = sobrenome ? sobrenome.charAt(0).toUpperCase() : '';
    return primeiraLetra + segundaLetra;
  };

  if (!usuario) {
    return <EstadoCarregamento mensagem="Carregando perfil..." />;
  }

  return (
    <div className="pagina-perfil">
      {/* Cabeçalho */}
      <div className="cabecalho-perfil">
        <div className="info-usuario-perfil">
          <div className="avatar-perfil">
            {obterIniciais(usuario.nome, usuario.sobrenome)}
          </div>
          <div className="dados-perfil">
            <h1>{usuario.nomeCompleto || `${usuario.nome} ${usuario.sobrenome}`}</h1>
            <p className="email-perfil">{usuario.email}</p>
            <div className="badges-perfil">
              {usuario.papeis?.map((papel, index) => (
                <span key={index} className="badge badge-papel">{papel}</span>
              ))}
            </div>
          </div>
        </div>
        
        <div className="estatisticas-perfil">
          <div className="stat-perfil">
            <span className="numero">{usuario.contadorLogins || 0}</span>
            <span className="label">Logins</span>
          </div>
          <div className="stat-perfil">
            <span className="numero">{usuario.permissoes?.length || 0}</span>
            <span className="label">Permissões</span>
          </div>
          <div className="stat-perfil">
            <span className="numero">
              {usuario.ultimoLogin ? 
                new Date(usuario.ultimoLogin).toLocaleDateString('pt-BR') : 
                'Nunca'
              }
            </span>
            <span className="label">Último Login</span>
          </div>
        </div>
      </div>

      {/* Mensagens */}
      {sucesso && (
        <div className="alert alert-sucesso">
          ✅ {sucesso}
        </div>
      )}

      {erros.geral && (
        <div className="alert alert-erro">
          ❌ {erros.geral}
        </div>
      )}

      {/* Abas */}
      <div className="abas-perfil">
        <button 
          className={`aba ${abaAtiva === 'dados' ? 'ativa' : ''}`}
          onClick={() => setAbaAtiva('dados')}
        >
          👤 Dados Pessoais
        </button>
        <button 
          className={`aba ${abaAtiva === 'senha' ? 'ativa' : ''}`}
          onClick={() => setAbaAtiva('senha')}
        >
          🔒 Alterar Senha
        </button>
        <button 
          className={`aba ${abaAtiva === 'preferencias' ? 'ativa' : ''}`}
          onClick={() => setAbaAtiva('preferencias')}
        >
          ⚙️ Preferências
        </button>
      </div>

      {/* Conteúdo das Abas */}
      <div className="conteudo-abas">
        {abaAtiva === 'dados' && (
          <div className="aba-conteudo">
            <div className="card">
              <div className="card-header">
                <h3>📋 Informações Pessoais</h3>
                <p>Gerencie seus dados pessoais</p>
              </div>
              <div className="card-body">
                <div className="form-grid">
                  <div className="campo-form">
                    <label>Nome *</label>
                    <input
                      type="text"
                      value={formulario.nome}
                      onChange={(e) => setFormulario(prev => ({ ...prev, nome: e.target.value }))}
                      className={erros.nome ? 'input-erro' : ''}
                    />
                    {erros.nome && <span className="erro-campo">{erros.nome}</span>}
                  </div>

                  <div className="campo-form">
                    <label>Sobrenome *</label>
                    <input
                      type="text"
                      value={formulario.sobrenome}
                      onChange={(e) => setFormulario(prev => ({ ...prev, sobrenome: e.target.value }))}
                      className={erros.sobrenome ? 'input-erro' : ''}
                    />
                    {erros.sobrenome && <span className="erro-campo">{erros.sobrenome}</span>}
                  </div>

                  <div className="campo-form">
                    <label>Email *</label>
                    <input
                      type="email"
                      value={formulario.email}
                      onChange={(e) => setFormulario(prev => ({ ...prev, email: e.target.value }))}
                      className={erros.email ? 'input-erro' : ''}
                    />
                    {erros.email && <span className="erro-campo">{erros.email}</span>}
                  </div>

                  <div className="campo-form">
                    <label>Telefone</label>
                    <input
                      type="tel"
                      value={formulario.telefone}
                      onChange={(e) => setFormulario(prev => ({ 
                        ...prev, 
                        telefone: formatarTelefone(e.target.value) 
                      }))}
                      placeholder="(99) 99999-9999"
                      className={erros.telefone ? 'input-erro' : ''}
                    />
                    {erros.telefone && <span className="erro-campo">{erros.telefone}</span>}
                  </div>

                  <div className="campo-form">
                    <label>Data de Nascimento</label>
                    <input
                      type="date"
                      value={formulario.dataNascimento}
                      onChange={(e) => setFormulario(prev => ({ ...prev, dataNascimento: e.target.value }))}
                    />
                  </div>

                  <div className="campo-form campo-completo">
                    <label>Biografia</label>
                    <textarea
                      value={formulario.biografia}
                      onChange={(e) => setFormulario(prev => ({ ...prev, biografia: e.target.value }))}
                      rows="4"
                      placeholder="Conte um pouco sobre você..."
                    />
                  </div>
                </div>

                <div className="acoes-form">
                  <button 
                    className="btn btn-primario"
                    onClick={salvarDados}
                    disabled={salvando}
                  >
                    {salvando ? 'Salvando...' : '💾 Salvar Alterações'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {abaAtiva === 'senha' && (
          <div className="aba-conteudo">
            <div className="card">
              <div className="card-header">
                <h3>🔒 Alterar Senha</h3>
                <p>Mantenha sua conta segura com uma senha forte</p>
              </div>
              <div className="card-body">
                <div className="form-grid senha-form">
                  <div className="campo-form">
                    <label>Senha Atual *</label>
                    <input
                      type="password"
                      value={senhaFormulario.senhaAtual}
                      onChange={(e) => setSenhaFormulario(prev => ({ ...prev, senhaAtual: e.target.value }))}
                      className={erros.senhaAtual ? 'input-erro' : ''}
                    />
                    {erros.senhaAtual && <span className="erro-campo">{erros.senhaAtual}</span>}
                  </div>

                  <div className="campo-form">
                    <label>Nova Senha *</label>
                    <input
                      type="password"
                      value={senhaFormulario.novaSenha}
                      onChange={(e) => setSenhaFormulario(prev => ({ ...prev, novaSenha: e.target.value }))}
                      className={erros.novaSenha ? 'input-erro' : ''}
                    />
                    {erros.novaSenha && <span className="erro-campo">{erros.novaSenha}</span>}
                  </div>

                  <div className="campo-form">
                    <label>Confirmar Nova Senha *</label>
                    <input
                      type="password"
                      value={senhaFormulario.confirmarSenha}
                      onChange={(e) => setSenhaFormulario(prev => ({ ...prev, confirmarSenha: e.target.value }))}
                      className={erros.confirmarSenha ? 'input-erro' : ''}
                    />
                    {erros.confirmarSenha && <span className="erro-campo">{erros.confirmarSenha}</span>}
                  </div>
                </div>

                <div className="dicas-senha">
                  <h4>💡 Dicas para uma senha segura:</h4>
                  <ul>
                    <li>Use pelo menos 8 caracteres</li>
                    <li>Combine letras maiúsculas e minúsculas</li>
                    <li>Inclua números e símbolos</li>
                    <li>Evite informações pessoais</li>
                  </ul>
                </div>

                <div className="acoes-form">
                  <button 
                    className="btn btn-primario"
                    onClick={alterarSenha}
                    disabled={salvando}
                  >
                    {salvando ? 'Alterando...' : '🔐 Alterar Senha'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {abaAtiva === 'preferencias' && (
          <div className="aba-conteudo">
            <div className="card">
              <div className="card-header">
                <h3>⚙️ Preferências</h3>
                <p>Personalize sua experiência no sistema</p>
              </div>
              <div className="card-body">
                <div className="secoes-preferencias">
                  <div className="secao-pref">
                    <h4>🎨 Aparência</h4>
                    <div className="campo-form">
                      <label>Tema</label>
                      <select
                        value={preferencias.tema}
                        onChange={(e) => setPreferencias(prev => ({ ...prev, tema: e.target.value }))}
                      >
                        <option value="sistema">Seguir sistema</option>
                        <option value="claro">Claro</option>
                        <option value="escuro">Escuro</option>
                      </select>
                    </div>
                  </div>

                  <div className="secao-pref">
                    <h4>🔔 Notificações</h4>
                    <div className="checkboxes-pref">
                      <label>
                        <input
                          type="checkbox"
                          checked={preferencias.notificacoes.email}
                          onChange={(e) => setPreferencias(prev => ({
                            ...prev,
                            notificacoes: { ...prev.notificacoes, email: e.target.checked }
                          }))}
                        />
                        Notificações por email
                      </label>
                      <label>
                        <input
                          type="checkbox"
                          checked={preferencias.notificacoes.sistema}
                          onChange={(e) => setPreferencias(prev => ({
                            ...prev,
                            notificacoes: { ...prev.notificacoes, sistema: e.target.checked }
                          }))}
                        />
                        Notificações do sistema
                      </label>
                    </div>
                  </div>

                  <div className="secao-pref">
                    <h4>🔒 Privacidade</h4>
                    <div className="checkboxes-pref">
                      <label>
                        <input
                          type="checkbox"
                          checked={preferencias.privacidade.perfilPublico}
                          onChange={(e) => setPreferencias(prev => ({
                            ...prev,
                            privacidade: { ...prev.privacidade, perfilPublico: e.target.checked }
                          }))}
                        />
                        Perfil público
                      </label>
                      <label>
                        <input
                          type="checkbox"
                          checked={preferencias.privacidade.mostrarEmail}
                          onChange={(e) => setPreferencias(prev => ({
                            ...prev,
                            privacidade: { ...prev.privacidade, mostrarEmail: e.target.checked }
                          }))}
                        />
                        Mostrar email para outros usuários
                      </label>
                    </div>
                  </div>
                </div>

                <div className="acoes-form">
                  <button 
                    className="btn btn-primario"
                    onClick={salvarPreferencias}
                    disabled={salvando}
                  >
                    {salvando ? 'Salvando...' : '💾 Salvar Preferências'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default MeuPerfil;