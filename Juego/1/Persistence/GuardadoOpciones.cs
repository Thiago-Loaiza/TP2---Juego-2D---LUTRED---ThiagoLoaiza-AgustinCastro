using System;
using System.IO;
using System.Text.Json;

namespace _1.Persistence;

public sealed record DatosOpciones(bool AlternarCorrer = false, bool SilenciarMusica = false,
    int VolumenMusica = 50, int VolumenSfx = 50);

public sealed class GuardadoOpciones
{
    public string RutaArchivo { get; }
    public string Error { get; private set; }
    private bool _archivoDanado;

    public GuardadoOpciones(string rutaArchivo = null) => RutaArchivo = Path.GetFullPath(rutaArchivo
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LUTRED", "opciones.json"));

    public DatosOpciones Cargar()
    {
        Error = null;
        _archivoDanado = false;
        try
        {
            var datos = JsonSerializer.Deserialize<DatosOpciones>(File.ReadAllText(RutaArchivo))
                ?? throw new JsonException("Opciones invalidas.");
            return datos with
            {
                VolumenMusica = Math.Clamp(datos.VolumenMusica, 0, 100),
                VolumenSfx = Math.Clamp(datos.VolumenSfx, 0, 100)
            };
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        catch (JsonException) { _archivoDanado = true; Error = "No se pudieron cargar las opciones."; }
        catch (IOException) { Error = "No se pudieron cargar las opciones."; }
        catch (UnauthorizedAccessException) { Error = "No se pudieron cargar las opciones."; }
        return new DatosOpciones();
    }

    public bool Guardar(DatosOpciones datos)
    {
        string temporal = RutaArchivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo));
            if (_archivoDanado)
                File.Copy(RutaArchivo, RutaArchivo + ".danado-" + Guid.NewGuid().ToString("N") + ".bak");
            File.WriteAllText(temporal, JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporal, RutaArchivo, overwrite: true);
            _archivoDanado = false;
            Error = null;
            return true;
        }
        catch (IOException) { Error = "No se pudieron guardar las opciones."; }
        catch (UnauthorizedAccessException) { Error = "No se pudieron guardar las opciones."; }
        finally
        {
            try { File.Delete(temporal); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return false;
    }
}
