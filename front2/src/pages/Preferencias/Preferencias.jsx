import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import api from '../../services/api';
import './Preferencias.css';

const Preferencias = () => {
  const { usuario } = useAutenticacao();
  const [salvando, setSalvando] = useState(false);
  const [sucesso, setSucesso] = useState('');
  const [erro, setErro] = useState('');
  
  const [preferencias, setPreferencias] = useState({
    // Aparência
    tema: 'sistema',
    corAccent: '#3498db',
    densidadeInterface: 'normal',
    animacoes: true,
    
    // Idioma e Região
    idioma: 'pt-BR',
    fusoHorario: 'America/Sao_Paulo',
    formatoData: 'DD/MM/YYYY',
    formatoHora: '24h',
    
    // Notificações
    notificacoes: {
      email: true,
      sistema: true,
      push: false,
      som: true,
      desktop: false
    },
    
    // Tipos de notificações
    tiposNotificacao: {
      loginSucesso: true,
      loginFalha: true,
      alteracaoSenha: true,
      alteracaoDados: true,
      novoUsuario: false,
      exclusaoUsuario: false,
      alteracaoPermissao: true
    },
    
    // Privacidade
    privacidade: {
      perfilPublico: false,
      mostrarEmail: false,
      mostrarTelefone: false,
      permitirBusca: true,
      historicoLogin: true,
      compartilharEstatisticas: false
    },
    
    // Interface
    interface: {
      menuCompacto: false,
      mostrarTooltips: true,
      atalhosTeclado: true,
      autoSalvar: true,
      confirmacaoAcoes: true,
      breadcrumbCompleto: true
    },
    
    // Dados e Backup
    dados: {
      manterHistoricoAcoes: 90, // dias
      exportarDadosPeriodicamente: false,
      frequenciaBackup: 'semanal',
      limparCacheAutomaticamente: true
    }
  });

  // Carregar preferências do usuário
  useEffect(() => {
    carregarPreferencias();
  }, []);

  const carregarPreferencias = async () => {
    try {
      const response = await api.get('/api/usuarios/preferencias');
      if (response.data) {
        setPreferencias(prev => ({ ...prev, ...response.data }));
      }
    } catch (error) {
      console.warn('Não foi possível carregar preferências, usando padrões');
    }
  };

  const salvarPreferencias = async () => {
    try {
      setSalvando(true);
      setErro('');
      
      await api.put('/api/usuarios/preferencias', preferencias);
      
      setSucesso('Preferências salvas com sucesso!');
      setTimeout(() => setSucesso(''), 3000);
      
    } catch (error) {
      console.error('Erro ao salvar preferências:', error);
      setErro('Erro ao salvar preferências. Tente novamente.');
    } finally {
      setSalvando(false);
    }
  };

  const resetarPreferencias = () => {
    if (window.confirm('Tem certeza que deseja restaurar todas as preferências para os valores padrão?')) {
      setPreferencias({
        tema: 'sistema',
        corAccent: '#3498db',
        densidadeInterface: 'normal',
        animacoes: true,
        idioma: 'pt-BR',
        fusoHorario: 'America/Sao_Paulo',
        formatoData: 'DD/MM/YYYY',
        formatoHora: '24h',
        notificacoes: {
          email: true,
          sistema: true,
          push: false,
          som: true,
          desktop: false
        },
        tiposNotificacao: {
          loginSucesso: true,
          loginFalha: true,
          alteracaoSenha: true,
          alteracaoDados: true,
          novoUsuario: false,
          exclusaoUsuario: false,
          alteracaoPermissao: true
        },
        privacidade: {
          perfilPublico: false,
          mostrarEmail: false,
          mostrarTelefone: false,
          permitirBusca: true,
          historicoLogin: true,
          compartilharEstatisticas: false
        },
        interface: {
          menuCompacto: false,
          mostrarTooltips: true,
          atalhosTeclado: true,
          autoSalvar: true,
          confirmacaoAcoes: true,
          breadcrumbCompleto: true
        },
        dados: {
          manterHistoricoAcoes: 90,
          exportarDadosPeriodicamente: false,
          frequenciaBackup: 'semanal',
          limparCacheAutomaticamente: true
        }
      });
    }
  };

  return (
    <div className="pagina-preferencias">
      {/* Cabeçalho */}
      <div className="cabecalho-preferencias">
        <div className="titulo-secao">
          <h1>⚙️ Preferências</h1>
          <p>Personalize sua experiência no sistema</p>
        </div>
        <div className="acoes-principais">
          <button 
            className="btn btn-secundario"
            onClick={resetarPreferencias}
          >
            🔄 Restaurar Padrão
          </button>
          <button 
            className="btn btn-primario"
            onClick={salvarPreferencias}
            disabled={salvando}
          >
            {salvando ? 'Salvando...' : '💾 Salvar Preferências'}
          </button>
        </div>
      </div>

      {/* Mensagens */}
      {sucesso && (
        <div className="alert alert-sucesso">
          ✅ {sucesso}
        </div>
      )}

      {erro && (
        <div className="alert alert-erro">
          ❌ {erro}
        </div>
      )}

      {/* Seções de Preferências */}
      <div className="secoes-preferencias">
        
        {/* Aparência */}
        <div className="secao-pref">
          <div className="cabecalho-secao">
            <h3>🎨 Aparência</h3>
            <p>Personalize a aparência da interface</p>
          </div>
          <div className="conteudo-secao">
            <div className="campo-pref">
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

            <div className="campo-pref">
              <label>Cor de destaque</label>
              <input
                type="color"
                value={preferencias.corAccent}
                onChange={(e) => setPreferencias(prev => ({ ...prev, corAccent: e.target.value }))}
              />
            </div>

            <div className="campo-pref">
              <label>Densidade da interface</label>
              <select
                value={preferencias.densidadeInterface}
                onChange={(e) => setPreferencias(prev => ({ ...prev, densidadeInterface: e.target.value }))}
              >
                <option value="compacta">Compacta</option>
                <option value="normal">Normal</option>
                <option value="espaçosa">Espaçosa</option>
              </select>
            </div>

            <div className="campo-checkbox">
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.animacoes}
                  onChange={(e) => setPreferencias(prev => ({ ...prev, animacoes: e.target.checked }))}
                />
                Habilitar animações
              </label>
            </div>
          </div>
        </div>

        {/* Idioma e Região */}
        <div className="secao-pref">
          <div className="cabecalho-secao">
            <h3>🌍 Idioma e Região</h3>
            <p>Configure idioma e formatos regionais</p>
          </div>
          <div className="conteudo-secao">
            <div className="campo-pref">
              <label>Idioma</label>
              <select
                value={preferencias.idioma}
                onChange={(e) => setPreferencias(prev => ({ ...prev, idioma: e.target.value }))}
              >
                <option value="pt-BR">Português (Brasil)</option>
                <option value="en-US">English (US)</option>
                <option value="es-ES">Español</option>
              </select>
            </div>

            <div className="campo-pref">
              <label>Formato de data</label>
              <select
                value={preferencias.formatoData}
                onChange={(e) => setPreferencias(prev => ({ ...prev, formatoData: e.target.value }))}
              >
                <option value="DD/MM/YYYY">DD/MM/AAAA</option>
                <option value="MM/DD/YYYY">MM/DD/AAAA</option>
                <option value="YYYY-MM-DD">AAAA-MM-DD</option>
              </select>
            </div>

            <div className="campo-pref">
              <label>Formato de hora</label>
              <select
                value={preferencias.formatoHora}
                onChange={(e) => setPreferencias(prev => ({ ...prev, formatoHora: e.target.value }))}
              >
                <option value="24h">24 horas</option>
                <option value="12h">12 horas (AM/PM)</option>
              </select>
            </div>
          </div>
        </div>

        {/* Notificações */}
        <div className="secao-pref">
          <div className="cabecalho-secao">
            <h3>🔔 Notificações</h3>
            <p>Configure como você quer receber notificações</p>
          </div>
          <div className="conteudo-secao">
            <div className="grupo-checkboxes">
              <h4>Canais de notificação</h4>
              <div className="checkboxes-list">
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
                  Notificações no sistema
                </label>
                <label>
                  <input
                    type="checkbox"
                    checked={preferencias.notificacoes.som}
                    onChange={(e) => setPreferencias(prev => ({
                      ...prev,
                      notificacoes: { ...prev.notificacoes, som: e.target.checked }
                    }))}
                  />
                  Som de notificação
                </label>
                <label>
                  <input
                    type="checkbox"
                    checked={preferencias.notificacoes.desktop}
                    onChange={(e) => setPreferencias(prev => ({
                      ...prev,
                      notificacoes: { ...prev.notificacoes, desktop: e.target.checked }
                    }))}
                  />
                  Notificações na área de trabalho
                </label>
              </div>
            </div>

            <div className="grupo-checkboxes">
              <h4>Tipos de notificação</h4>
              <div className="checkboxes-list">
                <label>
                  <input
                    type="checkbox"
                    checked={preferencias.tiposNotificacao.loginSucesso}
                    onChange={(e) => setPreferencias(prev => ({
                      ...prev,
                      tiposNotificacao: { ...prev.tiposNotificacao, loginSucesso: e.target.checked }
                    }))}
                  />
                  Login bem-sucedido
                </label>
                <label>
                  <input
                    type="checkbox"
                    checked={preferencias.tiposNotificacao.loginFalha}
                    onChange={(e) => setPreferencias(prev => ({
                      ...prev,
                      tiposNotificacao: { ...prev.tiposNotificacao, loginFalha: e.target.checked }
                    }))}
                  />
                  Tentativa de login falhada
                </label>
                <label>
                  <input
                    type="checkbox"
                    checked={preferencias.tiposNotificacao.alteracaoSenha}
                    onChange={(e) => setPreferencias(prev => ({
                      ...prev,
                      tiposNotificacao: { ...prev.tiposNotificacao, alteracaoSenha: e.target.checked }
                    }))}
                  />
                  Alteração de senha
                </label>
                <label>
                  <input
                    type="checkbox"
                    checked={preferencias.tiposNotificacao.alteracaoDados}
                    onChange={(e) => setPreferencias(prev => ({
                      ...prev,
                      tiposNotificacao: { ...prev.tiposNotificacao, alteracaoDados: e.target.checked }
                    }))}
                  />
                  Alteração de dados pessoais
                </label>
              </div>
            </div>
          </div>
        </div>

        {/* Privacidade */}
        <div className="secao-pref">
          <div className="cabecalho-secao">
            <h3>🔒 Privacidade</h3>
            <p>Controle a visibilidade dos seus dados</p>
          </div>
          <div className="conteudo-secao">
            <div className="checkboxes-list">
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.privacidade.perfilPublico}
                  onChange={(e) => setPreferencias(prev => ({
                    ...prev,
                    privacidade: { ...prev.privacidade, perfilPublico: e.target.checked }
                  }))}
                />
                Perfil público (visível para outros usuários)
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
                Mostrar email no perfil
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.privacidade.permitirBusca}
                  onChange={(e) => setPreferencias(prev => ({
                    ...prev,
                    privacidade: { ...prev.privacidade, permitirBusca: e.target.checked }
                  }))}
                />
                Permitir que me encontrem em buscas
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.privacidade.historicoLogin}
                  onChange={(e) => setPreferencias(prev => ({
                    ...prev,
                    privacidade: { ...prev.privacidade, historicoLogin: e.target.checked }
                  }))}
                />
                Manter histórico de login
              </label>
            </div>
          </div>
        </div>

        {/* Interface */}
        <div className="secao-pref">
          <div className="cabecalho-secao">
            <h3>💻 Interface</h3>
            <p>Configure o comportamento da interface</p>
          </div>
          <div className="conteudo-secao">
            <div className="checkboxes-list">
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.interface.mostrarTooltips}
                  onChange={(e) => setPreferencias(prev => ({
                    ...prev,
                    interface: { ...prev.interface, mostrarTooltips: e.target.checked }
                  }))}
                />
                Mostrar dicas de ajuda (tooltips)
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.interface.atalhosTeclado}
                  onChange={(e) => setPreferencias(prev => ({
                    ...prev,
                    interface: { ...prev.interface, atalhosTeclado: e.target.checked }
                  }))}
                />
                Habilitar atalhos de teclado
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.interface.confirmacaoAcoes}
                  onChange={(e) => setPreferencias(prev => ({
                    ...prev,
                    interface: { ...prev.interface, confirmacaoAcoes: e.target.checked }
                  }))}
                />
                Pedir confirmação para ações importantes
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.interface.autoSalvar}
                  onChange={(e) => setPreferencias(prev => ({
                    ...prev,
                    interface: { ...prev.interface, autoSalvar: e.target.checked }
                  }))}
                />
                Salvar automaticamente rascunhos
              </label>
            </div>
          </div>
        </div>

        {/* Dados e Backup */}
        <div className="secao-pref">
          <div className="cabecalho-secao">
            <h3>💾 Dados e Backup</h3>
            <p>Gerencie seus dados e backups</p>
          </div>
          <div className="conteudo-secao">
            <div className="campo-pref">
              <label>Manter histórico de ações por (dias)</label>
              <input
                type="number"
                min="1"
                max="365"
                value={preferencias.dados.manterHistoricoAcoes}
                onChange={(e) => setPreferencias(prev => ({
                  ...prev,
                  dados: { ...prev.dados, manterHistoricoAcoes: parseInt(e.target.value) }
                }))}
              />
            </div>

            <div className="campo-pref">
              <label>Frequência de backup</label>
              <select
                value={preferencias.dados.frequenciaBackup}
                onChange={(e) => setPreferencias(prev => ({
                  ...prev,
                  dados: { ...prev.dados, frequenciaBackup: e.target.value }
                }))}
              >
                <option value="nunca">Nunca</option>
                <option value="diario">Diário</option>
                <option value="semanal">Semanal</option>
                <option value="mensal">Mensal</option>
              </select>
            </div>

            <div className="checkboxes-list">
              <label>
                <input
                  type="checkbox"
                  checked={preferencias.dados.limparCacheAutomaticamente}
                  onChange={(e) => setPreferencias(prev => ({
                    ...prev,
                    dados: { ...prev.dados, limparCacheAutomaticamente: e.target.checked }
                  }))}
                />
                Limpar cache automaticamente
              </label>
            </div>
          </div>
        </div>

      </div>
    </div>
  );
};

export default Preferencias;