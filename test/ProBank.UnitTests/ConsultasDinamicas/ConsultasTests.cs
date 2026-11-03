using ProBank.Application.Modules.ConsultasDinamicas;
using ProBank.Contracts.ConsultasDinamicas;
using ProBank.Domain.Modules.ConsultasDinamicas;

namespace ProBank.UnitTests.ConsultasDinamicas;

public sealed class ConsultasTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 3);

    private static readonly ParametroConsulta[] Definicion =
    [
        new("@Desde", "Desde", TipoParametro.Fecha, true, "Inicio del mes anterior"),
        new("@Hasta", "Hasta", TipoParametro.Fecha, true, "Fin del mes anterior"),
        new("@Anio", "Año", TipoParametro.Numero, false, null),
        new("@Estado", "Estado", TipoParametro.Lista, false, "Todos", "Todos, Activo, Anulado"),
    ];

    [Theory(DisplayName = "SQL-01 · solo un procedimiento esquema.nombre, nunca texto SQL")]
    [InlineData("rpt.usp_SaldosPorProducto", true)]
    [InlineData("usp_Saldos", false)]
    [InlineData("rpt.usp_X; DROP TABLE seg.Usuario", false)]
    [InlineData("rpt.usp_X --", false)]
    [InlineData("SELECT * FROM seg.Usuario", false)]
    public void Procedimiento(string nombre, bool valido) => Assert.Equal(valido, Consulta.EsProcedimientoValido(nombre));

    [Fact(DisplayName = "SQL-01 · solo se ejecuta una consulta publicada, por sus roles y en sus bases")]
    public void PuedeEjecutar()
    {
        var c = new Consulta { Codigo = "QRY-001", Procedimiento = "rpt.usp_X", Publicada = true, RolesPermitidos = ["Gerencia de TI"], ConexionesPermitidas = ["CNX-001"] };
        Assert.True(c.PuedeEjecutar("Gerencia de TI", "CNX-001").IsSuccess);
        Assert.True(c.PuedeEjecutar("Administrador", "CNX-001").IsSuccess);
        Assert.Equal("forbidden", c.PuedeEjecutar("Marketing", "CNX-001").Error.Code);
        Assert.Equal("conexion", c.PuedeEjecutar("Gerencia de TI", "CNX-002").Error.Code);
        Assert.Equal("no_publicada", new Consulta { Procedimiento = "rpt.usp_X" }.PuedeEjecutar("Administrador", "CNX-001").Error.Code);
    }

    [Fact(DisplayName = "SQL-04 · las variables se resuelven al ejecutar y quedan con su valor en el texto auditable")]
    public void Variables()
    {
        var r = ResolverParametros.Resolver(Definicion, [], Hoy);
        Assert.True(r.IsSuccess);
        Assert.Equal(new DateOnly(2026, 10, 1), r.Value.Valores["@Desde"]);
        Assert.Equal(new DateOnly(2026, 10, 31), r.Value.Valores["@Hasta"]);
        Assert.Null(r.Value.Valores["@Anio"]);
        Assert.Equal("Desde=Inicio del mes anterior → 01/10/2026; Hasta=Fin del mes anterior → 31/10/2026; Año=(vacío); Estado=Todos", r.Value.Texto);
        // Lo que se guarda para ejecutar después de aprobar son los valores ya resueltos
        var guardados = ResolverParametros.DesdeJson(Definicion, r.Value.Json);
        Assert.Equal(new DateOnly(2026, 10, 1), guardados["@Desde"]);
        Assert.Equal("Todos", guardados["@Estado"]);
    }

    [Fact(DisplayName = "SQL-04 · valores fijos, variables enviadas y validaciones de tipo, obligatorio y lista")]
    public void ValoresEnviados()
    {
        var r = ResolverParametros.Resolver(Definicion,
            [new("@Desde", "2026-09-15", null), new("@Hasta", null, "Hoy"), new("@Anio", null, "Año anterior"), new("@Estado", "Activo", null)], Hoy);
        Assert.True(r.IsSuccess);
        Assert.Equal(new DateOnly(2026, 9, 15), r.Value.Valores["@Desde"]);
        Assert.Equal(Hoy, r.Value.Valores["@Hasta"]);
        Assert.Equal(2025m, r.Value.Valores["@Anio"]);
        // Si el usuario borra un obligatorio, no se toma su valor por defecto: se rechaza
        Assert.Equal("parametro", ResolverParametros.Resolver(Definicion, [new("@Desde", "", null)], Hoy).Error.Code);
        Assert.Equal("parametro", ResolverParametros.Resolver(Definicion, [new("@Estado", "Otro", null)], Hoy).Error.Code);
        Assert.Equal("parametro", ResolverParametros.Resolver(Definicion, [new("@Anio", "dos mil", null)], Hoy).Error.Code);
        Assert.Equal("parametro", ResolverParametros.Resolver([new ParametroConsulta("@Cuenta", "Cuenta", TipoParametro.Texto, true, null)], [], Hoy).Error.Code);
    }

    [Theory(DisplayName = "SQL-01 · el control del parámetro sale de su tipo SQL")]
    [InlineData("date", TipoParametro.Fecha)]
    [InlineData("datetime2", TipoParametro.Fecha)]
    [InlineData("decimal", TipoParametro.Numero)]
    [InlineData("int", TipoParametro.Numero)]
    [InlineData("nvarchar", TipoParametro.Texto)]
    public void TipoDesdeSql(string sql, TipoParametro tipo) => Assert.Equal(tipo, ReglasConsultas.TipoDesdeSql(sql));

    [Fact(DisplayName = "SQL-04 · el valor por defecto corresponde al control; una lista necesita opciones")]
    public void ConfigurarParametro()
    {
        Assert.Null(ReglasConsultas.ValidarParametro(new("@D", "Desde", TipoParametro.Fecha, true, "Fin de mes")));
        Assert.Null(ReglasConsultas.ValidarParametro(new("@D", "Desde", TipoParametro.Fecha, true, "01/10/2026")));
        Assert.NotNull(ReglasConsultas.ValidarParametro(new("@D", "Desde", TipoParametro.Fecha, true, "Año actual")));
        Assert.Null(ReglasConsultas.ValidarParametro(new("@A", "Año", TipoParametro.Numero, true, "Año actual")));
        Assert.Equal("opciones", ReglasConsultas.ValidarParametro(new("@E", "Estado", TipoParametro.Lista, false, null))!.Code);
        Assert.NotNull(ReglasConsultas.ValidarParametro(new("@E", "Estado", TipoParametro.Lista, false, "Otro", "Todos,Activo")));
    }

    [Fact(DisplayName = "SQL-02 · la base guarda solo el nombre del secreto de Key Vault")]
    public void Conexion()
    {
        Assert.Null(ReglasConsultas.ValidarConexion("Core", "sql.banco.local", "Core", "Usuario SQL de solo lectura", "cnx-core", "Activo"));
        Assert.Equal("secreto", ReglasConsultas.ValidarConexion("Core", "sql", "Core", "Usuario SQL de solo lectura", "Server=x;Password=y", "Activo")!.Code);
    }

    [Fact(DisplayName = "SQL-07 y SQL-08 · semanal con días, formato válido y correos de destino")]
    public void Programacion()
    {
        Assert.Null(ReglasConsultas.ValidarProgramacion("Semanal", [1, 3], null, "xlsx", ["a@banco.ec"]));
        Assert.Equal("dias", ReglasConsultas.ValidarProgramacion("Semanal", [], null, "xlsx", ["a@banco.ec"])!.Code);
        Assert.Equal("formato", ReglasConsultas.ValidarProgramacion("Diaria", [], null, "docx", ["a@banco.ec"])!.Code);
        Assert.Equal("destinatarios", ReglasConsultas.ValidarProgramacion("Diaria", [], null, "csv", ["no-es-correo"])!.Code);
        Assert.Equal(new DateTime(2026, 11, 4, 8, 0, 0),
            CalendarioProgramacion.Proxima(Frecuencia.Semanal, new TimeOnly(8, 0), [DayOfWeek.Wednesday], null, new DateTime(2026, 11, 3, 9, 0, 0)));
    }

    [Fact(DisplayName = "SQL-05 · CSV con encabezado, separador ; y fechas dd/mm/aaaa")]
    public void Csv()
    {
        var r = new ResultadoEjecucion(["Cuenta", "Fecha", "Saldo"], ["texto", "fecha", "numero"],
            [new Dictionary<string, object?> { ["Cuenta"] = "22\"01", ["Fecha"] = new DateTime(2026, 10, 31), ["Saldo"] = 1500.5m }], 10, false);
        var texto = System.Text.Encoding.UTF8.GetString(new ExportadorCsv().Exportar(r, "x")).TrimStart('﻿');
        Assert.Equal("\"Cuenta\";\"Fecha\";\"Saldo\"\r\n\"22\"\"01\";\"31/10/2026\";\"1500.50\"\r\n", texto);
    }
}
