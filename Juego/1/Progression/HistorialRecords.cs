using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text;

namespace _1.Progression;

public sealed record RegistroRecord(DateTimeOffset Fecha, int Puntaje, string Nombre = "ANÓNIMO",
    int? NivelLlegado = null);
public enum ResultadoRegistro { Guardado, Error }

public sealed class HistorialRecords
{
    public const int MaximoCaracteresNombre = 20;
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
            for (int i = 0; i < registros.Count; i++)
            {
                RegistroRecord registro = registros[i];
                if (registro == null || registro.Fecha == default || registro.Puntaje < 0
                    || registro.Puntaje > EstadoPuntaje.PuntajeMaximo || registro.NivelLlegado < 0)
                    throw new JsonException("El historial contiene un registro invalido.");
                registros[i] = registro with { Nombre = NormalizarNombre(registro.Nombre) };
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

    public static string NormalizarNombre(string nombre)
    {
        var resultado = new StringBuilder();
        foreach (char caracter in (nombre ?? "").Trim())
        {
            if (!char.IsControl(caracter) && caracter >= ' ' && caracter <= '\u00ff') resultado.Append(caracter);
            if (resultado.Length >= MaximoCaracteresNombre) break;
        }
        string limpio = resultado.ToString().Trim();
        return limpio.Length > 0 ? limpio : "ANÓNIMO";
    }

    public ResultadoRegistro Guardar(int puntaje, string nombre, DateTimeOffset? fecha = null, int? nivelLlegado = null)
    {
        if (puntaje < 0 || puntaje > EstadoPuntaje.PuntajeMaximo)
        {
            Error = "El puntaje no es valido.";
            return ResultadoRegistro.Error;
        }
        if (nivelLlegado < 0)
        {
            Error = "El nivel no es valido.";
            return ResultadoRegistro.Error;
        }
        // Leer de nuevo conserva todas las partidas anteriores, incluidos empates y puntajes menores.
        Cargar();
        if (Error != null && !ArchivoDanado) return ResultadoRegistro.Error;
        string temporal = RutaArchivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo));
            if (ArchivoDanado)
            {
                // Conservar el archivo ilegible antes de comenzar un historial nuevo.
                File.Copy(RutaArchivo, RutaArchivo + ".danado-" + Guid.NewGuid().ToString("N") + ".bak");
            }
            var nuevos = new List<RegistroRecord>(_registros)
            {
                new(fecha ?? DateTimeOffset.Now, puntaje, NormalizarNombre(nombre), nivelLlegado)
            };
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
