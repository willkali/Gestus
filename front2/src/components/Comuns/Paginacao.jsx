import React from 'react';
import './Paginacao.css';

const Paginacao = ({ paginaAtual = 1, totalPaginas = 0, totalItens = 0, onMudarPagina }) => {
  if (!totalPaginas || totalPaginas <= 1) return null;

  const podeAnterior = paginaAtual > 1;
  const podeProximo = paginaAtual < totalPaginas;

  const irPara = (pagina) => {
    if (pagina < 1 || pagina > totalPaginas) return;
    onMudarPagina && onMudarPagina(pagina);
  };

  const gerarPaginas = () => {
    const paginas = [];
    const inicio = Math.max(1, paginaAtual - 2);
    const fim = Math.min(totalPaginas, paginaAtual + 2);

    for (let i = inicio; i <= fim; i++) {
      paginas.push(i);
    }
    return paginas;
  };

  return (
    <div className="paginacao">
      <div className="resumo">
        <span>Total: {totalItens.toLocaleString('pt-BR')} itens</span>
        <span>Página {paginaAtual} de {totalPaginas}</span>
      </div>
      <div className="controles">
        <button 
          className="btn-nav" 
          onClick={() => irPara(1)}
          disabled={!podeAnterior}
          title="Primeira página"
        >
          «
        </button>
        <button 
          className="btn-nav" 
          onClick={() => irPara(paginaAtual - 1)}
          disabled={!podeAnterior}
          title="Página anterior"
        >
          ‹
        </button>

        <div className="lista-paginas">
          {gerarPaginas().map(p => (
            <button
              key={p}
              className={`btn-pagina ${p === paginaAtual ? 'ativa' : ''}`}
              onClick={() => irPara(p)}
            >
              {p}
            </button>
          ))}
        </div>

        <button 
          className="btn-nav" 
          onClick={() => irPara(paginaAtual + 1)}
          disabled={!podeProximo}
          title="Próxima página"
        >
          ›
        </button>
        <button 
          className="btn-nav" 
          onClick={() => irPara(totalPaginas)}
          disabled={!podeProximo}
          title="Última página"
        >
          »
        </button>
      </div>
    </div>
  );
};

export default Paginacao;
