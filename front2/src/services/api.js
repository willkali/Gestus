import axios from 'axios';

// Configuração base da API
const api = axios.create({
  baseURL: 'http://localhost:5000', // URL base da API
  timeout: 30000, // 30 segundos de timeout
  headers: {
    'Content-Type': 'application/json'
  }
});

// Interceptor de requisições para adicionar token automaticamente
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Interceptor de respostas para tratamento de erros
api.interceptors.response.use(
  (response) => {
    return response;
  },
  async (error) => {
    const originalRequest = error.config;

    // Verificar se é erro 401 (não autorizado) e não é a primeira tentativa
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        // Tentar renovar o token
        const tokenData = JSON.parse(localStorage.getItem('tokenData') || '{}');
        
        if (tokenData.refreshToken) {
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

          // Atualizar cabeçalho da requisição original
          originalRequest.headers.Authorization = `Bearer ${token}`;

          // Repetir a requisição original
          return api(originalRequest);
        }
      } catch (refreshError) {
        console.error('Erro ao renovar token:', refreshError);
        
        // Limpar dados do localStorage
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        localStorage.removeItem('tokenData');

        // Redirecionar para login
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  }
);

export default api;