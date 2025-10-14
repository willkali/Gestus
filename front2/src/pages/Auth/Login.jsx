import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import { useNavigate, useLocation } from 'react-router-dom';
import './Login.css';

const Login = () => {
  const [formData, setFormData] = useState({
    email: '',
    senha: '',
    lembrarLogin: false
  });
  const [erros, setErros] = useState({});
  const [mensagemErro, setMensagemErro] = useState('');
  const [detalhesErro, setDetalhesErro] = useState([]);
  const [estaCarregando, setEstaCarregando] = useState(false);

  const { entrar, estaAutenticado } = useAutenticacao();
  const navegar = useNavigate();
  const localizacao = useLocation();

  const origem = localizacao.state?.from?.pathname || '/dashboard';

  // Redirecionar se já estiver logado
  useEffect(() => {
    if (estaAutenticado) {
      navegar(origem, { replace: true });
    }
  }, [estaAutenticado, navegar, origem]);

  const validarFormulario = () => {
    const novosErros = {};

    // Validar email
    if (!formData.email) {
      novosErros.email = 'Email é obrigatório';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      novosErros.email = 'Email deve ter formato válido';
    }

    // Validar senha
    if (!formData.senha) {
      novosErros.senha = 'Senha é obrigatória';
    } else if (formData.senha.length < 6) {
      novosErros.senha = 'Senha deve ter pelo menos 6 caracteres';
    }

    setErros(novosErros);
    return Object.keys(novosErros).length === 0;
  };

  const handleInputChange = (evento) => {
    const { name, value, type, checked } = evento.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));

    // Limpar erro do campo quando o usuário começar a digitar
    if (erros[name]) {
      setErros(prev => ({
        ...prev,
        [name]: ''
      }));
    }
  };

  const handleSubmit = async (evento) => {
    evento.preventDefault();
    
    if (!validarFormulario()) {
      return;
    }

    setEstaCarregando(true);
    setMensagemErro('');
    setDetalhesErro([]);

    try {
      const resultado = await entrar(formData.email, formData.senha, formData.lembrarLogin);

      if (!resultado.sucesso) {
        setMensagemErro(resultado.mensagem);
        setDetalhesErro(resultado.detalhes || []);
        
        // Se for erro de email incorreto, focar no campo email
        if (resultado.erro === 'EmailIncorreto') {
          document.getElementById('email')?.focus();
        }
        // Se for erro de senha, focar no campo senha
        else if (resultado.erro === 'SenhaIncorreta' || resultado.erro === 'ContaBloqueada') {
          document.getElementById('senha')?.focus();
        }
      }
    } catch (erro) {
      console.error('Erro inesperado no login:', erro);
      setMensagemErro('Erro inesperado. Tente novamente.');
    } finally {
      setEstaCarregando(false);
    }
  };

  const getTipoAlerta = (tipoErro) => {
    switch (tipoErro) {
      case 'ContaBloqueada':
        return 'alerta-aviso';
      case 'ContaInativa':
        return 'alerta-aviso';
      case 'EmailIncorreto':
      case 'SenhaIncorreta':
        return 'alerta-erro';
      default:
        return 'alerta-erro';
    }
  };

  return (
    <div className="pagina-login">
      <div className="container-login">
        <div className="card-login">
          <div className="cabecalho-login">
            <h1 className="titulo-sistema">Gestus IAM</h1>
            <p className="subtitulo-sistema">Sistema de Gerenciamento de Identidade e Acesso</p>
          </div>

          <form onSubmit={handleSubmit} className="formulario-login">
          {mensagemErro && (
              <div className={`alerta ${getTipoAlerta(mensagemErro)}`}>
                <strong>{mensagemErro}</strong>
                {detalhesErro.length > 0 && (
                  <ul className="lista-detalhes-erro">
                    {detalhesErro.map((detalhe, indice) => (
                      <li key={indice}>{detalhe}</li>
                    ))}
                  </ul>
                )}
              </div>
            )}

            <div className="form-group">
              <label htmlFor="email" className="form-label obrigatorio">
                Email
              </label>
              <input
                type="email"
                id="email"
                name="email"
                value={formData.email}
                onChange={handleInputChange}
                className={`form-control ${erros.email ? 'erro' : ''}`}
                placeholder="Digite seu email"
                disabled={estaCarregando}
                autoComplete="email"
                autoFocus
              />
              {erros.email && (
                <div className="form-text erro">{erros.email}</div>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="senha" className="form-label obrigatorio">
                Senha
              </label>
              <input
                type="password"
                id="senha"
                name="senha"
                value={formData.senha}
                onChange={handleInputChange}
                className={`form-control ${erros.senha ? 'erro' : ''}`}
                placeholder="Digite sua senha"
                disabled={estaCarregando}
                autoComplete="current-password"
              />
              {erros.senha && (
                <div className="form-text erro">{erros.senha}</div>
              )}
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  name="lembrarLogin"
                  checked={formData.lembrarLogin}
                  onChange={handleInputChange}
                  disabled={estaCarregando}
                />
                <span className="checkmark"></span>
                Lembrar-me neste dispositivo
              </label>
            </div>

            <button
              type="submit"
              className="btn btn-primario btn-login"
              disabled={estaCarregando}
            >
              {estaCarregando ? (
                <>
                  <div className="spinner-pequeno"></div>
                  Entrando...
                </>
              ) : (
                'Entrar'
              )}
            </button>
          </form>

          <div className="rodape-login">
            <p className="texto-ajuda">
              Problemas para acessar? Entre em contato com o administrador do sistema.
            </p>
          </div>
        </div>

        <div className="info-sistema">
          <h3>Gestão Completa de Identidade</h3>
          <ul>
            <li>✓ Controle de usuários e permissões</li>
            <li>✓ Auditoria completa do sistema</li>
            <li>✓ Gestão de aplicações integradas</li>
            <li>✓ Segurança avançada</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default Login;