import React, { createContext, useContext, useState, useEffect } from 'react';
import api from '../services/api';

const ContextoNotificacoes = createContext();

export const useNotificacoes = () => {
  const contexto = useContext(ContextoNotificacoes);
  if (!contexto) {
    throw new Error('useNotificacoes deve ser usado dentro de ProvedorNotificacoes');
  }
  return contexto;
};

export const ProvedorNotificacoes = ({ children }) => {
  const [notificacoes, setNotificacoes] = useState([]);
  const [carregando, setCarregando] = useState(false);
  const [naoLidas, setNaoLidas] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [totalPaginas, setTotalPaginas] = useState(1);

  // Carregar notificações do usuário
  const carregarNotificacoes = async (paginaAtual = 1) => {
    try {
      setCarregando(true);
      
      // Carregar notificações com paginação
      const response = await api.get(`/api/notificacao?pagina=${paginaAtual}&itensPorPagina=20`);
      const { dados, totalPaginas: totalPag } = response.data;
      
      if (paginaAtual === 1) {
        setNotificacoes(dados || []);
      } else {
        setNotificacoes(prev => [...prev, ...(dados || [])]);
      }
      
      setTotalPaginas(totalPag || 1);
      setPagina(paginaAtual);
      
      // Carregar contador de não lidas separadamente
      await carregarContadorNaoLidas();
      
    } catch (erro) {
      console.error('Erro ao carregar notificações:', erro);
      // Em caso de erro, apenas limpar os dados
      if (paginaAtual === 1) {
        setNotificacoes([]);
        setNaoLidas(0);
      }
    } finally {
      setCarregando(false);
    }
  };

  // Carregar contador de não lidas
  const carregarContadorNaoLidas = async () => {
    try {
      const response = await api.get('/api/notificacao/contador');
      setNaoLidas(response.data || 0);
    } catch (erro) {
      console.error('Erro ao carregar contador de notificações não lidas:', erro);
    }
  };

  // Marcar notificação como lida
  const marcarComoLida = async (id) => {
    try {
      await api.put(`/api/notificacao/${id}/marcar-lida`);
      
      setNotificacoes(prev => 
        prev.map(n => 
          n.id === id 
            ? { ...n, lida: true, dataLeitura: new Date().toISOString() }
            : n
        )
      );
      
      setNaoLidas(prev => Math.max(0, prev - 1));
      
    } catch (erro) {
      console.error('Erro ao marcar notificação como lida:', erro);
      // Marcar localmente mesmo se a API falhar
      setNotificacoes(prev => 
        prev.map(n => 
          n.id === id 
            ? { ...n, lida: true, dataLeitura: new Date().toISOString() }
            : n
        )
      );
      setNaoLidas(prev => Math.max(0, prev - 1));
    }
  };

  // Marcar todas como lidas
  const marcarTodasComoLidas = async () => {
    try {
      await api.put('/api/notificacao/marcar-todas-lidas');
      
      setNotificacoes(prev => 
        prev.map(n => ({ 
          ...n, 
          lida: true, 
          dataLeitura: n.lida ? n.dataLeitura : new Date().toISOString() 
        }))
      );
      
      setNaoLidas(0);
      
    } catch (erro) {
      console.error('Erro ao marcar todas notificações como lidas:', erro);
      // Marcar localmente mesmo se a API falhar
      setNotificacoes(prev => 
        prev.map(n => ({ 
          ...n, 
          lida: true, 
          dataLeitura: n.lida ? n.dataLeitura : new Date().toISOString() 
        }))
      );
      setNaoLidas(0);
    }
  };

  // Excluir notificação
  const excluirNotificacao = async (id) => {
    try {
      await api.delete(`/api/notificacao/${id}`);
      
      const notificacao = notificacoes.find(n => n.id === id);
      setNotificacoes(prev => prev.filter(n => n.id !== id));
      
      if (notificacao && !notificacao.lida) {
        setNaoLidas(prev => Math.max(0, prev - 1));
      }
      
    } catch (erro) {
      console.error('Erro ao excluir notificação:', erro);
      // Excluir localmente mesmo se a API falhar
      const notificacao = notificacoes.find(n => n.id === id);
      setNotificacoes(prev => prev.filter(n => n.id !== id));
      
      if (notificacao && !notificacao.lida) {
        setNaoLidas(prev => Math.max(0, prev - 1));
      }
    }
  };

  // Adicionar nova notificação (para quando receber via WebSocket ou polling)
  const adicionarNotificacao = (novaNotificacao) => {
    setNotificacoes(prev => [novaNotificacao, ...prev]);
    if (!novaNotificacao.lida) {
      setNaoLidas(prev => prev + 1);
    }
  };


  // Carregar notificações ao inicializar
  useEffect(() => {
    carregarNotificacoes();
    
    // Configurar polling para novas notificações (a cada 30 segundos)
    const interval = setInterval(() => {
      carregarContadorNaoLidas();
      // Recarregar notificações apenas se estiver na primeira página
      if (pagina === 1) {
        carregarNotificacoes(1);
      }
    }, 30000);
    
    return () => clearInterval(interval);
  }, [pagina]);

  // Carregar mais notificações (para paginação)
  const carregarMais = async () => {
    if (pagina < totalPaginas && !carregando) {
      await carregarNotificacoes(pagina + 1);
    }
  };

  const valor = {
    notificacoes,
    naoLidas,
    carregando,
    pagina,
    totalPaginas,
    carregarNotificacoes,
    carregarMais,
    marcarComoLida,
    marcarTodasComoLidas,
    excluirNotificacao,
    adicionarNotificacao,
    carregarContadorNaoLidas
  };

  return (
    <ContextoNotificacoes.Provider value={valor}>
      {children}
    </ContextoNotificacoes.Provider>
  );
};