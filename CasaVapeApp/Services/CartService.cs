using System.Text.Json;
using CasaVapeApp.Models;

namespace CasaVapeApp.Services;

public class CartService
{
    private const string StorageKey = "casavape-cart";

    public List<CartItem> Items { get; private set; } = new();

    public CartService()
    {
        Load();
    }

    public void Load()
    {
        var json = Preferences.Get(StorageKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            Items = new List<CartItem>();
            return;
        }

        try
        {
            Items = JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }
        catch
        {
            Items = new List<CartItem>();
        }
    }

    public void Save()
    {
        Preferences.Set(StorageKey, JsonSerializer.Serialize(Items));
    }

    public void Add(CasaVape.Core.DTOs.ProductDto product, int cantidad = 1)
    {
        var existing = Items.FirstOrDefault(i => i.IdProducto == product.IdProducto);
        if (existing is null)
        {
            Items.Add(new CartItem
            {
                IdProducto = product.IdProducto,
                Nombre = product.Nombre,
                Precio = product.Precio,
                Cantidad = cantidad,
                Stock = product.Stock,
                ImagenUrl = product.ImagenUrl
            });
        }
        else
        {
            existing.Cantidad += cantidad;
            existing.Stock = product.Stock;
        }

        Save();
    }

    public void UpdateQuantity(int idProducto, int cantidad)
    {
        var item = Items.FirstOrDefault(i => i.IdProducto == idProducto);
        if (item is null)
        {
            return;
        }

        if (cantidad <= 0)
        {
            Items.Remove(item);
        }
        else
        {
            item.Cantidad = cantidad;
        }

        Save();
    }

    public void Remove(int idProducto)
    {
        Items.RemoveAll(i => i.IdProducto == idProducto);
        Save();
    }

    public void Clear()
    {
        Items.Clear();
        Save();
    }

    public int TotalQuantity => Items.Sum(i => i.Cantidad);

    public decimal Subtotal => Items.Sum(i => i.Subtotal);
}
