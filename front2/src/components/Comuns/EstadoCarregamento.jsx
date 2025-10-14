import React from 'react';
import './EstadoCarregamento.css';

/**
 * Componente de carregamento reutilizável com diferentes variações
 */
const EstadoCarregamento = ({
  tipo = 'giratorio', // 'giratorio', 'esqueleto', 'pontos', 'barras'
  tamanho = 'medio', // 'pequeno', 'medio', 'grande'
  mensagem = null,
  cor = 'primaria', // 'primaria', 'secundaria', 'branca'
  centralizado = true,
  linhasEsqueleto = 3
}) => {
  
  // Renderizar spinner giratório
  const renderizarGiratorio = () => (
    <div className={`carregamento-giratorio ${tamanho} ${cor}`}>
      <div className="circulo-giratorio"></div>
    </div>
  );
  
  // Renderizar esqueleto de carregamento
  const renderizarEsqueleto = () => (
    <div className="carregamento-esqueleto">
      {Array.from({ length: linhasEsqueleto }).map((_, indice) => (
        <div key={indice} className="linha-esqueleto">
          <div className="item-esqueleto" style={{ width: `${Math.random() * 40 + 60}%` }}></div>
        </div>
      ))}
    </div>
  );
  
  // Renderizar pontos animados
  const renderizarPontos = () => (
    <div className={`carregamento-pontos ${tamanho} ${cor}`}>
      <div className="ponto"></div>
      <div className="ponto"></div>
      <div className="ponto"></div>
    </div>
  );
  
  // Renderizar barras animadas
  const renderizarBarras = () => (
    <div className={`carregamento-barras ${tamanho} ${cor}`}>
      <div className="barra"></div>
      <div className="barra"></div>
      <div className="barra"></div>
      <div className="barra"></div>
    </div>
  );
  
  // Selecionar renderizador baseado no tipo
  const renderizarConteudo = () => {
    switch (tipo) {
      case 'esqueleto':
        return renderizarEsqueleto();
      case 'pontos':
        return renderizarPontos();
      case 'barras':
        return renderizarBarras();
      case 'giratorio':
      default:
        return renderizarGiratorio();
    }
  };
  
  return (
    <div className={`estado-carregamento ${centralizado ? 'centralizado' : ''}`}>
      {renderizarConteudo()}
      {mensagem && (
        <div className="mensagem-carregamento">
          {mensagem}
        </div>
      )}
    </div>
  );
};

export default EstadoCarregamento;