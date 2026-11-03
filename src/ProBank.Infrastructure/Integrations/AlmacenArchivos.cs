using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProBank.Application.Abstractions;

namespace ProBank.Infrastructure.Integrations;

/// <summary>Archivos en Azure Blob Storage con la identidad administrada de la API (sin claves de acceso).</summary>
internal sealed class BlobAlmacen(BlobServiceClient blobs) : IAlmacenArchivos
{
    public async Task Guardar(string contenedor, string ruta, Stream contenido, string tipo, CancellationToken ct) =>
        await blobs.GetBlobContainerClient(contenedor).GetBlobClient(ruta)
            .UploadAsync(contenido, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = tipo } }, ct);

    public async Task<Stream?> Abrir(string contenedor, string ruta, CancellationToken ct)
    {
        try { return await blobs.GetBlobContainerClient(contenedor).GetBlobClient(ruta).OpenReadAsync(cancellationToken: ct); }
        catch (RequestFailedException e) when (e.Status == 404) { return null; }
    }

    public async Task Eliminar(string contenedor, string ruta, CancellationToken ct) =>
        await blobs.GetBlobContainerClient(contenedor).GetBlobClient(ruta).DeleteIfExistsAsync(cancellationToken: ct);
}

/// <summary>Para ejecutar en local sin Azure: los archivos van a una carpeta del equipo (Almacen:CarpetaLocal).</summary>
internal sealed class CarpetaAlmacen(string raiz) : IAlmacenArchivos
{
    private string Ruta(string contenedor, string ruta)
    {
        var completa = Path.GetFullPath(Path.Combine(raiz, contenedor, ruta));
        if (!completa.StartsWith(Path.GetFullPath(raiz), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Ruta de archivo no válida.");
        return completa;
    }

    public async Task Guardar(string contenedor, string ruta, Stream contenido, string tipo, CancellationToken ct)
    {
        var destino = Ruta(contenedor, ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        await using var f = File.Create(destino);
        await contenido.CopyToAsync(f, ct);
    }

    public Task<Stream?> Abrir(string contenedor, string ruta, CancellationToken ct)
    {
        var origen = Ruta(contenedor, ruta);
        return Task.FromResult<Stream?>(File.Exists(origen) ? File.OpenRead(origen) : null);
    }

    public Task Eliminar(string contenedor, string ruta, CancellationToken ct)
    {
        File.Delete(Ruta(contenedor, ruta));
        return Task.CompletedTask;
    }
}
