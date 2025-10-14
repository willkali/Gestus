import CryptoJS from 'crypto-js';

// Chave para criptografia (em produção deve vir de variável de ambiente)
const CRYPTO_KEY = process.env.REACT_APP_CRYPTO_KEY || 'gestus-frontend-key-2024';

/**
 * Utilitários para gerenciar cookies criptografados
 */
class CookieManager {
  
  /**
   * Define um cookie criptografado
   * @param {string} nome - Nome do cookie
   * @param {any} valor - Valor a ser armazenado
   * @param {Object} opcoes - Opções do cookie
   */
  static definir(nome, valor, opcoes = {}) {
    try {
      // Criptografar o valor
      const valorString = JSON.stringify(valor);
      const valorCriptografado = CryptoJS.AES.encrypt(valorString, CRYPTO_KEY).toString();
      
      // Opções padrão
      const opcoesDefault = {
        expires: 7, // 7 dias
        secure: window.location.protocol === 'https:',
        sameSite: 'Lax',
        path: '/'
      };
      
      const opcoesFinais = { ...opcoesDefault, ...opcoes };
      
      // Construir string do cookie
      let cookieString = `${nome}=${encodeURIComponent(valorCriptografado)}`;
      
      if (opcoesFinais.expires) {
        const dataExpiracao = new Date();
        dataExpiracao.setTime(dataExpiracao.getTime() + (opcoesFinais.expires * 24 * 60 * 60 * 1000));
        cookieString += `; expires=${dataExpiracao.toUTCString()}`;
      }
      
      if (opcoesFinais.path) {
        cookieString += `; path=${opcoesFinais.path}`;
      }
      
      if (opcoesFinais.secure) {
        cookieString += `; secure`;
      }
      
      cookieString += `; SameSite=${opcoesFinais.sameSite}`;
      
      document.cookie = cookieString;
      
      return true;
    } catch (erro) {
      console.error('Erro ao definir cookie:', erro);
      return false;
    }
  }
  
  /**
   * Obtém e descriptografa um cookie
   * @param {string} nome - Nome do cookie
   * @returns {any|null} Valor descriptografado ou null
   */
  static obter(nome) {
    try {
      const nomeCompleto = nome + '=';
      const cookies = document.cookie.split(';');
      
      for (let cookie of cookies) {
        cookie = cookie.trim();
        if (cookie.indexOf(nomeCompleto) === 0) {
          const valorCriptografado = decodeURIComponent(cookie.substring(nomeCompleto.length));
          
          // Descriptografar
          const bytes = CryptoJS.AES.decrypt(valorCriptografado, CRYPTO_KEY);
          const valorDescriptografado = bytes.toString(CryptoJS.enc.Utf8);
          
          if (valorDescriptografado) {
            return JSON.parse(valorDescriptografado);
          }
        }
      }
      
      return null;
    } catch (erro) {
      console.error('Erro ao obter cookie:', erro);
      return null;
    }
  }
  
  /**
   * Remove um cookie
   * @param {string} nome - Nome do cookie
   * @param {string} path - Caminho do cookie
   */
  static remover(nome, path = '/') {
    document.cookie = `${nome}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=${path}`;
  }
  
  /**
   * Verifica se um cookie existe
   * @param {string} nome - Nome do cookie
   * @returns {boolean}
   */
  static existe(nome) {
    return this.obter(nome) !== null;
  }
  
  /**
   * Remove todos os cookies do aplicativo
   */
  static limparTodos() {
    const cookies = ['usuario', 'dadosToken', 'permissoes', 'preferencias'];
    cookies.forEach(cookie => this.remover(cookie));
  }
}

/**
 * Gerenciador específico para dados do usuário
 */
export class GerenciadorDadosUsuario {
  
  /**
   * Salva dados completos do usuário
   * @param {Object} usuario - Dados do usuário
   * @param {Object} tokenData - Dados do token
   */
  static salvarDadosCompletos(usuario, tokenData) {
    // Salvar usuário (sem dados sensíveis)
    const dadosUsuarioLimpos = {
      id: usuario.id,
      email: usuario.email,
      nome: usuario.nome,
      sobrenome: usuario.sobrenome,
      nomeCompleto: usuario.nomeCompleto,
      papeis: usuario.papeis,
      permissoes: usuario.permissoes,
      contadorLogins: usuario.contadorLogins,
      ultimoLogin: usuario.ultimoLogin
    };
    
    CookieManager.definir('usuario', dadosUsuarioLimpos, { expires: 7 });
    
    // Salvar dados do token
    CookieManager.definir('dadosToken', {
      token: tokenData.token,
      tipoToken: tokenData.tipoToken,
      expiracaoEm: tokenData.expiracaoEm,
      refreshToken: tokenData.refreshToken
    }, { expires: 7 });
    
    // Salvar permissões separadamente para acesso rápido
    CookieManager.definir('permissoes', usuario.permissoes || [], { expires: 1 });
  }
  
  /**
   * Obtém dados do usuário
   * @returns {Object|null}
   */
  static obterDadosUsuario() {
    return CookieManager.obter('usuario');
  }
  
  /**
   * Obtém dados do token
   * @returns {Object|null}
   */
  static obterDadosToken() {
    return CookieManager.obter('dadosToken');
  }
  
  /**
   * Obtém permissões do usuário
   * @returns {Array}
   */
  static obterPermissoes() {
    return CookieManager.obter('permissoes') || [];
  }
  
  /**
   * Verifica se usuário tem permissão específica
   * @param {string} permissao - Nome da permissão
   * @returns {boolean}
   */
  static temPermissao(permissao) {
    const permissoes = this.obterPermissoes();
    const usuario = this.obterDadosUsuario();
    
    // SuperAdmin tem todas as permissões - verificar por papel ou nome
    if (usuario) {
      if (usuario.papeis && usuario.papeis.includes('SuperAdmin')) return true;
      if (usuario.papeis && usuario.papeis.includes('Super Admin')) return true;
      if (usuario.nome && usuario.nome.toLowerCase().includes('super')) return true;
      if (usuario.isSuperAdmin === true) return true;
    }
    
    // Verificar permissões específicas
    if (permissoes.includes('*')) return true;
    
    return permissoes.includes(permissao);
  }
  
  /**
   * Atualiza dados do usuário mantendo os tokens
   * @param {Object} novosUsuarioDados 
   */
  static atualizarDadosUsuario(novosUsuarioDados) {
    const dadosAtuais = this.obterDadosUsuario();
    if (dadosAtuais) {
      const dadosAtualizados = { ...dadosAtuais, ...novosUsuarioDados };
      CookieManager.definir('usuario', dadosAtualizados, { expires: 7 });
      
      // Atualizar permissões se foram modificadas
      if (novosUsuarioDados.permissoes) {
        CookieManager.definir('permissoes', novosUsuarioDados.permissoes, { expires: 1 });
      }
    }
  }
  
  /**
   * Limpa todos os dados do usuário
   */
  static limparDados() {
    CookieManager.limparTodos();
  }
  
  /**
   * Verifica se o token está próximo do vencimento
   * @returns {boolean}
   */
  static tokenProximoVencimento() {
    const dadosToken = this.obterDadosToken();
    if (!dadosToken || !dadosToken.expiracaoEm) return false;
    
    const agora = new Date();
    const expiracao = new Date(dadosToken.expiracaoEm);
    const diferenca = expiracao.getTime() - agora.getTime();
    
    // Se faltam menos de 10 minutos para expirar
    return diferenca < (10 * 60 * 1000);
  }
}

export default CookieManager;