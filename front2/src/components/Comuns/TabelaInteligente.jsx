import React, { useState, useEffect, useCallback } from 'react';
import EstadoCarregamento from './EstadoCarregamento';
import './TabelaInteligente.css';

/**
 * Componente de tabela inteligente reutilizável
 * Suporta paginação, filtros, ordenação e seleção múltipla
 */
const TabelaInteligente = ({
  // Dados e carregamento
  dados = [],
  carregando = false,
  erro = null,
  
  // Configuração das colunas
  colunas = [],
  
  // Paginação
  paginacao = null, // { paginaAtual, totalPaginas, totalItens, itensPorPagina }
  aoMudarPagina = null,
  
  // Ordenação
  ordenacao = null, // { campo, direcao }
  aoMudarOrdenacao = null,
  
  // Seleção
  selecionavel = false,
  itensSelecionados = [],
  aoSelecionarItem = null,
  aoSelecionarTodos = null,
  
  // Ações
  acoesPorLinha = null, // função que retorna array de ações para cada linha
  acoesEmLote = null, // array de ações para itens selecionados
  aoExecutarAcaoLote = null,
  
  // Personalização
  className = '',
  semDadosMensagem = 'Nenhum item encontrado',
  alturaFixa = false,
  
  // Callbacks
  aoClicarLinha = null
}) => {
  
  const [ordenacaoLocal, setOrdenacaoLocal] = useState(ordenacao || {});
  
  // Função para ordenar dados localmente se não houver ordenação externa
  const manipularOrdenacao = useCallback((campo) => {
    if (aoMudarOrdenacao) {
      // Ordenação controlada externamente
      const novaOrdenacao = {
        campo,
        direcao: ordenacao?.campo === campo && ordenacao?.direcao === 'asc' ? 'desc' : 'asc'
      };
      aoMudarOrdenacao(novaOrdenacao);
    } else {
      // Ordenação local
      const novaOrdenacao = {
        campo,
        direcao: ordenacaoLocal.campo === campo && ordenacaoLocal.direcao === 'asc' ? 'desc' : 'asc'
      };
      setOrdenacaoLocal(novaOrdenacao);
    }
  }, [aoMudarOrdenacao, ordenacao, ordenacaoLocal]);
  
  // Renderizar cabeçalho da tabela
  const renderCabecalho = () => (
    <thead className="tabela-cabecalho">
      <tr>
        {selecionavel && (
          <th className="coluna-selecao">
            <label className="checkbox-wrapper">
              <input
                type="checkbox"
                checked={dados.length > 0 && itensSelecionados.length === dados.length}
                onChange={(e) => aoSelecionarTodos?.(e.target.checked)}
                className="checkbox-input"
              />
              <span className="checkbox-custom"></span>
            </label>
          </th>
        )}
        {colunas.map((coluna) => (
          <th 
            key={coluna.campo}
            className={`
              tabela-coluna 
              ${coluna.ordenavel ? 'ordenavel' : ''} 
              ${coluna.alinhamento ? `texto-${coluna.alinhamento}` : ''}
              ${coluna.largura ? `largura-${coluna.largura}` : ''}
            `}
            onClick={coluna.ordenavel ? () => manipularOrdenacao(coluna.campo) : undefined}
            style={coluna.estilo}
          >
            <div className="conteudo-cabecalho">
              <span>{coluna.titulo}</span>
              {coluna.ordenavel && (
                <span className="icone-ordenacao">
                  {(ordenacao?.campo || ordenacaoLocal.campo) === coluna.campo ? (
                    (ordenacao?.direcao || ordenacaoLocal.direcao) === 'asc' ? '↑' : '↓'
                  ) : '↕'}
                </span>
              )}
            </div>
          </th>
        ))}
        {acoesPorLinha && (
          <th className="coluna-acoes">Ações</th>
        )}
      </tr>
    </thead>
  );
  
  // Renderizar linha da tabela
  const renderLinha = (item, indice) => {
    const itemSelecionado = itensSelecionados.some(id => 
      typeof id === 'object' ? id.id === item.id : id === item.id
    );
    
    return (
      <tr 
        key={item.id || indice}
        className={`
          tabela-linha 
          ${itemSelecionado ? 'selecionada' : ''}
          ${aoClicarLinha ? 'clicavel' : ''}
        `}
        onClick={aoClicarLinha ? () => aoClicarLinha(item) : undefined}
      >
        {selecionavel && (
          <td className="coluna-selecao">
            <label className="checkbox-wrapper">
              <input
                type="checkbox"
                checked={itemSelecionado}
                onChange={(e) => {
                  e.stopPropagation();
                  aoSelecionarItem?.(item, e.target.checked);
                }}
                className="checkbox-input"
              />
              <span className="checkbox-custom"></span>
            </label>
          </td>
        )}
        {colunas.map((coluna) => (
          <td 
            key={coluna.campo}
            className={`
              tabela-celula 
              ${coluna.alinhamento ? `texto-${coluna.alinhamento}` : ''}
            `}
          >
            {coluna.renderizar ? 
              coluna.renderizar(item[coluna.campo], item, indice) : 
              item[coluna.campo]
            }
          </td>
        ))}
        {acoesPorLinha && (
          <td className="coluna-acoes">
            <div className="acoes-linha">
              {acoesPorLinha(item).map((acao, idx) => (
                <button
                  key={idx}
                  onClick={(e) => {
                    e.stopPropagation();
                    acao.onClick(item);
                  }}
                  className={`btn-acao ${acao.tipo || 'secundario'}`}
                  title={acao.titulo}
                  disabled={acao.desabilitado}
                >
                  {acao.icone && <span className="icone">{acao.icone}</span>}
                  {acao.rotulo}
                </button>
              ))}
            </div>
          </td>
        )}
      </tr>
    );
  };
  
  // Renderizar paginação
  const renderPaginacao = () => {
    if (!paginacao || paginacao.totalPaginas <= 1) return null;
    
    return (
      <div className="tabela-paginacao">
        <div className="info-paginacao">
          <span>
            Mostrando {((paginacao.paginaAtual - 1) * paginacao.itensPorPagina) + 1} a{' '}
            {Math.min(paginacao.paginaAtual * paginacao.itensPorPagina, paginacao.totalItens)} de{' '}
            {paginacao.totalItens} itens
          </span>
        </div>
        
        <div className="controles-paginacao">
          <button
            onClick={() => aoMudarPagina?.(1)}
            disabled={paginacao.paginaAtual === 1}
            className="btn-paginacao"
            title="Primeira página"
          >
            «
          </button>
          
          <button
            onClick={() => aoMudarPagina?.(paginacao.paginaAtual - 1)}
            disabled={paginacao.paginaAtual === 1}
            className="btn-paginacao"
            title="Página anterior"
          >
            ‹
          </button>
          
          <span className="pagina-atual">
            Página {paginacao.paginaAtual} de {paginacao.totalPaginas}
          </span>
          
          <button
            onClick={() => aoMudarPagina?.(paginacao.paginaAtual + 1)}
            disabled={paginacao.paginaAtual === paginacao.totalPaginas}
            className="btn-paginacao"
            title="Próxima página"
          >
            ›
          </button>
          
          <button
            onClick={() => aoMudarPagina?.(paginacao.totalPaginas)}
            disabled={paginacao.paginaAtual === paginacao.totalPaginas}
            className="btn-paginacao"
            title="Última página"
          >
            »
          </button>
        </div>
      </div>
    );
  };
  
  // Renderizar ações em lote
  const renderAcoesEmLote = () => {
    if (!acoesEmLote || !itensSelecionados.length) return null;
    
    return (
      <div className="acoes-lote">
        <span className="contador-selecionados">
          {itensSelecionados.length} item(ns) selecionado(s)
        </span>
        <div className="botoes-lote">
          {acoesEmLote.map((acao, indice) => (
            <button
              key={indice}
              onClick={() => aoExecutarAcaoLote?.(acao.key, itensSelecionados)}
              className={`btn ${acao.tipo || 'secundario'}`}
              disabled={acao.desabilitado}
            >
              {acao.icone && <span className="icone">{acao.icone}</span>}
              {acao.rotulo}
            </button>
          ))}
        </div>
      </div>
    );
  };
  
  // Renderizar estado de erro
  if (erro) {
    return (
      <div className="tabela-erro">
        <div className="erro-conteudo">
          <span className="erro-icone">⚠️</span>
          <h3>Erro ao carregar dados</h3>
          <p>{erro}</p>
        </div>
      </div>
    );
  }
  
  return (
    <div className={`tabela-inteligente ${className}`}>
      {renderAcoesEmLote()}
      
      <div className={`tabela-container ${alturaFixa ? 'altura-fixa' : ''}`}>
        {carregando ? (
          <EstadoCarregamento />
        ) : (
          <table className="tabela">
            {renderCabecalho()}
            <tbody className="tabela-corpo">
              {dados.length > 0 ? (
                dados.map(renderLinha)
              ) : (
                <tr>
                  <td 
                    colSpan={
                      colunas.length + 
                      (selecionavel ? 1 : 0) + 
                      (acoesPorLinha ? 1 : 0)
                    }
                    className="sem-dados"
                  >
                    <div className="sem-dados-conteudo">
                      <span className="sem-dados-icone">📄</span>
                      <p>{semDadosMensagem}</p>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>
      
      {renderPaginacao()}
    </div>
  );
};

export default TabelaInteligente;