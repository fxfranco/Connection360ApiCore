using Connection360Notification.Api.Extensions;
using Connection360Notification.Api.Filters;
using Connection360Notification.Api.Middleware;
using Connection360Notification.Api.Models;
using Connection360Notification.Domain.Settings;
using Connection360Notification.Infrastructure.Adapters.Input;
using Connection360Notification.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Mapeo de Configuraciones
// MongoDbSettings ya no se mapea aquí: lo hace el propio proveedor de persistencia
// (MongoPersistenceServiceCollectionExtensions.AddMongoPersistence, invocado desde
// AddInfrastructure) para que Program.cs no necesite saber qué motor de base de datos
// está activo ni sus claves de configuración.
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("KafkaSettings"));

builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddApiVersioningSetup();

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Connection 360 API Notifications", Version = "v1" });
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
//builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

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
//app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseMiddleware<NotFoundMiddleware>();
app.MapHealthChecks("/health");
app.Map("/error", () => Results.Problem(title: "Ha ocurrido un error inesperado."));
// Mapeo del Endpoint del Hub (El Hub vive en la capa de Infrastructure)
app.MapHub<NotificationHub>("api/v{version:apiVersion}/hubs/notifications");

app.Run();
