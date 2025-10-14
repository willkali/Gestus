import React, { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import { GerenciadorDadosUsuario } from '../utils/cookieUtils';

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

  // Carrega os dados do usuário dos cookies criptografados na inicialização
  useEffect(() => {
    const carregarDadosUsuario = () => {
      try {
        const dadosUsuario = GerenciadorDadosUsuario.obterDadosUsuario();
        const dadosToken = GerenciadorDadosUsuario.obterDadosToken();
        
        if (dadosToken?.token && dadosUsuario) {
          setUsuario(dadosUsuario);
          setEstaAutenticado(true);
          
          // Configurar token no cabeçalho das requisições
          api.defaults.headers.common['Authorization'] = `Bearer ${dadosToken.token}`;
          
          // Verificar se o token não expirou
          if (dadosToken.expiracaoEm) {
            const agora = new Date();
            const expiracao = new Date(dadosToken.expiracaoEm);
            
            if (agora >= expiracao) {
              // Token expirado, tentar renovar
              renovarToken();
            } else if (GerenciadorDadosUsuario.tokenProximoVencimento()) {
              // Token próximo do vencimento, renovar preventivamente
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

  const entrar = async (email, senha, lembrarLogin = false) => {
    try {
      setEstaCarregando(true);
      
      const resposta = await api.post('/api/autenticacao/login', {
        email,
        senha,
        lembrarLogin
      });

      const { token, tipoToken, expiracaoEm, refreshToken, usuario } = resposta.data;

      // Salvar dados em cookies criptografados
      GerenciadorDadosUsuario.salvarDadosCompletos(usuario, {
        token,
        tipoToken,
        expiracaoEm,
        refreshToken
      });

      // Atualizar estado
      setUsuario(usuario);
      setEstaAutenticado(true);
      
      // Configurar token no cabeçalho das requisições
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      // Navegar para dashboard
      navegar('/dashboard');

      return { sucesso: true };
    } catch (erro) {
      console.error('Erro no login:', erro);
      
      const mensagemErro = erro.response?.data?.mensagem || 'Erro interno no servidor';
      const detalhes = erro.response?.data?.detalhes || [];
      
      return {
        sucesso: false,
        erro: erro.response?.data?.erro || 'ErroLogin',
        mensagem: mensagemErro,
        detalhes
      };
    } finally {
      setEstaCarregando(false);
    }
  };

  const renovarToken = async () => {
    try {
      const dadosToken = GerenciadorDadosUsuario.obterDadosToken();
      
      if (!dadosToken?.refreshToken) {
        throw new Error('Refresh token não encontrado');
      }

      const resposta = await api.post('/api/autenticacao/refresh', {
        refreshToken: dadosToken.refreshToken
      });

      const { token, tipoToken, expiracaoEm, refreshToken: novoRefreshToken } = resposta.data;

      // Atualizar dados nos cookies
      const dadosUsuario = GerenciadorDadosUsuario.obterDadosUsuario();
      if (dadosUsuario) {
        GerenciadorDadosUsuario.salvarDadosCompletos(dadosUsuario, {
          token,
          tipoToken,
          expiracaoEm,
          refreshToken: novoRefreshToken || dadosToken.refreshToken
        });
      }

      // Atualizar cabeçalho das requisições
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      return true;
    } catch (erro) {
      console.error('Erro ao renovar token:', erro);
      sair();
      return false;
    }
  };

  const sair = () => {
    // Limpar cookies criptografados
    GerenciadorDadosUsuario.limparDados();

    // Limpar estado
    setUsuario(null);
    setEstaAutenticado(false);

    // Remover token do cabeçalho das requisições
    delete api.defaults.headers.common['Authorization'];

    // Navegar para login
    navegar('/login');
  };

  const temPermissao = (permissao) => {
    // Verificar primeiro nos cookies para performance
    if (GerenciadorDadosUsuario.temPermissao(permissao)) return true;
    
    // Fallback para dados em memória
    if (!usuario) return false;
    
    // SuperAdmin tem todas as permissões - verificar por papel ou nome
    if (usuario.papeis && usuario.papeis.includes('SuperAdmin')) return true;
    if (usuario.papeis && usuario.papeis.includes('Super Admin')) return true;
    if (usuario.nome && usuario.nome.toLowerCase().includes('super')) return true;
    if (usuario.isSuperAdmin === true) return true;
    
    // Verificar permissões específicas
    if (!usuario.permissoes) return false;
    if (usuario.permissoes.includes('*')) return true;
    
    return usuario.permissoes.includes(permissao);
  };

  const temPapel = (papel) => {
    if (!usuario || !usuario.papeis) return false;
    return usuario.papeis.includes(papel);
  };

  const atualizarUsuario = (dadosUsuarioAtualizados) => {
    const novoUsuario = { ...usuario, ...dadosUsuarioAtualizados };
    setUsuario(novoUsuario);
    
    // Atualizar nos cookies também
    GerenciadorDadosUsuario.atualizarDadosUsuario(dadosUsuarioAtualizados);
  };

  const valor = {
    usuario,
    estaAutenticado,
    estaCarregando,
    entrar,
    sair,
    renovarToken,
    temPermissao,
    temPapel,
    atualizarUsuario
  };

  return (
    <ContextoAutenticacao.Provider value={valor}>
      {children}
    </ContextoAutenticacao.Provider>
  );
};