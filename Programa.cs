using Gestus;

var builder = WebApplication.CreateBuilder(args);

// Configurar todos os serviços
builder.ConfigurarServicos();

var app = builder.Build();

// Configurar o pipeline de requisição
await app.ConfigurarPipelineAsync();

app.Run();