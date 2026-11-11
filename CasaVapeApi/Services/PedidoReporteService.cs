using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CasaVape.Core.Models.Entities;

namespace CasaVapeApi.Services
{
    public class PedidoReporteService
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

        private readonly string _rutaQr;

        public PedidoReporteService(string webRootPath)
        {
            _rutaQr = Path.Combine(webRootPath, "images", "qr-transferencia.png");
        }

        public byte[] GenerarListadoPdf(
            IReadOnlyList<Pedido> pedidos,
            DateTime? desde,
            DateTime? hasta,
            string? estado)
        {
            var generado = DateTime.Now;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(c => ComponerHeaderListado(c, desde, hasta, estado, generado));
                    page.Content().PaddingVertical(10).Element(c => ComponerContenidoListado(c, pedidos));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerarComprobantePdf(Pedido pedido)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComponerHeaderComprobante(c, pedido));
                    page.Content().PaddingVertical(10).Element(c => ComponerContenidoComprobante(c, pedido));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static void ComponerHeaderListado(
            IContainer container,
            DateTime? desde,
            DateTime? hasta,
            string? estado,
            DateTime generado)
        {
            container.Column(column =>
            {
                column.Item().Text("E-COMMERCE").FontSize(18).SemiBold();
                column.Item().Text("Listado de pedidos").FontSize(13).SemiBold();
                column.Item().Text($"Generado: {generado:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);

                var filtros = new List<string>();
                if (desde.HasValue)
                {
                    filtros.Add($"Desde {desde.Value:dd/MM/yyyy}");
                }
                if (hasta.HasValue)
                {
                    filtros.Add($"Hasta {hasta.Value:dd/MM/yyyy}");
                }
                if (!string.IsNullOrWhiteSpace(estado))
                {
                    filtros.Add($"Estado: {estado}");
                }

                if (filtros.Any())
                {
                    column.Item().Text("Filtros: " + string.Join(" | ", filtros))
                        .FontColor(Colors.Grey.Darken1);
                }

                column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });
        }

        private static void ComponerContenidoListado(IContainer container, IReadOnlyList<Pedido> pedidos)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                if (!pedidos.Any())
                {
                    column.Item().Text("No hay pedidos que coincidan con los filtros.");
                    return;
                }

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(34);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(2.8f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(1.3f);
                        columns.ConstantColumn(70);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CeldaEncabezado).Text("Nº");
                        header.Cell().Element(CeldaEncabezado).Text("Fecha");
                        header.Cell().Element(CeldaEncabezado).Text("Cliente");
                        header.Cell().Element(CeldaEncabezado).Text("Estado");
                        header.Cell().Element(CeldaEncabezado).Text("Entrega");
                        header.Cell().Element(CeldaEncabezado).Text("Pago");
                        header.Cell().Element(CeldaEncabezado).AlignRight().Text("Total");
                    });

                    foreach (var pedido in pedidos)
                    {
                        var pago = pedido.Pagos.OrderByDescending(p => p.IdPago).FirstOrDefault();
                        var cliente = pedido.Usuario == null
                            ? "-"
                            : $"{pedido.Usuario.Nombre} {pedido.Usuario.Apellido}\n{pedido.Usuario.Email}";

                        table.Cell().Element(Celda).Text($"#{pedido.IdPedido}");
                        table.Cell().Element(Celda).Text(pedido.Fecha.ToString("dd/MM/yyyy HH:mm"));
                        table.Cell().Element(Celda).Text(cliente);
                        table.Cell().Element(Celda).Text(pedido.EstadoPedido?.Nombre ?? "-");
                        table.Cell().Element(Celda).Text(pedido.MetodoEntrega?.Nombre ?? "-");
                        table.Cell().Element(Celda).Text(pago?.EstadoPago ?? "-");
                        table.Cell().Element(Celda).AlignRight().Text(FormatoMoneda(CalcularTotal(pedido)));
                    }
                });

                var totalGeneral = pedidos.Sum(CalcularTotal);

                column.Item().AlignRight().Text(text =>
                {
                    text.Span($"Pedidos: {pedidos.Count}    ");
                    text.Span($"Total: {FormatoMoneda(totalGeneral)}").SemiBold();
                });
            });
        }

        private void ComponerHeaderComprobante(IContainer container, Pedido pedido)
        {
            container.Column(column =>
            {
                column.Item().Text("E-COMMERCE").FontSize(18).SemiBold();
                column.Item().Text($"Comprobante de pedido #{pedido.IdPedido}").FontSize(14).SemiBold();
                column.Item().Text($"Fecha: {pedido.Fecha:dd/MM/yyyy HH:mm}    Estado: {pedido.EstadoPedido?.Nombre ?? "-"}")
                    .FontColor(Colors.Grey.Darken1);
                column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });
        }

        private void ComponerContenidoComprobante(IContainer container, Pedido pedido)
        {
            var subtotal = CalcularSubtotal(pedido);
            var total = subtotal + pedido.CostoEnvio;
            var pago = pedido.Pagos.OrderByDescending(p => p.IdPago).FirstOrDefault();
            var transferenciaPendiente = pago != null
                && string.Equals(pago.MetodoPago, "Transferencia por QR", StringComparison.OrdinalIgnoreCase)
                && string.Equals(pago.EstadoPago, "Pendiente", StringComparison.OrdinalIgnoreCase);

            container.Column(column =>
            {
                column.Spacing(12);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Cliente").SemiBold();
                        if (pedido.Usuario != null)
                        {
                            col.Item().Text($"{pedido.Usuario.Nombre} {pedido.Usuario.Apellido}");
                            col.Item().Text(pedido.Usuario.Email ?? "-");
                            if (!string.IsNullOrWhiteSpace(pedido.Usuario.PhoneNumber))
                            {
                                col.Item().Text($"Tel.: {pedido.Usuario.PhoneNumber}");
                            }
                        }
                        else
                        {
                            col.Item().Text("Sin datos");
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Dirección de envío").SemiBold();
                        if (pedido.DireccionEnvio != null)
                        {
                            col.Item().Text($"{pedido.DireccionEnvio.Calle} {pedido.DireccionEnvio.Numero}");
                            col.Item().Text($"{pedido.DireccionEnvio.Ciudad}, {pedido.DireccionEnvio.Provincia?.Nombre ?? "-"}");
                            col.Item().Text($"CP: {pedido.DireccionEnvio.CodigoPostal}");
                        }
                        else
                        {
                            col.Item().Text("Sin datos");
                        }
                    });
                });

                column.Item().Text("Productos").SemiBold();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CeldaEncabezado).Text("Producto");
                        header.Cell().Element(CeldaEncabezado).AlignRight().Text("Cantidad");
                        header.Cell().Element(CeldaEncabezado).AlignRight().Text("Precio unit.");
                        header.Cell().Element(CeldaEncabezado).AlignRight().Text("Subtotal");
                    });

                    foreach (var detalle in pedido.DetallesPedido)
                    {
                        table.Cell().Element(Celda).Text(detalle.Producto?.Nombre ?? "-");
                        table.Cell().Element(Celda).AlignRight().Text(detalle.Cantidad.ToString());
                        table.Cell().Element(Celda).AlignRight().Text(FormatoMoneda(detalle.PrecioUnitario));
                        table.Cell().Element(Celda).AlignRight().Text(FormatoMoneda(detalle.Cantidad * detalle.PrecioUnitario));
                    }
                });

                column.Item().AlignRight().Column(col =>
                {
                    col.Item().Text($"Subtotal: {FormatoMoneda(subtotal)}");
                    col.Item().Text($"Envío: {FormatoMoneda(pedido.CostoEnvio)}");
                    col.Item().Text($"Total: {FormatoMoneda(total)}").FontSize(12).SemiBold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Pago").SemiBold();
                        if (pago != null)
                        {
                            col.Item().Text($"Medio: {pago.MetodoPago}");
                            col.Item().Text($"Estado: {pago.EstadoPago}");
                            col.Item().Text($"Monto: {FormatoMoneda(pago.Monto)}");
                            if (pago.FechaPago.HasValue)
                            {
                                col.Item().Text($"Fecha: {pago.FechaPago.Value:dd/MM/yyyy HH:mm}");
                            }
                        }
                        else
                        {
                            col.Item().Text("Sin datos");
                        }
                    });

                    if (transferenciaPendiente)
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Transferencia por QR").SemiBold();
                            col.Item().Text("Realizá la transferencia por el total indicado y el pedido se confirmará al verificar el pago.")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);

                            if (File.Exists(_rutaQr))
                            {
                                col.Item().PaddingTop(4).Image(File.ReadAllBytes(_rutaQr)).FitWidth();
                            }
                        });
                    }
                });
            });
        }

        private static IContainer CeldaEncabezado(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Medium);
        }

        private static IContainer Celda(IContainer container)
        {
            return container
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2);
        }

        private static decimal CalcularSubtotal(Pedido pedido)
        {
            return pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);
        }

        private static decimal CalcularTotal(Pedido pedido)
        {
            return CalcularSubtotal(pedido) + pedido.CostoEnvio;
        }

        private static string FormatoMoneda(decimal monto)
        {
            return "$ " + monto.ToString("N2", Cultura);
        }
    }
}
