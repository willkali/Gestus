import React, { useState, useEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import { useNotificacoes } from '../../contexts/ContextoNotificacoes';
import DropdownNotificacoes from '../Notificacoes/DropdownNotificacoes';

const Header = ({ sidebarAberta, aoAlternarSidebar }) => {
  const { usuario, sair } = useAutenticacao();
  const { naoLidas } = useNotificacoes();
  const localizacao = useLocation();
  const navegar = useNavigate();
  const menuRef = useRef(null);
  const notificacoesRef = useRef(null);
  const [menuUsuarioAberto, setMenuUsuarioAberto] = useState(false);
  const [notificacoesAbertas, setNotificacoesAbertas] = useState(false);

  // Fechar menus quando clicar fora
  useEffect(() => {
    const handleClickFora = (evento) => {
      if (menuRef.current && !menuRef.current.contains(evento.target)) {
        setMenuUsuarioAberto(false);
      }
      if (notificacoesRef.current && !notificacoesRef.current.contains(evento.target)) {
        setNotificacoesAbertas(false);
      }
    };

    document.addEventListener('mousedown', handleClickFora);
    return () => document.removeEventListener('mousedown', handleClickFora);
  }, []);

  // Fechar menus ao navegar para outra página
  useEffect(() => {
    setMenuUsuarioAberto(false);
    setNotificacoesAbertas(false);
  }, [localizacao]);

  const obterTituloPagina = () => {
    const caminho = localizacao.pathname;
    const mapeamentoTitulos = {
      '/dashboard': 'Dashboard',
      '/usuarios': 'Gestão de Usuários',
      '/papeis': 'Gestão de Papéis',
      '/permissoes': 'Gestão de Permissões',
      '/aplicacoes': 'Gestão de Aplicações',
      '/grupos': 'Gestão de Grupos',
      '/auditoria': 'Auditoria do Sistema',
      '/configuracoes': 'Configurações do Sistema',
      '/perfil': 'Meu Perfil',
      '/preferencias': 'Preferências'
    };

    // Verificar se é uma rota de detalhes (ex: /usuarios/123)
    if (caminho.startsWith('/usuarios/')) {
      return 'Detalhes do Usuário';
    }

    return mapeamentoTitulos[caminho] || 'Gestus IAM';
  };

  const obterIniciais = (nome, sobrenome) => {
    const primeiraLetra = nome ? nome.charAt(0).toUpperCase() : '';
    const segundaLetra = sobrenome ? sobrenome.charAt(0).toUpperCase() : '';
    return primeiraLetra + segundaLetra;
  };

  const formatarDataHora = () => {
    const agora = new Date();
    return agora.toLocaleDateString('pt-BR', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const handleLogout = () => {
    if (window.confirm('Tem certeza que deseja sair do sistema?')) {
      sair();
    }
  };

  const irParaPerfil = () => {
    setMenuUsuarioAberto(false);
    navegar('/perfil');
  };

  const irParaPreferencias = () => {
    setMenuUsuarioAberto(false);
    navegar('/preferencias');
  };

  return (
    <header className="header">
      <div className="header-esquerda">
        <button
          className="btn-toggle-sidebar"
          onClick={aoAlternarSidebar}
          title={sidebarAberta ? 'Fechar menu' : 'Abrir menu'}
        >
          <span className="icone-hamburger">☰</span>
        </button>

        <div className="breadcrumb">
          <h1 className="titulo-pagina">{obterTituloPagina()}</h1>
          <span className="data-hora">{formatarDataHora()}</span>
        </div>
      </div>

      <div className="header-direita">
        <div className="notificacoes" ref={notificacoesRef}>
          <button 
            className="btn-notificacao" 
            title="Notificações"
            onClick={() => setNotificacoesAbertas(!notificacoesAbertas)}
          >
            <span className="icone-notificacao">🔔</span>
            {naoLidas > 0 && (
              <span className="badge-contador">{naoLidas > 99 ? '99+' : naoLidas}</span>
            )}
          </button>
          
          <DropdownNotificacoes 
            aberto={notificacoesAbertas}
            onFechar={() => setNotificacoesAbertas(false)}
          />
        </div>

        <div className="menu-usuario" ref={menuRef}>
          <button
            className="btn-usuario"
            onClick={() => setMenuUsuarioAberto(!menuUsuarioAberto)}
          >
            <div className="avatar-header">
              {obterIniciais(usuario?.nome, usuario?.sobrenome)}
            </div>
            <div className="info-usuario-header">
              <span className="nome-usuario">
                {usuario?.nome || 'Usuário'}
              </span>
              <span className="seta-dropdown">
                {menuUsuarioAberto ? '▲' : '▼'}
              </span>
            </div>
          </button>

          {menuUsuarioAberto && (
            <div className="dropdown-usuario">
              <div className="cabecalho-dropdown">
                <div className="avatar-grande">
                  {obterIniciais(usuario?.nome, usuario?.sobrenome)}
                </div>
                <div className="info-completa">
                  <div className="nome-completo">
                    {usuario?.nomeCompleto || `${usuario?.nome} ${usuario?.sobrenome}`}
                  </div>
                  <div className="email-usuario">{usuario?.email}</div>
                  <div className="status-usuario">
                    <span className="status-indicador online"></span>
                    Online
                  </div>
                </div>
              </div>

              <div className="estatisticas-usuario">
                <div className="stat">
                  <span className="stat-numero">{usuario?.contadorLogins || 0}</span>
                  <span className="stat-label">Logins</span>
                </div>
                <div className="stat">
                  <span className="stat-numero">{usuario?.permissoes?.length || 0}</span>
                  <span className="stat-label">Permissões</span>
                </div>
                <div className="stat">
                  <span className="stat-numero">{usuario?.papeis?.length || 0}</span>
                  <span className="stat-label">Papéis</span>
                </div>
              </div>

              <div className="acoes-dropdown">
                <button className="acao-dropdown" onClick={irParaPerfil}>
                  <span className="acao-icone">👤</span>
                  Meu Perfil
                </button>
                <button className="acao-dropdown" onClick={irParaPreferencias}>
                  <span className="acao-icone">⚙️</span>
                  Preferências
                </button>
                <div className="separador-dropdown"></div>
                <button 
                  className="acao-dropdown acao-logout"
                  onClick={handleLogout}
                >
                  <span className="acao-icone">🚪</span>
                  Sair do Sistema
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default Header;