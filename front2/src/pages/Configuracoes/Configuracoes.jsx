import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import api from '../../services/api';
import './Configuracoes.css';

const Configuracoes = () => {
  const { temPermissao } = useAutenticacao();
  const [carregando, setCarregando] = useState(false);
  const [abaSelecionada, setAbaSelecionada] = useState('sistema');
  const [configuracoesSistema, setConfiguracoesSistema] = useState({
    nomeAplicacao: 'Gestus IAM',
    versao: '1.0.0',
    descricao: 'Sistema de Gestão de Identidade e Acesso',
    logotipo: '',
    tempoSessao: 30,
    permitirRegistro: false,
    verificacaoEmail: true,
    tentativasLogin: 5,
    bloqueioTempo: 15
  });
  
  const [configuracoesEmail, setConfiguracoesEmail] = useState({
    servidor: '',
    porta: 587,
    usuario: '',
    senha: '',
    ssl: true,
    remetente: '',
    nomeRemetente: ''
  });
  
  const [templates, setTemplates] = useState({
    bemVindo: {
      assunto: 'Bem-vindo ao Gestus IAM',
      corpo: 'Olá {nome}, seja bem-vindo ao sistema!'
    },
    recuperarSenha: {
      assunto: 'Recuperação de Senha',
      corpo: 'Clique no link para redefinir sua senha: {link}'
    },
    novoUsuario: {
      assunto: 'Nova Conta Criada',
      corpo: 'Sua conta foi criada. Login: {email}, Senha: {senha}'
    }
  });
  
  const [salvando, setSalvando] = useState(false);
  const [testando, setTestando] = useState(false);
  const [mensagem, setMensagem] = useState({ tipo: '', texto: '' });

  useEffect(() => {
    carregarConfiguracoes();
  }, []);

  const carregarConfiguracoes = async () => {
    try {
      setCarregando(true);
      
      const [sistemaRes, emailRes, templatesRes] = await Promise.allSettled([
        api.get('/api/configuracoes/sistema'),
        api.get('/api/configuracoes/email'),
        api.get('/api/templates')
      ]);
      
      if (sistemaRes.status === 'fulfilled') {
        setConfiguracoesSistema(prev => ({ ...prev, ...sistemaRes.value.data }));
      }
      
      if (emailRes.status === 'fulfilled') {
        setConfiguracoesEmail(prev => ({ ...prev, ...emailRes.value.data }));
      }
      
      if (templatesRes.status === 'fulfilled') {
        setTemplates(prev => ({ ...prev, ...templatesRes.value.data }));
      }
    } catch (erro) {
      console.error('Erro ao carregar configurações:', erro);
    } finally {
      setCarregando(false);
    }
  };

  const salvarConfiguracao = async (tipo) => {
    try {
      setSalvando(true);
      let dados, endpoint;
      
      switch (tipo) {
        case 'sistema':
          dados = configuracoesSistema;
          endpoint = '/api/configuracoes/sistema';
          break;
        case 'email':
          dados = configuracoesEmail;
          endpoint = '/api/configuracoes/email';
          break;
        case 'templates':
          dados = templates;
          endpoint = '/api/templates';
          break;
      }
      
      await api.put(endpoint, dados);
      setMensagem({ tipo: 'sucesso', texto: 'Configurações salvas com sucesso!' });
    } catch (erro) {
      console.error('Erro ao salvar:', erro);
      setMensagem({ tipo: 'erro', texto: 'Erro ao salvar configurações' });
    } finally {
      setSalvando(false);
      setTimeout(() => setMensagem({ tipo: '', texto: '' }), 3000);
    }
  };

  const testarEmail = async () => {
    try {
      setTestando(true);
      await api.post('/api/configuracoes/email/testar', {
        destinatario: 'teste@exemplo.com'
      });
      setMensagem({ tipo: 'sucesso', texto: 'Email de teste enviado com sucesso!' });
    } catch (erro) {
      setMensagem({ tipo: 'erro', texto: 'Erro ao enviar email de teste' });
    } finally {
      setTestando(false);
      setTimeout(() => setMensagem({ tipo: '', texto: '' }), 3000);
    }
  };

  if (!temPermissao('Configuracoes.Visualizar')) {
    return (
      <div className="pagina-sem-permissao">
        <h2>Acesso Negado</h2>
        <p>Você não tem permissão para acessar configurações.</p>
      </div>
    );
  }

  return (
    <div className="pagina-configuracoes">
      <div className="cabecalho-pagina">
        <div className="titulo-secao">
          <h1>⚙️ Configurações</h1>
          <p>Configurar parâmetros do sistema</p>
        </div>
      </div>

      {mensagem.texto && (
        <div className={`mensagem ${mensagem.tipo}`}>
          {mensagem.tipo === 'sucesso' ? '✅' : '❌'} {mensagem.texto}
        </div>
      )}

      <div className="abas-configuracoes">
        <div className="cabecalhos-abas">
          <button 
            className={`aba-cabecalho ${abaSelecionada === 'sistema' ? 'ativa' : ''}`}
            onClick={() => setAbaSelecionada('sistema')}
          >
            🖥️ Sistema
          </button>
          <button 
            className={`aba-cabecalho ${abaSelecionada === 'email' ? 'ativa' : ''}`}
            onClick={() => setAbaSelecionada('email')}
          >
            📧 Email
          </button>
          <button 
            className={`aba-cabecalho ${abaSelecionada === 'templates' ? 'ativa' : ''}`}
            onClick={() => setAbaSelecionada('templates')}
          >
            📄 Templates
          </button>
        </div>

        <div className="conteudo-abas">
          {carregando ? (
            <EstadoCarregamento mensagem="Carregando configurações..." />
          ) : (
            <>
              {/* Aba Sistema */}
              {abaSelecionada === 'sistema' && (
                <div className="aba-sistema">
                  <div className="card">
                    <div className="card-header">
                      <h3>🖥️ Configurações do Sistema</h3>
                    </div>
                    <div className="card-body">
                      <div className="form-row">
                        <div className="campo-form">
                          <label>Nome da Aplicação</label>
                          <input
                            type="text"
                            value={configuracoesSistema.nomeAplicacao}
                            onChange={(e) => setConfiguracoesSistema(prev => ({ 
                              ...prev, 
                              nomeAplicacao: e.target.value 
                            }))}
                            placeholder="Nome do sistema"
                          />
                        </div>
                        <div className="campo-form">
                          <label>Versão</label>
                          <input
                            type="text"
                            value={configuracoesSistema.versao}
                            onChange={(e) => setConfiguracoesSistema(prev => ({ 
                              ...prev, 
                              versao: e.target.value 
                            }))}
                            placeholder="1.0.0"
                          />
                        </div>
                      </div>
                      
                      <div className="campo-form">
                        <label>Descrição</label>
                        <textarea
                          value={configuracoesSistema.descricao}
                          onChange={(e) => setConfiguracoesSistema(prev => ({ 
                            ...prev, 
                            descricao: e.target.value 
                          }))}
                          rows={3}
                          placeholder="Descrição do sistema"
                        />
                      </div>
                      
                      <div className="form-row">
                        <div className="campo-form">
                          <label>Tempo de Sessão (minutos)</label>
                          <input
                            type="number"
                            value={configuracoesSistema.tempoSessao}
                            onChange={(e) => setConfiguracoesSistema(prev => ({ 
                              ...prev, 
                              tempoSessao: parseInt(e.target.value) 
                            }))}
                            min="5"
                            max="480"
                          />
                        </div>
                        <div className="campo-form">
                          <label>Tentativas de Login</label>
                          <input
                            type="number"
                            value={configuracoesSistema.tentativasLogin}
                            onChange={(e) => setConfiguracoesSistema(prev => ({ 
                              ...prev, 
                              tentativasLogin: parseInt(e.target.value) 
                            }))}
                            min="3"
                            max="10"
                          />
                        </div>
                        <div className="campo-form">
                          <label>Tempo de Bloqueio (minutos)</label>
                          <input
                            type="number"
                            value={configuracoesSistema.bloqueioTempo}
                            onChange={(e) => setConfiguracoesSistema(prev => ({ 
                              ...prev, 
                              bloqueioTempo: parseInt(e.target.value) 
                            }))}
                            min="5"
                            max="60"
                          />
                        </div>
                      </div>
                      
                      <div className="configuracoes-checkbox">
                        <label className="checkbox-config">
                          <input
                            type="checkbox"
                            checked={configuracoesSistema.permitirRegistro}
                            onChange={(e) => setConfiguracoesSistema(prev => ({ 
                              ...prev, 
                              permitirRegistro: e.target.checked 
                            }))}
                          />
                          Permitir auto-registro de usuários
                        </label>
                        
                        <label className="checkbox-config">
                          <input
                            type="checkbox"
                            checked={configuracoesSistema.verificacaoEmail}
                            onChange={(e) => setConfiguracoesSistema(prev => ({ 
                              ...prev, 
                              verificacaoEmail: e.target.checked 
                            }))}
                          />
                          Exigir verificação por email
                        </label>
                      </div>
                    </div>
                    <div className="card-footer">
                      <button 
                        className="btn btn-primario"
                        onClick={() => salvarConfiguracao('sistema')}
                        disabled={salvando}
                      >
                        {salvando ? 'Salvando...' : '💾 Salvar Configurações'}
                      </button>
                    </div>
                  </div>
                </div>
              )}

              {/* Aba Email */}
              {abaSelecionada === 'email' && (
                <div className="aba-email">
                  <div className="card">
                    <div className="card-header">
                      <h3>📧 Configurações de Email</h3>
                    </div>
                    <div className="card-body">
                      <div className="form-row">
                        <div className="campo-form">
                          <label>Servidor SMTP</label>
                          <input
                            type="text"
                            value={configuracoesEmail.servidor}
                            onChange={(e) => setConfiguracoesEmail(prev => ({ 
                              ...prev, 
                              servidor: e.target.value 
                            }))}
                            placeholder="smtp.gmail.com"
                          />
                        </div>
                        <div className="campo-form">
                          <label>Porta</label>
                          <input
                            type="number"
                            value={configuracoesEmail.porta}
                            onChange={(e) => setConfiguracoesEmail(prev => ({ 
                              ...prev, 
                              porta: parseInt(e.target.value) 
                            }))}
                            placeholder="587"
                          />
                        </div>
                      </div>
                      
                      <div className="form-row">
                        <div className="campo-form">
                          <label>Usuário</label>
                          <input
                            type="email"
                            value={configuracoesEmail.usuario}
                            onChange={(e) => setConfiguracoesEmail(prev => ({ 
                              ...prev, 
                              usuario: e.target.value 
                            }))}
                            placeholder="seu-email@gmail.com"
                          />
                        </div>
                        <div className="campo-form">
                          <label>Senha</label>
                          <input
                            type="password"
                            value={configuracoesEmail.senha}
                            onChange={(e) => setConfiguracoesEmail(prev => ({ 
                              ...prev, 
                              senha: e.target.value 
                            }))}
                            placeholder="Senha do email"
                          />
                        </div>
                      </div>
                      
                      <div className="form-row">
                        <div className="campo-form">
                          <label>Email Remetente</label>
                          <input
                            type="email"
                            value={configuracoesEmail.remetente}
                            onChange={(e) => setConfiguracoesEmail(prev => ({ 
                              ...prev, 
                              remetente: e.target.value 
                            }))}
                            placeholder="noreply@empresa.com"
                          />
                        </div>
                        <div className="campo-form">
                          <label>Nome do Remetente</label>
                          <input
                            type="text"
                            value={configuracoesEmail.nomeRemetente}
                            onChange={(e) => setConfiguracoesEmail(prev => ({ 
                              ...prev, 
                              nomeRemetente: e.target.value 
                            }))}
                            placeholder="Sistema Gestus"
                          />
                        </div>
                      </div>
                      
                      <div className="configuracoes-checkbox">
                        <label className="checkbox-config">
                          <input
                            type="checkbox"
                            checked={configuracoesEmail.ssl}
                            onChange={(e) => setConfiguracoesEmail(prev => ({ 
                              ...prev, 
                              ssl: e.target.checked 
                            }))}
                          />
                          Usar SSL/TLS
                        </label>
                      </div>
                    </div>
                    <div className="card-footer">
                      <button 
                        className="btn btn-secundario"
                        onClick={testarEmail}
                        disabled={testando}
                      >
                        {testando ? 'Testando...' : '🧪 Testar Email'}
                      </button>
                      <button 
                        className="btn btn-primario"
                        onClick={() => salvarConfiguracao('email')}
                        disabled={salvando}
                      >
                        {salvando ? 'Salvando...' : '💾 Salvar Configurações'}
                      </button>
                    </div>
                  </div>
                </div>
              )}

              {/* Aba Templates */}
              {abaSelecionada === 'templates' && (
                <div className="aba-templates">
                  <div className="templates-grid">
                    {Object.entries(templates).map(([chave, template]) => (
                      <div key={chave} className="card">
                        <div className="card-header">
                          <h3>
                            {chave === 'bemVindo' && '👋 Bem-vindo'}
                            {chave === 'recuperarSenha' && '🔑 Recuperar Senha'}
                            {chave === 'novoUsuario' && '👤 Novo Usuário'}
                          </h3>
                        </div>
                        <div className="card-body">
                          <div className="campo-form">
                            <label>Assunto</label>
                            <input
                              type="text"
                              value={template.assunto}
                              onChange={(e) => setTemplates(prev => ({
                                ...prev,
                                [chave]: {
                                  ...prev[chave],
                                  assunto: e.target.value
                                }
                              }))}
                              placeholder="Assunto do email"
                            />
                          </div>
                          <div className="campo-form">
                            <label>Corpo do Email</label>
                            <textarea
                              value={template.corpo}
                              onChange={(e) => setTemplates(prev => ({
                                ...prev,
                                [chave]: {
                                  ...prev[chave],
                                  corpo: e.target.value
                                }
                              }))}
                              rows={5}
                              placeholder="Conteúdo do template"
                            />
                          </div>
                          <div className="variaveis-disponiveis">
                            <small>
                              <strong>Variáveis disponíveis:</strong> 
                              {chave === 'bemVindo' && '{nome}'}
                              {chave === 'recuperarSenha' && '{nome}, {link}'}
                              {chave === 'novoUsuario' && '{nome}, {email}, {senha}'}
                            </small>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                  
                  <div className="acoes-templates">
                    <button 
                      className="btn btn-primario"
                      onClick={() => salvarConfiguracao('templates')}
                      disabled={salvando}
                    >
                      {salvando ? 'Salvando...' : '💾 Salvar Templates'}
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default Configuracoes;