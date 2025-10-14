import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import api from '../../services/api';
import './Dashboard.css';

const Dashboard = () => {
  const { usuario, temPermissao } = useAutenticacao();
  const [estatisticas, setEstatisticas] = useState(null);
  const [carregandoEstatisticas, setCarregandoEstatisticas] = useState(true);
  const [atividadesRecentes, setAtividadesRecentes] = useState([]);
  const [carregandoAtividades, setCarregandoAtividades] = useState(true);

  // Carregar estatísticas gerais
  useEffect(() => {
    const carregarEstatisticas = async () => {
      try {
        setCarregandoEstatisticas(true);
        
        // Buscar estatísticas de diferentes endpoints
        const promises = [];
        
        if (temPermissao('Usuarios.Listar')) {
          promises.push(api.get('/api/usuarios?pagina=1&itensPorPagina=1'));
        }
        
        if (temPermissao('Papeis.Listar')) {
          promises.push(api.get('/api/papeis?pagina=1&itensPorPagina=1'));
        }
        
        if (temPermissao('Aplicacoes.Listar')) {
          promises.push(api.get('/api/aplicacoes?pagina=1&itensPorPagina=1'));
        }
        
        if (temPermissao('Grupos.Listar')) {
          promises.push(api.get('/api/grupos?pagina=1&itensPorPagina=1'));
        }
        
        const resultados = await Promise.allSettled(promises);
        
        // Processar resultados das estatísticas
        const stats = {
          totalUsuarios: 0,
          totalPapeis: 0,
          totalAplicacoes: 0,
          totalGrupos: 0
        };
        
        let indice = 0;
        if (temPermissao('Usuarios.Listar') && resultados[indice]?.status === 'fulfilled') {
          stats.totalUsuarios = resultados[indice].value.data.totalItens || 0;
        }
        indice++;
        
        if (temPermissao('Papeis.Listar') && resultados[indice]?.status === 'fulfilled') {
          stats.totalPapeis = resultados[indice].value.data.totalItens || 0;
        }
        indice++;
        
        if (temPermissao('Aplicacoes.Listar') && resultados[indice]?.status === 'fulfilled') {
          stats.totalAplicacoes = resultados[indice].value.data.totalItens || 0;
        }
        indice++;
        
        if (temPermissao('Grupos.Listar') && resultados[indice]?.status === 'fulfilled') {
          stats.totalGrupos = resultados[indice].value.data.totalItens || 0;
        }
        
        setEstatisticas(stats);
      } catch (erro) {
        console.error('Erro ao carregar estatísticas:', erro);
        setEstatisticas({
          totalUsuarios: 0,
          totalPapeis: 0,
          totalAplicacoes: 0,
          totalGrupos: 0
        });
      } finally {
        setCarregandoEstatisticas(false);
      }
    };

    carregarEstatisticas();
  }, [temPermissao]);
  
  // Carregar atividades recentes (auditoria)
  useEffect(() => {
    const carregarAtividades = async () => {
      if (!temPermissao('Auditoria.Visualizar')) {
        setCarregandoAtividades(false);
        return;
      }
      
      try {
        setCarregandoAtividades(true);
        const response = await api.get('/api/auditoria?pagina=1&itensPorPagina=5&ordenarPor=dataHora&direcaoOrdenacao=desc');
        setAtividadesRecentes(response.data.dados || []);
      } catch (erro) {
        console.error('Erro ao carregar atividades:', erro);
        setAtividadesRecentes([]);
      } finally {
        setCarregandoAtividades(false);
      }
    };

    carregarAtividades();
  }, [temPermissao]);
  
  // Renderizar cartão de estatística
  const renderCartaoEstatistica = (titulo, valor, icone, cor, permissao) => {
    if (permissao && !temPermissao(permissao)) return null;
    
    return (
      <div className={`cartao-estatistica ${cor}`}>
        <div className="icone-estatistica">{icone}</div>
        <div className="conteudo-estatistica">
          <h3 className="titulo-estatistica">{titulo}</h3>
          <div className="valor-estatistica">
            {carregandoEstatisticas ? (
              <EstadoCarregamento tipo="pontos" tamanho="pequeno" centralizado={false} />
            ) : (
              valor.toLocaleString('pt-BR')
            )}
          </div>
        </div>
      </div>
    );
  };
  
  // Formatar data/hora
  const formatarDataHora = (dataString) => {
    const data = new Date(dataString);
    const agora = new Date();
    const diferenca = agora - data;
    const minutos = Math.floor(diferenca / 60000);
    
    if (minutos < 1) return 'Agora há pouco';
    if (minutos < 60) return `${minutos} minuto${minutos > 1 ? 's' : ''} atrás`;
    
    const horas = Math.floor(minutos / 60);
    if (horas < 24) return `${horas} hora${horas > 1 ? 's' : ''} atrás`;
    
    const dias = Math.floor(horas / 24);
    if (dias < 7) return `${dias} dia${dias > 1 ? 's' : ''} atrás`;
    
    return data.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  return (
    <div className="pagina-dashboard">
      {/* Cabeçalho de Boas-vindas */}
      <div className="cabecalho-dashboard">
        <div className="boas-vindas">
          <h1>Bem-vindo, {usuario?.nome}! 👋</h1>
          <p>Aqui está um resumo do seu sistema Gestus IAM</p>
        </div>
        <div className="info-usuario-dashboard">
          <div className="avatar-dashboard">
            {usuario?.nome?.charAt(0)}{usuario?.sobrenome?.charAt(0)}
          </div>
          <div className="dados-usuario-dashboard">
            <div className="nome-completo">{usuario?.nomeCompleto}</div>
            <div className="papeis-usuario">
              {usuario?.papeis?.slice(0, 2).map((papel, index) => (
                <span key={index} className="badge badge-primario">{papel}</span>
              ))}
              {usuario?.papeis?.length > 2 && (
                <span className="badge badge-secundario">+{usuario.papeis.length - 2}</span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Cards de Estatísticas */}
      <div className="estatisticas-dashboard">
        {renderCartaoEstatistica(
          'Total de Usuários',
          estatisticas?.totalUsuarios || 0,
          '👥',
          'azul',
          'Usuarios.Listar'
        )}
        {renderCartaoEstatistica(
          'Papéis Ativos',
          estatisticas?.totalPapeis || 0,
          '🎭',
          'verde',
          'Papeis.Listar'
        )}
        {renderCartaoEstatistica(
          'Aplicações',
          estatisticas?.totalAplicacoes || 0,
          '📱',
          'roxo',
          'Aplicacoes.Listar'
        )}
        {renderCartaoEstatistica(
          'Grupos',
          estatisticas?.totalGrupos || 0,
          '👨‍👩‍👧‍👦',
          'laranja',
          'Grupos.Listar'
        )}
      </div>

      {/* Conteúdo Principal */}
      <div className="conteudo-dashboard">
        {/* Informações do Usuário */}
        <div className="secao-dashboard">
          <h2>Suas Informações</h2>
          <div className="card">
            <div className="card-body">
              <div className="info-pessoal">
                <div className="campo-info">
                  <label>Nome Completo:</label>
                  <span>{usuario?.nomeCompleto}</span>
                </div>
                <div className="campo-info">
                  <label>Email:</label>
                  <span>{usuario?.email}</span>
                </div>
                <div className="campo-info">
                  <label>Total de Logins:</label>
                  <span>{usuario?.contadorLogins?.toLocaleString('pt-BR') || 0}</span>
                </div>
                <div className="campo-info">
                  <label>Último Login:</label>
                  <span>
                    {usuario?.ultimoLogin ? 
                      formatarDataHora(usuario.ultimoLogin) : 
                      'Primeiro login'
                    }
                  </span>
                </div>
                <div className="campo-info">
                  <label>Permissões:</label>
                  <span>{usuario?.permissoes?.length || 0} permissão(ões)</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Atividades Recentes */}
        {temPermissao('Auditoria.Visualizar') && (
          <div className="secao-dashboard">
            <h2>Atividades Recentes</h2>
            <div className="card">
              <div className="card-body">
                {carregandoAtividades ? (
                  <EstadoCarregamento mensagem="Carregando atividades..." />
                ) : atividadesRecentes.length > 0 ? (
                  <div className="lista-atividades">
                    {atividadesRecentes.map((atividade, index) => (
                      <div key={index} className="item-atividade">
                        <div className="icone-atividade">
                          {atividade.acao === 'Login' ? '🔐' : 
                           atividade.acao === 'Criar' ? '➕' :
                           atividade.acao === 'Atualizar' ? '✏️' :
                           atividade.acao === 'Excluir' ? '🗑️' : '📋'}
                        </div>
                        <div className="conteudo-atividade">
                          <div className="descricao-atividade">
                            <strong>{atividade.usuario?.nome || 'Sistema'}</strong> {atividade.acao.toLowerCase()} {atividade.recurso.toLowerCase()}
                            {atividade.observacoes && (
                              <span className="observacoes"> - {atividade.observacoes}</span>
                            )}
                          </div>
                          <div className="tempo-atividade">
                            {formatarDataHora(atividade.dataHora)}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="sem-atividades">
                    <span className="icone-vazio">📋</span>
                    <p>Nenhuma atividade recente encontrada</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
