using Connection360.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Connection360.Api.Controllers
{
    /// <summary>
    /// Adaptador de entrada (Driving Adapter) del hexagono: traduce peticiones HTTP a
    /// llamadas al puerto de entrada IProductService. No contiene logica de negocio,
    /// solo orquesta HTTP <-> Aplicacion (Single Responsibility Principle).
    ///
    /// Todos los endpoints requieren un JWT valido (emitido/validado en el API Gateway
    /// o validado tambien aqui como "defensa en profundidad" - ver Program.cs).
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/products")]
    [Authorize] // Requiere JWT valido en todos los endpoints por defecto
    [Produces("application/json")]
    public sealed class ProductsController : ControllerBase
    {
        /// <summary>GET /api/v1/products - Lista paginada con busqueda opcional.</summary>
        [HttpGet]
        [AllowAnonymous] // Ejemplo: lectura publica de catalogo (ajustar segun negocio)
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] Int16 pageNumber = 1, [FromQuery] Int16 pageSize = 20, [FromQuery] String? search = null, CancellationToken cancellationToken = default)
        {
            return Ok("GET /api/v1/products");
        }

        /// <summary>GET /api/v1/products/{id} - Obtiene un producto por Id.</summary>
        [HttpGet("{id:guid}", Name = "GetProductById")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            return Ok("GET /api/v1/products/{id}");
        }

        /// <summary>POST /api/v1/products - Crea un nuevo producto.</summary>
        [HttpPost]
        [Authorize] // RBAC: solo roles autorizados pueden escribir
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
        {
            return Ok("POST /api/v1/products");
        }

        /// <summary>PUT /api/v1/products/{id} - Reemplaza los datos editables de un producto (idempotente).</summary>
        [HttpPut("{id:guid}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
        {
            return Ok("PUT /api/v1/products/{id}");
        }

        /// <summary>PATCH /api/v1/products/{id}/stock - Ajuste parcial de inventario (incrementar/decrementar).</summary>
        [HttpPatch("{id:guid}/stock")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockRequest request, CancellationToken cancellationToken)
        {
            return Ok("PATCH /api/v1/products/{id}/stock");
        }

        /// <summary>DELETE /api/v1/products/{id} - Elimina (soft delete) un producto.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize] // Solo Admin puede eliminar
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            return Ok("DELETE /api/v1/products/{id}");
        }

    }
}
