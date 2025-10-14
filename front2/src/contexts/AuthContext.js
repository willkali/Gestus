import React, { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';

const ContextoAutenticacao = createContext();

export const useAutenticacao = () => {
  const contexto = useContext(ContextoAutenticacao);
  if (!contexto) {
    throw new Error('useAutenticacao deve ser usado dentro de ProvedorAutenticacao');
  }
  return contexto;
};

export const ProvedorAutenticacao = ({ children }) => {
  const [usuario, setUsuario] = useState(null);
  const [estaCarregando, setEstaCarregando] = useState(true);
  const [estaAutenticado, setEstaAutenticado] = useState(false);
  const navegar = useNavigate();

  // Carrega os dados do usuário do localStorage na inicialização
  useEffect(() => {
    const carregarDadosUsuario = () => {
      try {
        const token = localStorage.getItem('token');
        const dadosUsuario = localStorage.getItem('usuario');
        
        if (token && dadosUsuario) {
          const usuarioParsed = JSON.parse(dadosUsuario);
          setUsuario(usuarioParsed);
          setEstaAutenticado(true);
          
          // Configurar token no cabeçalho das requisições
          api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
          
          // Verificar se o token não expirou
          const dadosToken = JSON.parse(localStorage.getItem('dadosToken') || '{}');
          if (dadosToken.expiracaoEm) {
            const agora = new Date();
            const expiracao = new Date(dadosToken.expiracaoEm);
            
            if (agora >= expiracao) {
              // Token expirado, tentar renovar
              renovarToken();
            }
          }
        }
      } catch (erro) {
        console.error('Erro ao carregar dados do usuário:', erro);
        sair();
      } finally {
        setEstaCarregando(false);
      }
    };

    carregarDadosUsuario();
  }, []);

  const login = async (email, senha, lembrarLogin = false) => {
    try {
      setIsLoading(true);
      
      const response = await api.post('/api/autenticacao/login', {
        email,
        senha,
        lembrarLogin
      });

      const { token, tipoToken, expiracaoEm, refreshToken, usuario } = response.data;

      // Salvar dados no localStorage
      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(usuario));
      localStorage.setItem('tokenData', JSON.stringify({
        token,
        tipoToken,
        expiracaoEm,
        refreshToken
      }));

      // Atualizar estado
      setUser(usuario);
      setIsAuthenticated(true);
      
      // Configurar token no cabeçalho das requisições
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      // Navegar para dashboard
      navigate('/dashboard');

      return { sucesso: true };
    } catch (error) {
      console.error('Erro no login:', error);
      
      const mensagemErro = error.response?.data?.mensagem || 'Erro interno no servidor';
      const detalhes = error.response?.data?.detalhes || [];
      
      return {
        sucesso: false,
        erro: error.response?.data?.erro || 'ErroLogin',
        mensagem: mensagemErro,
        detalhes
      };
    } finally {
      setIsLoading(false);
    }
  };

  const refreshToken = async () => {
    try {
      const tokenData = JSON.parse(localStorage.getItem('tokenData') || '{}');
      
      if (!tokenData.refreshToken) {
        throw new Error('Refresh token não encontrado');
      }

      const response = await api.post('/api/autenticacao/refresh', {
        refreshToken: tokenData.refreshToken
      });

      const { token, tipoToken, expiracaoEm, refreshToken: newRefreshToken } = response.data;

      // Atualizar dados salvos
      const updatedTokenData = {
        ...tokenData,
        token,
        tipoToken,
        expiracaoEm,
        refreshToken: newRefreshToken || tokenData.refreshToken
      };

      localStorage.setItem('token', token);
      localStorage.setItem('tokenData', JSON.stringify(updatedTokenData));

      // Atualizar cabeçalho das requisições
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      return true;
    } catch (error) {
      console.error('Erro ao renovar token:', error);
      logout();
      return false;
    }
  };

  const logout = () => {
    // Limpar localStorage
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('tokenData');

    // Limpar estado
    setUser(null);
    setIsAuthenticated(false);

    // Remover token do cabeçalho das requisições
    delete api.defaults.headers.common['Authorization'];

    // Navegar para login
    navigate('/login');
  };

  const hasPermission = (permission) => {
    if (!user || !user.permissoes) return false;
    
    // SuperAdmin tem todas as permissões
    if (user.permissoes.includes('*')) return true;
    
    return user.permissoes.includes(permission);
  };

  const hasRole = (role) => {
    if (!user || !user.papeis) return false;
    return user.papeis.includes(role);
  };

  const updateUser = (updatedUserData) => {
    const newUser = { ...user, ...updatedUserData };
    setUser(newUser);
    localStorage.setItem('user', JSON.stringify(newUser));
  };

  const value = {
    user,
    isAuthenticated,
    isLoading,
    login,
    logout,
    refreshToken,
    hasPermission,
    hasRole,
    updateUser
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};