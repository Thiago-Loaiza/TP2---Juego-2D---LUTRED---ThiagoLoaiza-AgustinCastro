using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace _1.Progression;

public static class FiltroNombreRecord
{
    // Las palabras cortas o ambiguas se comprueban completas para no bloquear nombres legitimos.
    private static readonly HashSet<string> Palabras = new(StringComparer.Ordinal)
    {
        "PUTA", "PUTO", "PUTAS", "PUTOS", "PUTON", "PUTONA", "PERRA", "PERRAS", "MIERDA", "MIERDAS",
        "JODER", "JODIDO", "JODIDA", "COJON", "COJONES", "CULO", "CULOS", "CABRON", "CABRONA", "CABRONES",
        "IMBECIL", "IDIOTA", "ESTUPIDO", "ESTUPIDA", "GILIPOLLAS", "PELOTUDO", "PELOTUDA", "BOLUDO", "BOLUDA",
        "FORRO", "FORRA", "TARADO", "TARADA", "PENDEJO", "PENDEJA", "MAMON", "MAMONA", "PITO", "VERGA",
        "CHINGAR", "CHINGADO", "CHINGADA", "PINCHE", "CARACULO", "ORTO", "CONCHA",
        "TROLO", "MARICON", "MARICA", "MOGOLICO", "MOGOLICA", "ZORRA", "ZORRAS", "CAGADA", "CAGAR",
        "FUCK", "FUCKS", "FUCKER", "FUCKING", "SHIT", "SHITS", "SHITTY", "BITCH", "BITCHES", "DICK", "DICKS",
        "ASS", "ASSES", "ASSHOLE", "BASTARD", "BASTARDS", "CUNT", "CUNTS", "PISS", "PISSED", "CRAP",
        "DAMN", "WHORE", "WHORES", "SLUT", "SLUTS", "COCK", "COCKS", "PUSSY", "PUSSIES", "TWAT", "WANKER",
        "FAG", "FAGGOT", "NIGGER", "NIGGA", "RETARD", "RETARDED", "DUMBASS", "JACKASS"
    };

    // Raices ofensivas inequívocas y frases frecuentes, incluso unidas a un apodo.
    private static readonly string[] Fragmentos =
    {
        "HIJODEPUTA", "HIJADEPUTA", "HDPUTA", "MALPARIDO", "MALPARIDA", "PELOTUD", "GILIPOLLAS", "BOLUD",
        "CONCHATUMADRE", "CONCHETUMARE", "LACONCHA", "CHUPAPIJA", "CHUPAVERGA", "CHUPAMELA", "PUTAMADRE",
        "PENDEJ", "MARICON", "MOGOLIC", "FUCK", "SHIT", "BITCH", "ASSHOLE", "MOTHERFUCK", "DICKHEAD",
        "COCKSUCKER", "DUMBASS", "JACKASS", "FAGGOT", "NIGGER", "NIGGA", "CUNT", "WHORE", "WANKER"
    };

    public static bool ContieneMalasPalabras(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return false;
        var limpio = new StringBuilder();
        var palabra = new StringBuilder();
        var palabraLiteral = new StringBuilder();
        foreach (char original in nombre.Normalize(NormalizationForm.FormKD).ToUpperInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(original) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetter(original)) palabraLiteral.Append(original);
            else
            {
                if (Palabras.Contains(palabraLiteral.ToString())) return true;
                palabraLiteral.Clear();
            }
            char c = original switch
            {
                '0' => 'O', '1' => 'I', '3' => 'E', '4' => 'A', '5' => 'S', '7' => 'T', '8' => 'B',
                _ => original
            };
            if (char.IsLetter(c)) { palabra.Append(c); limpio.Append(c); }
            else
            {
                if (Palabras.Contains(palabra.ToString())) return true;
                palabra.Clear();
            }
        }
        if (Palabras.Contains(palabra.ToString()) || Palabras.Contains(palabraLiteral.ToString())) return true;
        string compacto = limpio.ToString();
        if (Palabras.Contains(compacto)) return true;
        foreach (string fragmento in Fragmentos)
            if (compacto.Contains(fragmento, StringComparison.Ordinal)) return true;
        return false;
    }
}
