// Script simples para testar se todas as páginas carregam sem erros críticos
const fs = require('fs');
const path = require('path');

console.log('🧪 Iniciando testes básicos das páginas...\n');

// Lista de páginas para verificar
const paginas = [
  { nome: 'Usuarios', caminho: 'src/pages/Usuarios/Usuarios.jsx' },
  { nome: 'Papeis', caminho: 'src/pages/Papeis/Papeis.jsx' },
  { nome: 'Aplicacoes', caminho: 'src/pages/Aplicacoes/Aplicacoes.jsx' },
  { nome: 'Grupos', caminho: 'src/pages/Grupos/Grupos.jsx' },
  { nome: 'Auditoria', caminho: 'src/pages/Auditoria/Auditoria.jsx' },
  { nome: 'Configuracoes', caminho: 'src/pages/Configuracoes/Configuracoes.jsx' },
  { nome: 'Dashboard', caminho: 'src/pages/Dashboard/Dashboard.jsx' },
  { nome: 'Login', caminho: 'src/pages/Auth/Login.jsx' }
];

// Lista de componentes para verificar
const componentes = [
  { nome: 'Layout', caminho: 'src/components/Layout/Layout.jsx' },
  { nome: 'Header', caminho: 'src/components/Layout/Header.jsx' },
  { nome: 'Sidebar', caminho: 'src/components/Layout/Sidebar.jsx' },
  { nome: 'RotaProtegida', caminho: 'src/components/Auth/RotaProtegida.jsx' },
  { nome: 'EstadoCarregamento', caminho: 'src/components/Comuns/EstadoCarregamento.jsx' },
  { nome: 'Paginacao', caminho: 'src/components/Comuns/Paginacao.jsx' },
  { nome: 'ModalConfirmacao', caminho: 'src/components/Comuns/ModalConfirmacao.jsx' }
];

let erros = [];
let sucesso = 0;

// Verificar se os arquivos existem
function verificarArquivo(item, tipo) {
  try {
    if (fs.existsSync(item.caminho)) {
      const conteudo = fs.readFileSync(item.caminho, 'utf8');
      
      // Verificações básicas
      const temImportReact = conteudo.includes('import React');
      const temExportDefault = conteudo.includes('export default');
      
      if (!temImportReact) {
        erros.push(`❌ ${item.nome} (${tipo}): Não importa React`);
      } else if (!temExportDefault) {
        erros.push(`❌ ${item.nome} (${tipo}): Não tem export default`);
      } else {
        console.log(`✅ ${item.nome} (${tipo}): OK`);
        sucesso++;
      }
    } else {
      erros.push(`❌ ${item.nome} (${tipo}): Arquivo não encontrado`);
    }
  } catch (erro) {
    erros.push(`❌ ${item.nome} (${tipo}): Erro ao ler arquivo - ${erro.message}`);
  }
}

// Verificar todas as páginas
console.log('📄 Verificando páginas...');
paginas.forEach(pagina => verificarArquivo(pagina, 'Página'));

console.log('\n🧩 Verificando componentes...');
componentes.forEach(componente => verificarArquivo(componente, 'Componente'));

// Verificar arquivos CSS principais
console.log('\n🎨 Verificando arquivos CSS...');
const cssImportantes = [
  'src/styles/App.css',
  'src/pages/Usuarios/Usuarios.css',
  'src/pages/Papeis/Papeis.css',
  'src/pages/Aplicacoes/Aplicacoes.css',
  'src/pages/Grupos/Grupos.css',
  'src/pages/Auditoria/Auditoria.css',
  'src/pages/Configuracoes/Configuracoes.css'
];

cssImportantes.forEach(css => {
  if (fs.existsSync(css)) {
    console.log(`✅ CSS: ${css}`);
    sucesso++;
  } else {
    erros.push(`❌ CSS: ${css} não encontrado`);
  }
});

// Verificar package.json
console.log('\n📦 Verificando dependências...');
if (fs.existsSync('package.json')) {
  const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
  const dependenciasEssenciais = [
    'react',
    'react-dom', 
    'react-router-dom',
    'axios',
    'crypto-js'
  ];
  
  dependenciasEssenciais.forEach(dep => {
    if (packageJson.dependencies && packageJson.dependencies[dep]) {
      console.log(`✅ Dependência: ${dep} v${packageJson.dependencies[dep]}`);
      sucesso++;
    } else {
      erros.push(`❌ Dependência: ${dep} não encontrada`);
    }
  });
}

// Relatório final
console.log('\n' + '='.repeat(50));
console.log('📊 RELATÓRIO FINAL');
console.log('='.repeat(50));
console.log(`✅ Verificações bem-sucedidas: ${sucesso}`);
console.log(`❌ Erros encontrados: ${erros.length}`);

if (erros.length > 0) {
  console.log('\n🔍 ERROS DETALHADOS:');
  erros.forEach(erro => console.log(erro));
} else {
  console.log('\n🎉 Todos os testes básicos passaram!');
  console.log('🚀 O projeto está pronto para testes manuais no navegador.');
}

console.log('\n💡 Próximos passos:');
console.log('1. Verificar funcionamento no navegador (http://localhost:3000)');
console.log('2. Testar autenticação e navegação');
console.log('3. Testar funcionalidades CRUD de cada página');
console.log('4. Verificar responsividade em diferentes dispositivos');