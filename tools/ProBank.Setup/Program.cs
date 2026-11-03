using Dapper;
using Microsoft.Data.SqlClient;
using ProBank.Domain.Common;

// ProBank.Setup: crea el administrador local inicial o un usuario con contraseña y roles.
// Uso: dotnet run --project tools/ProBank.Setup -- admin   --usuario admin.sistema --ip 10.20.30.45
//      dotnet run --project tools/ProBank.Setup -- usuario --usuario ana.perez --nombre "Ana Pérez" --roles "Gerencia de TI,Consulta"
//      dotnet run --project tools/ProBank.Setup -- llaves [--keyvault <id de la llave>]   (Always Encrypted; antes de publicar la base)
//      La cadena de conexión se lee de PROBANK_SQL. La clave se pide por consola o, para scripts, de PROBANK_CLAVE.
var comando = args.Length > 0 ? args[0] : "";
if (comando is not ("admin" or "usuario" or "llaves"))
{
    Console.WriteLine("Uso: ProBank.Setup admin --usuario <usuario> --ip <ip-permitida> [--nombre <nombre>]");
    Console.WriteLine("     ProBank.Setup usuario --usuario <usuario> --nombre <nombre> --roles <rol1,rol2> [--correo <correo>]");
    Console.WriteLine("     ProBank.Setup llaves [--keyvault <id de la llave en Key Vault>]  (sin --keyvault: certificado local)");
    return 1;
}
string? Opcional(string n)
{
    var i = Array.IndexOf(args, n);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}
string Arg(string n, string? def = null) => Opcional(n) ?? def ?? throw new ArgumentException($"Falta {n}");

var cs = Environment.GetEnvironmentVariable("PROBANK_SQL") ?? throw new InvalidOperationException("Define PROBANK_SQL.");
if (comando == "llaves") return await Llaves.Crear(cs, Opcional("--keyvault"));

var usuario = Arg("--usuario");

var clave = Environment.GetEnvironmentVariable("PROBANK_CLAVE");
if (string.IsNullOrEmpty(clave))
{
    Console.Write($"Nueva clave (mínimo {Claves.LongitudMinima} caracteres): ");
    clave = LeerOculto();
}
if (Claves.Validar(clave) is { } error) { Console.Error.WriteLine(error); return 2; }

var (hash, sal) = Claves.Generar(clave);
await using var cn = new SqlConnection(cs);
if (comando == "admin")
{
    var ip = Arg("--ip");
    await cn.ExecuteAsync("seg.usp_Usuario_CrearAdmin",
        new { Usuario = usuario, Nombre = Arg("--nombre", "Administrador del sistema"), ClaveHash = hash, ClaveSal = sal, IpPermitida = ip },
        commandType: System.Data.CommandType.StoredProcedure);
    Console.WriteLine($"Administrador {usuario} listo (solo desde {ip}).");
}
else
{
    var roles = Arg("--roles");
    await cn.ExecuteAsync("seg.usp_Usuario_Guardar",
        new { Usuario = usuario, Nombre = Arg("--nombre"), Correo = Opcional("--correo"), ClaveHash = hash, ClaveSal = sal, Roles = roles },
        commandType: System.Data.CommandType.StoredProcedure);
    Console.WriteLine($"Usuario {usuario} listo con roles: {roles}.");
}
return 0;

static string LeerOculto()
{
    var s = new System.Text.StringBuilder();
    while (true)
    {
        var k = Console.ReadKey(intercept: true);
        if (k.Key == ConsoleKey.Enter) { Console.WriteLine(); return s.ToString(); }
        if (k.Key == ConsoleKey.Backspace && s.Length > 0) s.Length--;
        else if (!char.IsControl(k.KeyChar)) s.Append(k.KeyChar);
    }
}
