using Connection360.Api.Extensions;
using Connection360.Api.Filters;
using Connection360.Api.Middleware;
using Connection360.Api.Models;
using Connection360.Domain.Ports.Persistence;
using Connection360.Infrastructure.Adapters.Input;
using Connection360.Infrastructure.DependencyInjection;
using Connection360.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ---------- Registro de capas (composition root) ----------

// 1. Obtener la cadena de conexión
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection")
    ?? throw new InvalidOperationException("La conexión PostgresConnection no está configurada.");

// 2. Registrar NpgsqlDataSource como SINGLETON (Gestiona el pool global sin reabrir sockets)
builder.Services.AddSingleton(sp => NpgsqlDataSource.Create(connectionString));

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddApplicationServices();               // Application (casos de uso)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddApiVersioningSetup();

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll",
//        policy =>
//        {
//            policy
//                .WithOrigins(
//                    "http://localhost:4200"
//                 )
//                //.AllowAnyOrigin()
//                .AllowAnyMethod()
//                .AllowAnyHeader()
//                .AllowCredentials();
//        });
//});

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseFilter>();
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = String.Join(" | ", context.ModelState
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage)));

        var response = new ApiResponse<Object>
        {
            Status = StatusCodes.Status400BadRequest,
            Error = errors,
            Message = "Errores de validación",
            Path = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddControllers();

// Swagger con soporte de JWT Bearer para probar endpoints protegidos
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Connection 360 API", Version = "v1" });
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
// El middleware de excepciones va PRIMERO en el pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
//app.UseCors("AllowAll");	
app.UseHttpsRedirection(); // Fuerza TLS
app.UseAuthentication();
app.UseAuthorization();



app.MapControllers();
app.UseMiddleware<NotFoundMiddleware>();
app.MapHealthChecks("/health");
app.Map("/error", () => Results.Problem(title: "Ha ocurrido un error inesperado."));

// Mapeo del Endpoint del Hub (El Hub vive en la capa de Infrastructure)
app.MapHub<NotificationHub>("api/v{version:apiVersion}/hubs/notifications");

app.Run();
