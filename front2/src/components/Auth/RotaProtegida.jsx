import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAutenticacao } from '../../contexts/ContextoAutenticacao';

const RotaProtegida = ({ children }) => {
  const { estaAutenticado, estaCarregando } = useAutenticacao();
  const localizacao = useLocation();

  // Mostrar loading enquanto verifica autenticação
  if (estaCarregando) {
    return (
      <div className="loading-container">
        <div className="loading-spinner">
          <div className="spinner"></div>
          <p>Verificando autenticação...</p>
        </div>
      </div>
    );
  }

  // Se não está autenticado, redirecionar para login
  if (!estaAutenticado) {
    return <Navigate to="/login" state={{ from: localizacao }} replace />;
  }

  // Se está autenticado, renderizar os componentes filhos
  return children;
};

export default RotaProtegida;