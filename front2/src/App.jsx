import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { ProvedorAutenticacao } from './contexts/ContextoAutenticacao';
import { ProvedorNotificacoes } from './contexts/ContextoNotificacoes';
import Layout from './components/Layout/Layout';
import Login from './pages/Auth/Login';
import Dashboard from './pages/Dashboard/Dashboard';
import Usuarios from './pages/Usuarios/Usuarios';
import UsuarioDetalhes from './pages/Usuarios/UsuarioDetalhes';
import Papeis from './pages/Papeis/Papeis';
import Permissoes from './pages/Permissoes/Permissoes';
import Aplicacoes from './pages/Aplicacoes/Aplicacoes';
import Grupos from './pages/Grupos/Grupos';
import Auditoria from './pages/Auditoria/Auditoria';
import Configuracoes from './pages/Configuracoes/Configuracoes';
import MeuPerfil from './pages/Perfil/MeuPerfil';
import Preferencias from './pages/Preferencias/Preferencias';
import RotaProtegida from './components/Auth/RotaProtegida';
import './styles/App.css';

function App() {
  return (
    <Router>
      <ProvedorAutenticacao>
        <ProvedorNotificacoes>
          <div className="App">
          <Routes>
            {/* Rota pública de login */}
            <Route path="/login" element={<Login />} />
            
            {/* Rotas protegidas */}
            <Route path="/" element={<RotaProtegida><Layout /></RotaProtegida>}>
              <Route index element={<Navigate to="/dashboard" replace />} />
              <Route path="dashboard" element={<Dashboard />} />
              
              {/* Gestão de Usuários */}
              <Route path="usuarios" element={<Usuarios />} />
              <Route path="usuarios/:id" element={<UsuarioDetalhes />} />
              
              {/* Gestão de Papéis e Permissões */}
              <Route path="papeis" element={<Papeis />} />
              <Route path="permissoes" element={<Permissoes />} />
              
              {/* Gestão de Aplicações */}
              <Route path="aplicacoes" element={<Aplicacoes />} />
              
              {/* Gestão de Grupos */}
              <Route path="grupos" element={<Grupos />} />
              
              {/* Auditoria */}
              <Route path="auditoria" element={<Auditoria />} />
              
              {/* Configurações */}
              <Route path="configuracoes" element={<Configuracoes />} />
              
              {/* Perfil do Usuário */}
              <Route path="perfil" element={<MeuPerfil />} />
              
              {/* Preferências do Usuário */}
              <Route path="preferencias" element={<Preferencias />} />
            </Route>

            {/* Redirect para dashboard se logado, senão para login */}
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
          </div>
        </ProvedorNotificacoes>
      </ProvedorAutenticacao>
    </Router>
  );
}

export default App;
