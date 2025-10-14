import React from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';

const Sidebar = ({ estaAberta, aoAlternar }) => {
  const { usuario, temPermissao } = useAutenticacao();
  const localizacao = useLocation();

  const itensMenu = [
    {
      titulo: 'Dashboard',
      icone: '📊',
      caminho: '/dashboard',
      permissao: null // Sempre visível
    },
    {
      titulo: 'Usuários',
      icone: '👥',
      caminho: '/usuarios',
      permissao: 'Usuarios.Listar'
    },
    {
      titulo: 'Papéis',
      icone: '🎭',
      caminho: '/papeis',
      permissao: 'Papeis.Listar'
    },
    {
      titulo: 'Permissões',
      icone: '🔐',
      caminho: '/permissoes',
      permissao: 'Permissoes.Listar'
    },
    {
      titulo: 'Aplicações',
      icone: '📱',
      caminho: '/aplicacoes',
      permissao: 'Aplicacoes.Listar'
    },
    {
      titulo: 'Grupos',
      icone: '👨‍👩‍👧‍👦',
      caminho: '/grupos',
      permissao: 'Grupos.Listar'
    },
    {
      titulo: 'Auditoria',
      icone: '📋',
      caminho: '/auditoria',
      permissao: 'Auditoria.Visualizar'
    },
    {
      titulo: 'Configurações',
      icone: '⚙️',
      caminho: '/configuracoes',
      permissao: 'Sistema.Configurar'
    }
  ];

  const menuFiltrado = itensMenu.filter(item => 
    !item.permissao || temPermissao(item.permissao)
  );

  const obterIniciais = (nome, sobrenome) => {
    const primeiraLetra = nome ? nome.charAt(0).toUpperCase() : '';
    const segundaLetra = sobrenome ? sobrenome.charAt(0).toUpperCase() : '';
    return primeiraLetra + segundaLetra;
  };

  return (
    <>
      <div className={`sidebar ${estaAberta ? 'aberta' : 'fechada'}`}>
        {/* Header da Sidebar */}
        <div className="sidebar-header">
          <div className="logo-container">
            <div className="logo-icone">G</div>
            {estaAberta && (
              <div className="logo-texto">
                <span className="nome-sistema">Gestus</span>
                <span className="subtitulo-sistema">IAM</span>
              </div>
            )}
          </div>
        </div>

        {/* Informações do usuário */}
        <div className="info-usuario">
          <div className="avatar-usuario">
            {obterIniciais(usuario?.nome, usuario?.sobrenome)}
          </div>
          {estaAberta && (
            <div className="dados-usuario">
              <div className="nome-usuario">
                {usuario?.nomeCompleto || `${usuario?.nome} ${usuario?.sobrenome}`}
              </div>
              <div className="email-usuario">{usuario?.email}</div>
              {usuario?.papeis && usuario.papeis.length > 0 && (
                <div className="papeis-usuario">
                  {usuario.papeis.slice(0, 2).map((papel, index) => (
                    <span key={index} className="badge badge-secundario">
                      {papel}
                    </span>
                  ))}
                  {usuario.papeis.length > 2 && (
                    <span className="badge badge-secundario">
                      +{usuario.papeis.length - 2}
                    </span>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Navegação */}
        <nav className="navegacao">
          <ul className="menu-lista">
            {menuFiltrado.map((item, index) => (
              <li key={index} className="menu-item">
                <NavLink
                  to={item.caminho}
                  className={({ isActive }) => 
                    `menu-link ${isActive ? 'ativo' : ''}`
                  }
                  title={!estaAberta ? item.titulo : ''}
                >
                  <span className="menu-icone">{item.icone}</span>
                  {estaAberta && (
                    <span className="menu-texto">{item.titulo}</span>
                  )}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        {/* Rodapé da Sidebar */}
        <div className="sidebar-footer">
          {estaAberta && (
            <div className="info-sistema">
              <div className="versao-sistema">v1.0.0</div>
              <div className="status-sistema">
                <span className="status-indicador online"></span>
                Sistema Online
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Overlay para mobile */}
      {estaAberta && (
        <div 
          className="sidebar-overlay"
          onClick={aoAlternar}
        />
      )}
    </>
  );
};

export default Sidebar;