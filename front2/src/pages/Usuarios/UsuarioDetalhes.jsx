import React from 'react';
import { useParams } from 'react-router-dom';

const UsuarioDetalhes = () => {
  const { id } = useParams();

  return (
    <div className="pagina-usuario-detalhes">
      <div className="cabecalho-pagina">
        <h1>Detalhes do Usuário</h1>
        <p>Visualizar e editar informações do usuário ID: {id}</p>
      </div>

      <div className="card">
        <div className="card-body">
          <h3>Detalhes do Usuário</h3>
          <p>Esta página será desenvolvida com os detalhes completos do usuário.</p>
        </div>
      </div>
    </div>
  );
};

export default UsuarioDetalhes;