import React, { useState } from 'react';
import { useNotificacoes } from '../../contexts/ContextoNotificacoes';
import EstadoCarregamento from '../Comuns/EstadoCarregamento';
import './DropdownNotificacoes.css';

const DropdownNotificacoes = ({ aberto, onFechar }) => {
  const { 
    notificacoes, 
    naoLidas, 
    carregando,
    pagina,
    totalPaginas,
    carregarMais,
    marcarComoLida, 
    marcarTodasComoLidas, 
    excluirNotificacao 
  } = useNotificacoes();
  
  const [filtro, setFiltro] = useState('todas'); // 'todas', 'nao_lidas', 'lidas'

  const formatarTempo = (dataISO) => {
    const agora = new Date();
    const data = new Date(dataISO);
    const diferenca = agora.getTime() - data.getTime();
    
    const segundos = Math.floor(diferenca / 1000);
    const minutos = Math.floor(segundos / 60);
    const horas = Math.floor(minutos / 60);
    const dias = Math.floor(horas / 24);
    
    if (dias > 0) {
      return `${dias} dia${dias > 1 ? 's' : ''} atrás`;
    } else if (horas > 0) {
      return `${horas} hora${horas > 1 ? 's' : ''} atrás`;
    } else if (minutos > 0) {
      return `${minutos} minuto${minutos > 1 ? 's' : ''} atrás`;
    } else {
      return 'Agora mesmo';
    }
  };

  const obterCorPorTipo = (cor) => {
    const cores = {
      'success': '#27ae60',
      'error': '#e74c3c', 
      'warning': '#f39c12',
      'info': '#3498db'
    };
    return cores[cor] || cores.info;
  };

  const notificacoesFiltradas = notificacoes.filter(notificacao => {
    switch (filtro) {
      case 'nao_lidas':
        return !notificacao.lida;
      case 'lidas':
        return notificacao.lida;
      default:
        return true;
    }
  });

  const handleMarcarComoLida = (id, evento) => {
    evento.stopPropagation();
    marcarComoLida(id);
  };

  const handleExcluir = (id, evento) => {
    evento.stopPropagation();
    if (window.confirm('Tem certeza que deseja excluir esta notificação?')) {
      excluirNotificacao(id);
    }
  };

  const handleMarcarTodasComoLidas = () => {
    marcarTodasComoLidas();
  };

  if (!aberto) return null;

  return (
    <div className="dropdown-notificacoes">
      <div className="cabecalho-notificacoes">
        <div className="titulo-notificacoes">
          <h3>🔔 Notificações</h3>
          <span className="contador-nao-lidas">
            {naoLidas} não lida{naoLidas !== 1 ? 's' : ''}
          </span>
        </div>
        
        <div className="acoes-notificacoes">
          {naoLidas > 0 && (
            <button 
              className="btn-acao-notif"
              onClick={handleMarcarTodasComoLidas}
              title="Marcar todas como lidas"
            >
              ✅ Marcar todas como lidas
            </button>
          )}
          <button 
            className="btn-fechar-notif"
            onClick={onFechar}
          >
            ✕
          </button>
        </div>
      </div>

      <div className="filtros-notificacoes">
        <button 
          className={`filtro-btn ${filtro === 'todas' ? 'ativo' : ''}`}
          onClick={() => setFiltro('todas')}
        >
          Todas ({notificacoes.length})
        </button>
        <button 
          className={`filtro-btn ${filtro === 'nao_lidas' ? 'ativo' : ''}`}
          onClick={() => setFiltro('nao_lidas')}
        >
          Não lidas ({naoLidas})
        </button>
        <button 
          className={`filtro-btn ${filtro === 'lidas' ? 'ativo' : ''}`}
          onClick={() => setFiltro('lidas')}
        >
          Lidas ({notificacoes.length - naoLidas})
        </button>
      </div>

      <div className="lista-notificacoes">
        {carregando ? (
          <div className="carregando-notificacoes">
            <EstadoCarregamento 
              tipo="pontos" 
              tamanho="pequeno" 
              mensagem="Carregando notificações..."
            />
          </div>
        ) : notificacoesFiltradas.length === 0 ? (
          <div className="sem-notificacoes">
            <span className="icone-vazio">🔕</span>
            <p>
              {filtro === 'nao_lidas' && 'Nenhuma notificação não lida'}
              {filtro === 'lidas' && 'Nenhuma notificação lida'}
              {filtro === 'todas' && 'Nenhuma notificação'}
            </p>
          </div>
        ) : (
          notificacoesFiltradas.map(notificacao => (
            <div 
              key={notificacao.id}
              className={`item-notificacao ${!notificacao.lida ? 'nao-lida' : 'lida'}`}
              onClick={() => !notificacao.lida && marcarComoLida(notificacao.id)}
            >
              <div className="icone-notificacao" style={{ color: obterCorPorTipo(notificacao.cor) }}>
                {notificacao.icone}
              </div>
              
              <div className="conteudo-notificacao">
                <div className="cabecalho-item">
                  <h4 className="titulo-item">{notificacao.titulo}</h4>
                  <div className="meta-notificacao">
                    <span className="tempo-notificacao">
                      {notificacao.idadeTexto || formatarTempo(notificacao.dataCriacao)}
                    </span>
                    {!notificacao.lida && (
                      <span className="indicador-nao-lida">●</span>
                    )}
                  </div>
                </div>
                
                <p className="mensagem-notificacao">{notificacao.mensagem}</p>
                
                <div className="rodape-item">
                  <span className="origem-notificacao">{notificacao.origem}</span>
                  <div className="acoes-item">
                    {!notificacao.lida && (
                      <button 
                        className="btn-marcar-lida"
                        onClick={(e) => handleMarcarComoLida(notificacao.id, e)}
                        title="Marcar como lida"
                      >
                        👁️
                      </button>
                    )}
                    <button 
                      className="btn-excluir-notif"
                      onClick={(e) => handleExcluir(notificacao.id, e)}
                      title="Excluir notificação"
                    >
                      🗑️
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {pagina < totalPaginas && (
        <div className="rodape-notificacoes">
          <button 
            className="btn-ver-todas"
            onClick={carregarMais}
            disabled={carregando}
          >
            {carregando ? 'Carregando...' : `Carregar mais (${pagina}/${totalPaginas})`}
          </button>
        </div>
      )}
    </div>
  );
};

export default DropdownNotificacoes;