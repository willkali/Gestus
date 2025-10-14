import api from './api';

class UsuarioService {
  
  // Listar usuários com filtros e paginação
  async listarUsuarios(filtros = {}) {
    try {
      const params = new URLSearchParams();
      
      // Adicionar filtros aos parâmetros
      if (filtros.email) params.append('email', filtros.email);
      if (filtros.nome) params.append('nome', filtros.nome);
      if (filtros.ativo !== undefined) params.append('ativo', filtros.ativo);
      if (filtros.dataCriacaoInicio) params.append('dataCriacaoInicio', filtros.dataCriacaoInicio);
      if (filtros.dataCriacaoFim) params.append('dataCriacaoFim', filtros.dataCriacaoFim);
      if (filtros.papeis) params.append('papeis', filtros.papeis.join(','));
      if (filtros.ordenarPor) params.append('ordenarPor', filtros.ordenarPor);
      if (filtros.direcaoOrdenacao) params.append('direcaoOrdenacao', filtros.direcaoOrdenacao);
      if (filtros.pagina) params.append('pagina', filtros.pagina);
      if (filtros.itensPorPagina) params.append('itensPorPagina', filtros.itensPorPagina);

      const response = await api.get(`/api/usuarios?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Erro ao listar usuários:', error);
      throw error;
    }
  }

  // Obter usuário por ID
  async obterUsuario(id) {
    try {
      const response = await api.get(`/api/usuarios/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Erro ao obter usuário ${id}:`, error);
      throw error;
    }
  }

  // Criar novo usuário
  async criarUsuario(dadosUsuario) {
    try {
      const response = await api.post('/api/usuarios', dadosUsuario);
      return response.data;
    } catch (error) {
      console.error('Erro ao criar usuário:', error);
      throw error;
    }
  }

  // Atualizar usuário
  async atualizarUsuario(id, dadosUsuario) {
    try {
      const response = await api.put(`/api/usuarios/${id}`, dadosUsuario);
      return response.data;
    } catch (error) {
      console.error(`Erro ao atualizar usuário ${id}:`, error);
      throw error;
    }
  }

  // Excluir usuário
  async excluirUsuario(id) {
    try {
      const response = await api.delete(`/api/usuarios/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Erro ao excluir usuário ${id}:`, error);
      throw error;
    }
  }

  // Ativar/Desativar usuário
  async alterarStatusUsuario(id, ativo) {
    try {
      const response = await api.patch(`/api/usuarios/${id}/status`, { ativo });
      return response.data;
    } catch (error) {
      console.error(`Erro ao alterar status do usuário ${id}:`, error);
      throw error;
    }
  }

  // Gerenciar papéis do usuário
  async gerenciarPapeisUsuario(id, papeis) {
    try {
      const response = await api.post(`/api/usuarios/${id}/papeis`, { papeis });
      return response.data;
    } catch (error) {
      console.error(`Erro ao gerenciar papéis do usuário ${id}:`, error);
      throw error;
    }
  }

  // Resetar senha do usuário
  async resetarSenhaUsuario(id) {
    try {
      const response = await api.post(`/api/usuarios/${id}/reset-senha`);
      return response.data;
    } catch (error) {
      console.error(`Erro ao resetar senha do usuário ${id}:`, error);
      throw error;
    }
  }
}

// Exportar instância única do serviço
export default new UsuarioService();