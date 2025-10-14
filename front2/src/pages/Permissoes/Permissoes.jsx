import React, { useState, useEffect } from 'react';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';
import EstadoCarregamento from '../../components/Comuns/EstadoCarregamento';
import Paginacao from '../../components/Comuns/Paginacao';
import api from '../../services/api';
import './Permissoes.css';

const Permissoes = () => {
  const { temPermissao } = useAutenticacao();
  const [permissoes, setPermissoes] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [paginacao, setPaginacao] = useState({
    paginaAtual: 1,
    itensPorPagina: 15,
    totalItens: 0,
    totalPaginas: 0
  });
  const [filtros, setFiltros] = useState({
    busca: '',
    categoria: '',
    ordenarPor: 'nome',
    direcaoOrdenacao: 'asc'
  });

  // Carregar permissões
  const carregarPermissoes = async (novaPagina = paginacao.paginaAtual) => {
    try {
      setCarregando(true);
      const params = new URLSearchParams({
        pagina: novaPagina,
        itensPorPagina: paginacao.itensPorPagina,
        ordenarPor: filtros.ordenarPor,
        direcaoOrdenacao: filtros.direcaoOrdenacao
      });
      
      if (filtros.busca) params.append('busca', filtros.busca);
      if (filtros.categoria) params.append('categoria', filtros.categoria);
      
      const response = await api.get(`/api/permissoes?${params}`);
      setPermissoes(response.data.dados || []);
      setPaginacao(prev => ({
        ...prev,
        paginaAtual: response.data.paginaAtual || novaPagina,
        totalItens: response.data.totalItens || 0,
        totalPaginas: response.data.totalPaginas || 0
      }));
    } catch (erro) {
      console.error('Erro ao carregar permissões:', erro);
      setPermissoes([]);
    } finally {
      setCarregando(false);
    }
  };

  useEffect(() => {
    if (temPermissao('Permissoes.Listar')) {
      carregarPermissoes();
    }
  }, [filtros, temPermissao]);

  // Aplicar filtros
  const aplicarFiltros = () => {
    setPaginacao(prev => ({ ...prev, paginaAtual: 1 }));
    carregarPermissoes(1);
  };

  // Limpar filtros
  const limparFiltros = () => {
    setFiltros({
      busca: '',
      categoria: '',
      ordenarPor: 'nome',
      direcaoOrdenacao: 'asc'
    });
  };

  // Agrupar permissões por categoria
  const permissoesAgrupadas = permissoes.reduce((grupos, permissao) => {
    const categoria = permissao.categoria || 'Geral';
    if (!grupos[categoria]) {
      grupos[categoria] = [];
    }
    grupos[categoria].push(permissao);
    return grupos;
  }, {});

  // Obter categorias únicas
  const categorias = [...new Set(permissoes.map(p => p.categoria || 'Geral'))].sort();

  if (!temPermissao('Permissoes.Listar')) {
    return (
      <div className="pagina-sem-permissao">
        <h2>Acesso Negado</h2>
        <p>Você não tem permissão para visualizar permissões.</p>
      </div>
    );
  }

  return (
    <div className="pagina-permissoes">
      {/* Cabeçalho */}
      <div className="cabecalho-pagina">
        <div className="titulo-secao">
          <h1>🔐 Permissões</h1>
          <p>Visualizar permissões disponíveis no sistema</p>
        </div>
        <div className="estatisticas-permissoes">
          <div className="stat-card">
            <span className="stat-numero">{permissoes.length}</span>
            <span className="stat-label">Total</span>
          </div>
          <div className="stat-card">
            <span className="stat-numero">{categorias.length}</span>
            <span className="stat-label">Categorias</span>
          </div>
        </div>
      </div>

      {/* Filtros */}
      <div className="filtros-permissoes">
        <div className="card">
          <div className="card-body">
            <div className="row-filtros">
              <div className="campo-filtro">
                <input
                  type="text"
                  placeholder="🔍 Buscar por nome ou descrição..."
                  value={filtros.busca}
                  onChange={(e) => setFiltros(prev => ({ ...prev, busca: e.target.value }))}
                  className="input-busca"
                />
              </div>
              <div className="campo-filtro">
                <select 
                  value={filtros.categoria}
                  onChange={(e) => setFiltros(prev => ({ ...prev, categoria: e.target.value }))}
                  className="select-filtro"
                >
                  <option value="">Todas as categorias</option>
                  {categorias.map(categoria => (
                    <option key={categoria} value={categoria}>{categoria}</option>
                  ))}
                </select>
              </div>
              <div className="acoes-filtro">
                <button className="btn btn-secundario" onClick={aplicarFiltros}>🔍 Filtrar</button>
                <button className="btn btn-neutro" onClick={limparFiltros}>🗑️ Limpar</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Lista de Permissões */}
      <div className="card">
        <div className="card-body">
          {carregando ? (
            <EstadoCarregamento mensagem="Carregando permissões..." />
          ) : permissoes.length > 0 ? (
            <>
              {/* Visualização Agrupada */}
              <div className="permissoes-agrupadas">
                {Object.entries(permissoesAgrupadas).map(([categoria, permissoesCategoria]) => (
                  <div key={categoria} className="grupo-categoria">
                    <div className="cabecalho-categoria">
                      <h3 className="titulo-categoria">📂 {categoria}</h3>
                      <span className="contador-categoria">{permissoesCategoria.length} permissões</span>
                    </div>
                    
                    <div className="grid-permissoes">
                      {permissoesCategoria.map(permissao => (
                        <div key={permissao.id} className="card-permissao">
                          <div className="cabecalho-permissao">
                            <div className="icone-permissao">🔑</div>
                            <div className="info-permissao">
                              <h4 className="nome-permissao">{permissao.nome}</h4>
                              <p className="descricao-permissao">{permissao.descricao}</p>
                            </div>
                          </div>
                          
                          <div className="detalhes-permissao">
                            <div className="detalhe-item">
                              <span className="label-detalhe">Módulo:</span>
                              <span className="valor-detalhe">{permissao.modulo || 'Sistema'}</span>
                            </div>
                            
                            {permissao.recurso && (
                              <div className="detalhe-item">
                                <span className="label-detalhe">Recurso:</span>
                                <span className="valor-detalhe">{permissao.recurso}</span>
                              </div>
                            )}
                            
                            {permissao.acao && (
                              <div className="detalhe-item">
                                <span className="label-detalhe">Ação:</span>
                                <span className="badge badge-acao">{permissao.acao}</span>
                              </div>
                            )}
                          </div>
                          
                          {permissao.dependencias && permissao.dependencias.length > 0 && (
                            <div className="dependencias-permissao">
                              <span className="label-dependencias">Depende de:</span>
                              <div className="lista-dependencias">
                                {permissao.dependencias.map((dep, index) => (
                                  <span key={index} className="badge badge-dependencia">{dep}</span>
                                ))}
                              </div>
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
              
              <Paginacao 
                paginaAtual={paginacao.paginaAtual}
                totalPaginas={paginacao.totalPaginas}
                totalItens={paginacao.totalItens}
                onMudarPagina={(pagina) => carregarPermissoes(pagina)}
              />
            </>
          ) : (
            <div className="sem-dados">
              <span className="icone-vazio">🔐</span>
              <h3>Nenhuma permissão encontrada</h3>
              <p>Não há permissões cadastradas ou que correspondam aos filtros aplicados.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Permissoes;
