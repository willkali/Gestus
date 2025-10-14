import React from 'react';
import './ModalConfirmacao.css';

const ModalConfirmacao = ({
  aberto,
  titulo = 'Confirmação',
  mensagem = 'Tem certeza?',
  textoBotaoConfirmar = 'Confirmar',
  textoBotaoCancelar = 'Cancelar',
  onConfirmar,
  onCancelar,
  tipo = 'perigo' // 'perigo', 'aviso', 'info'
}) => {
  if (!aberto) return null;

  const obterIcone = () => {
    switch (tipo) {
      case 'perigo':
        return '⚠️';
      case 'aviso':
        return '💡';
      case 'info':
        return 'ℹ️';
      default:
        return '❓';
    }
  };

  const obterCorTema = () => {
    switch (tipo) {
      case 'perigo':
        return 'vermelho';
      case 'aviso':
        return 'amarelo';
      case 'info':
        return 'azul';
      default:
        return 'cinza';
    }
  };

  return (
    <div className="modal-overlay" onClick={onCancelar}>
      <div className={`modal-confirmacao ${obterCorTema()}`} onClick={(e) => e.stopPropagation()}>
        <div className="modal-confirmacao-header">
          <div className="icone-confirmacao">
            {obterIcone()}
          </div>
          <h3 className="titulo-confirmacao">{titulo}</h3>
        </div>
        
        <div className="modal-confirmacao-body">
          <p className="mensagem-confirmacao">{mensagem}</p>
        </div>
        
        <div className="modal-confirmacao-footer">
          <button 
            className="btn btn-secundario"
            onClick={onCancelar}
          >
            {textoBotaoCancelar}
          </button>
          <button 
            className={`btn btn-${tipo === 'perigo' ? 'perigo' : 'primario'}`}
            onClick={onConfirmar}
          >
            {textoBotaoConfirmar}
          </button>
        </div>
      </div>
    </div>
  );
};

export default ModalConfirmacao;