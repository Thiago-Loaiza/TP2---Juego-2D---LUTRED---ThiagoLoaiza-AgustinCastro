using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace _1.Progression;

public sealed record RegistroRecord(DateTimeOffset Fecha, int Puntaje);
public enum ResultadoRegistro { NoSupera, Guardado, Error }

public sealed class HistorialRecords
{
    public string RutaArchivo { get; }
    public string Error { get; private set; }
    public bool ArchivoDanado { get; private set; }
    private List<RegistroRecord> _registros = new();
    public IReadOnlyList<RegistroRecord> Registros => _registros.AsReadOnly();

    public HistorialRecords(string rutaArchivo = null)
    {
        RutaArchivo = Path.GetFullPath(rutaArchivo ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LUTRED", "records.json"));
    }

    public void Cargar()
    {
        _registros.Clear();
        Error = null;
        ArchivoDanado = false;
        try
        {
            string contenido;
            try { contenido = File.ReadAllText(RutaArchivo); }
            catch (FileNotFoundException) { return; }
            catch (DirectoryNotFoundException) { return; }
            var registros = JsonSerializer.Deserialize<List<RegistroRecord>>(contenido)
                ?? throw new JsonException("Historial vacio o invalido.");
            int mejor = -1;
            foreach (RegistroRecord registro in registros)
            {
                if (registro == null || registro.Fecha == default || registro.Puntaje < 0
                    || registro.Puntaje > EstadoPuntaje.PuntajeMaximo || registro.Puntaje <= mejor)
                    throw new JsonException("El historial no contiene records consecutivos validos.");
                mejor = registro.Puntaje;
            }
            _registros = registros;
        }
        catch (JsonException)
        {
            ArchivoDanado = true;
            Error = "No se pudo leer el historial de records.";
        }
        catch (IOException) { Error = "No se pudo acceder al historial de records."; }
        catch (UnauthorizedAccessException) { Error = "No se pudo acceder al historial de records."; }
    }

    public ResultadoRegistro GuardarSiSupera(int puntaje, DateTimeOffset? fecha = null)
    {
        if (puntaje < 0 || puntaje > EstadoPuntaje.PuntajeMaximo)
        {
            Error = "El puntaje no es valido.";
            return ResultadoRegistro.Error;
        }
        // Leer de nuevo antes de comparar conserva los records de las partidas anteriores.
        Cargar();
        if (Error != null && !ArchivoDanado) return ResultadoRegistro.Error;
        if (_registros.Count > 0 && puntaje <= _registros[^1].Puntaje) return ResultadoRegistro.NoSupera;
        string temporal = RutaArchivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo));
            if (ArchivoDanado)
            {
                // Conservar el archivo ilegible antes de comenzar un historial nuevo.
                File.Copy(RutaArchivo, RutaArchivo + ".danado-" + Guid.NewGuid().ToString("N") + ".bak");
            }
            var nuevos = new List<RegistroRecord>(_registros) { new(fecha ?? DateTimeOffset.Now, puntaje) };
            string json = JsonSerializer.Serialize(nuevos, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(temporal, json);
            // El archivo anterior solo se sustituye despues de escribir el nuevo completo.
            File.Move(temporal, RutaArchivo, overwrite: true);
            _registros = nuevos;
            Error = null;
            ArchivoDanado = false;
            return ResultadoRegistro.Guardado;
        }
        catch (IOException) { Error = "No se pudo guardar el record."; }
        catch (UnauthorizedAccessException) { Error = "No se pudo guardar el record."; }
        finally
        {
            try { File.Delete(temporal); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return ResultadoRegistro.Error;
    }
}
