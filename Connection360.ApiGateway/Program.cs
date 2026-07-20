using Connection360.ApiGateway.Endpoints;
using Connection360.ApiGateway.Extensions;
using Connection360.ApiGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureSecureKestrel();

// ---------- Seguridad ----------
builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddGatewayCors(builder.Configuration);

// ---------- Enrutamiento (YARP Reverse Proxy hacia los microservicios internos) ----------
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add services to the container.

builder.Services.AddHealthChecks();

//builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

var app = builder.Build();

// El orden del pipeline es critico para la seguridad:
app.UseMiddleware<ExceptionHandlingMiddleware>();   // 1. Captura cualquier fallo primero
app.UseMiddleware<SecurityHeadersMiddleware>();     // 2. Cabeceras de seguridad en toda respuesta
app.UseMiddleware<RequestLoggingMiddleware>();      // 3. Auditoria de cada peticion

app.UseHttpsRedirection();                          // 4. Fuerza HTTPS/TLS

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseHsts();
}

app.UseCors(Connection360.ApiGateway.Extensions.CorsExtensions.PolicyName); // 5. CORS restrictivo

app.UseRateLimiter();                               // 6. Rate limiting global

app.UseAuthentication();                            // 7. Quien eres (JWT / OAuth2)
app.UseAuthorization();                              // 8. Que puedes hacer (roles/policies)

app.MapTokenEndpoints();                            // Endpoint OAuth2 client_credentials (/connect/token)
app.MapHealthChecks("/health");

//app.MapControllers();
app.MapReverseProxy();                              // 9. Enruta hacia los microservicios (p.ej. Products.Api)

app.Run();
