using Microsoft.OpenApi.Models;
using Connection360.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---------- Registro de capas (composition root) ----------
builder.Services.AddApplicationServices();               // Application (casos de uso)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddApiVersioningSetup();

// Add services to the container.

builder.Services.AddControllers();

// Swagger con soporte de JWT Bearer para probar endpoints protegidos
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Products API (Hexagonal)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresar: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks();

// HSTS solo aplica en produccion (en dev rompe localhost sin certificado real)
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    // OWASP: no exponer detalles de excepcion en produccion
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection(); // Fuerza TLS
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.Map("/error", () => Results.Problem(title: "Ha ocurrido un error inesperado."));


app.Run();
