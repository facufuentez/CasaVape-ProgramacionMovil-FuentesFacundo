using CasaVape.Core.Data;
using CasaVape.Core.DTOs;
using CasaVape.Core.Models.Entities;
using CasaVape.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaVapeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly AuditoriaService _auditoria;

    public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment, AuditoriaService auditoria)
    {
        _context = context;
        _environment = environment;
        _auditoria = auditoria;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> Get(string? q, int? categoryId, int page = 1, int pageSize = 12, bool todos = false)
    {
        if (todos && !User.IsInRole("Administrador"))
        {
            todos = false;
        }

        var query = _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .AsQueryable();

        if (!todos)
        {
            query = query.Where(p => p.Activo);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => p.Nombre.Contains(q) ||
                                     (p.Descripcion != null && p.Descripcion.Contains(q)) ||
                                     (p.Categoria != null && p.Categoria.Nombre.Contains(q)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.IdCategoria == categoryId.Value);
        }

        var products = await query
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                IdProducto = p.IdProducto,
                IdCategoria = p.IdCategoria,
                Categoria = p.Categoria != null ? p.Categoria.Nombre : string.Empty,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Stock = p.Stock,
                ImagenUrl = p.ImagenUrl,
                Activo = p.Activo
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.IdProducto == id)
            .Select(p => new ProductDto
            {
                IdProducto = p.IdProducto,
                IdCategoria = p.IdCategoria,
                Categoria = p.Categoria != null ? p.Categoria.Nombre : string.Empty,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Stock = p.Stock,
                ImagenUrl = p.ImagenUrl,
                Activo = p.Activo
            })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ProductDto>> Create([FromForm] CreateProductRequest request, IFormFile? imagenArchivo)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var category = await _context.Categorias.FindAsync(request.IdCategoria);
        if (category is null)
        {
            return BadRequest(new { error = "La categoría no existe." });
        }

        var product = new Producto
        {
            IdCategoria = request.IdCategoria,
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Precio = request.Precio,
            Stock = request.Stock,
            Activo = request.Activo
        };

        if (imagenArchivo is not null && imagenArchivo.Length > 0)
        {
            var imageUrl = await SaveImageAsync(imagenArchivo);
            if (imageUrl is null)
            {
                return BadRequest(new { error = "Solo se permiten archivos de imagen (.jpg, .jpeg, .png, .gif, .webp)." });
            }

            product.ImagenUrl = imageUrl;
        }

        _context.Productos.Add(product);
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Crear", "Producto", product.IdProducto.ToString(), null, new
        {
            product.Nombre,
            product.IdCategoria,
            product.Precio,
            product.Stock,
            product.Activo,
            product.ImagenUrl
        });

        var dto = await GetDtoAsync(product.IdProducto);
        return CreatedAtAction(nameof(GetById), new { id = product.IdProducto }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromForm] UpdateProductRequest request, IFormFile? imagenArchivo)
    {
        if (id != request.IdProducto && request.IdProducto != 0)
        {
            return BadRequest(new { error = "El id no coincide." });
        }

        var product = await _context.Productos.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var before = new
        {
            product.Nombre,
            product.IdCategoria,
            product.Precio,
            product.Stock,
            product.Activo,
            product.ImagenUrl
        };

        product.IdCategoria = request.IdCategoria;
        product.Nombre = request.Nombre;
        product.Descripcion = request.Descripcion;
        product.Precio = request.Precio;
        product.Stock = request.Stock;
        product.Activo = request.Activo;

        if (imagenArchivo is not null && imagenArchivo.Length > 0)
        {
            var imageUrl = await SaveImageAsync(imagenArchivo);
            if (imageUrl is null)
            {
                return BadRequest(new { error = "Solo se permiten archivos de imagen (.jpg, .jpeg, .png, .gif, .webp)." });
            }

            DeleteImage(product.ImagenUrl);
            product.ImagenUrl = imageUrl;
        }

        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Editar", "Producto", id.ToString(), before, new
        {
            product.Nombre,
            product.IdCategoria,
            product.Precio,
            product.Stock,
            product.Activo,
            product.ImagenUrl
        });

        var dto = await GetDtoAsync(product.IdProducto);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Productos.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var hasOrders = await _context.DetallesPedido.AnyAsync(d => d.IdProducto == id);
        if (hasOrders)
        {
            return BadRequest(new { error = "No se puede eliminar el producto porque tiene pedidos asociados. Puede marcarlo como inactivo." });
        }

        DeleteImage(product.ImagenUrl);

        _context.Productos.Remove(product);
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAsync("Eliminar", "Producto", id.ToString(), new
        {
            product.Nombre,
            product.IdCategoria,
            product.Precio,
            product.Stock,
            product.Activo,
            product.ImagenUrl
        }, null);

        return NoContent();
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
    {
        return Ok("API funcionando correctamente");
    }
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoryDto
            {
                IdCategoria = c.IdCategoria,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion
            })
            .ToListAsync();

        return Ok(categories);
    }

    private async Task<ProductDto?> GetDtoAsync(int id)
    {
        return await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.IdProducto == id)
            .Select(p => new ProductDto
            {
                IdProducto = p.IdProducto,
                IdCategoria = p.IdCategoria,
                Categoria = p.Categoria != null ? p.Categoria.Nombre : string.Empty,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Stock = p.Stock,
                ImagenUrl = p.ImagenUrl,
                Activo = p.Activo
            })
            .FirstOrDefaultAsync();
    }

    private async Task<string?> SaveImageAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        if (!allowedExtensions.Contains(extension))
        {
            return null;
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "productos");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid() + extension;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return "/images/productos/" + uniqueFileName;
    }

    private void DeleteImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        var path = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}
