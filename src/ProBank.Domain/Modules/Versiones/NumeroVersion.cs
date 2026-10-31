namespace ProBank.Domain.Modules.Versiones;

public enum EstadoVersion { Planificada, EnPruebas, Aprobada, Desplegada, Revertida }
public enum Ambiente { Desarrollo, Pruebas, Produccion }

/// <summary>VER-02: versión semántica mayor.menor.parche.</summary>
public readonly record struct NumeroVersion(int Mayor, int Menor, int Parche) : IComparable<NumeroVersion>
{
    public static bool TryParse(string? texto, out NumeroVersion v)
    {
        v = default;
        var p = (texto ?? string.Empty).Trim().TrimStart('v', 'V').Split('.');
        if (p.Length != 3 || !int.TryParse(p[0], out var a) || !int.TryParse(p[1], out var b) || !int.TryParse(p[2], out var c) || a < 0 || b < 0 || c < 0)
            return false;
        v = new NumeroVersion(a, b, c);
        return true;
    }

    public int CompareTo(NumeroVersion o) =>
        Mayor != o.Mayor ? Mayor.CompareTo(o.Mayor) : Menor != o.Menor ? Menor.CompareTo(o.Menor) : Parche.CompareTo(o.Parche);

    public static bool operator <(NumeroVersion a, NumeroVersion b) => a.CompareTo(b) < 0;
    public static bool operator >(NumeroVersion a, NumeroVersion b) => a.CompareTo(b) > 0;
    public static bool operator <=(NumeroVersion a, NumeroVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >=(NumeroVersion a, NumeroVersion b) => a.CompareTo(b) >= 0;
    public override string ToString() => $"{Mayor}.{Menor}.{Parche}";
}

public static class ReglasVersion
{
    /// <summary>VER-04: un despliegue rompe una dependencia si la versión instalada es menor que la mínima.</summary>
    public static bool CumpleDependencia(NumeroVersion instalada, NumeroVersion minima) => instalada >= minima;

    public static bool PuedePasarA(EstadoVersion actual, EstadoVersion destino) => (actual, destino) switch
    {
        (EstadoVersion.Planificada, EstadoVersion.EnPruebas) => true,
        (EstadoVersion.EnPruebas, EstadoVersion.Aprobada) => true,
        (EstadoVersion.Aprobada, EstadoVersion.Desplegada) => true,
        (EstadoVersion.Desplegada, EstadoVersion.Revertida) => true,
        _ => false
    };
}
