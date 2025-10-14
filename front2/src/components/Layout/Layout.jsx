import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import Header from './Header';
import './Layout.css';

const Layout = () => {
  const [sidebarAberta, setSidebarAberta] = useState(true);

  const alternarSidebar = () => {
    setSidebarAberta(!sidebarAberta);
  };

  return (
    <div className={`layout ${sidebarAberta ? 'sidebar-aberta' : 'sidebar-fechada'}`}>
      <Sidebar 
        estaAberta={sidebarAberta} 
        aoAlternar={alternarSidebar}
      />
      
      <div className="conteudo-principal">
        <Header 
          aoAlternarSidebar={alternarSidebar}
          sidebarAberta={sidebarAberta}
        />
        
        <main className="area-conteudo">
          <div className="container-conteudo">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};

export default Layout;