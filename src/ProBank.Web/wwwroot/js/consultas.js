// Consultas dinámicas: piezas comunes de sus pantallas (formulario de ejecución con valores fijos o variables, ventana de
// resultados con exportación). Reglas SQL-01 a SQL-08 en spec/modulos/consultas-dinamicas.md.
(function () {
  "use strict";
  const url = (a, q) => "/ConsultasDinamicas/Datos/" + a + (q ? "?" + q : "");
  const FIJA = "Fecha fija", FIJO = "Valor fijo";
  const FORMATOS = [{ id: "xlsx", text: "Excel (.xlsx)", icon: "exportxlsx" }, { id: "csv", text: "CSV", icon: "export" }, { id: "pdf", text: "PDF", icon: "exportpdf" },
                    { id: "json", text: "JSON", icon: "codeblock" }, { id: "txt", text: "Texto delimitado", icon: "textdocument" }];
  const iso = d => d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0");
  const deDdMm = s => { const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(s || ""); return m ? new Date(+m[3], +m[2] - 1, +m[1]) : null; };
  const filas = n => n.toLocaleString("es-EC") + (n === 1 ? " fila" : " filas");
  // Corte posible tras . _ \ para que nombres largos (rpt.usp_Pagos_PorProveedor, (localdb)\MSSQLLocalDB) no se monten en la columna vecina
  const corte = s => String(s == null ? "" : s).replace(/([._\\])/g, "$1​");
  const aprobacionTexto = ap => ap.length ? "Requiere aprobación: " + (ap.length > 1 ? "deben aprobar todos (" + ap.join(", ") + ")" : ap[0]) +
    ". Se registra la solicitud y la ejecuta usted cuando esté aprobada." : "No requiere aprobación: se ejecuta al confirmar.";

  // ---------- Parámetros: un control por tipo; fechas y números pueden tomar una variable que se resuelve al ejecutar ----------
  // Devuelve los items del dxForm y una función que arma [{ nombre, valor, variable }] para la API.
  function camposParametros(params, variables, data, form) {
    const vars = t => variables.filter(v => v.tipo === t);
    const items = [], lectores = [];
    params.forEach((p, k) => {
      const etiqueta = p.etiqueta + " (" + p.nombre + ")", def = p.valorDefecto || "";
      if (p.tipo === "Fecha" || p.tipo === "Numero") {
        const vs = vars(p.tipo), fijo = p.tipo === "Fecha" ? FIJA : FIJO, esVar = vs.some(v => v.nombre === def);
        const resolver = n => { const v = vs.find(x => x.nombre === n); return v ? (p.tipo === "Fecha" ? deDdMm(v.valor) : +v.valor) : null; };
        data["v" + k] = esVar ? def : fijo;
        data["p" + k] = esVar ? resolver(def) : p.tipo === "Fecha" ? (deDdMm(def) || (p.obligatorio ? new Date() : null)) : (def !== "" ? +def : null);
        items.push({ dataField: "v" + k, label: { text: etiqueta }, editorType: "dxSelectBox", editorOptions: { items: [fijo].concat(vs.map(v => v.nombre)),
          onValueChanged: e => { const f = e.value === fijo, ed = form().getEditor("p" + k); if (!f) form().updateData("p" + k, resolver(e.value)); if (ed) ed.option("readOnly", !f); } } });
        items.push({ dataField: "p" + k, label: { text: p.tipo === "Fecha" ? "Fecha resultante" : "Valor resultante" }, isRequired: p.obligatorio,
          editorType: p.tipo === "Fecha" ? "dxDateBox" : "dxNumberBox",
          editorOptions: p.tipo === "Fecha" ? { displayFormat: "dd/MM/yyyy", readOnly: esVar, showClearButton: !p.obligatorio } : { readOnly: esVar, showClearButton: !p.obligatorio } });
        lectores.push(() => {
          const modo = data["v" + k], v = data["p" + k];
          if (modo !== fijo) return { nombre: p.nombre, valor: null, variable: modo };
          return { nombre: p.nombre, valor: v == null || v === "" ? null : p.tipo === "Fecha" ? iso(v instanceof Date ? v : new Date(v)) : String(v), variable: null };
        });
        return;
      }
      const opciones = (p.opciones || "").split(",").map(s => s.trim()).filter(Boolean);
      data["p" + k] = def || (p.tipo === "Lista" && p.obligatorio ? opciones[0] : "");
      items.push(p.tipo === "Lista"
        ? { dataField: "p" + k, label: { text: etiqueta }, colSpan: 2, isRequired: p.obligatorio, editorType: "dxSelectBox", editorOptions: { items: opciones, showClearButton: !p.obligatorio } }
        : { dataField: "p" + k, label: { text: etiqueta }, colSpan: 2, isRequired: p.obligatorio, editorOptions: { maxLength: 200 } });
      lectores.push(() => ({ nombre: p.nombre, valor: data["p" + k] || null, variable: null }));
    });
    return { items, valores: () => lectores.map(l => l()) };
  }

  // ---------- Ejecutar una consulta publicada (SQL-01 a SQL-04) ----------
  async function ejecutar(codigo, alTerminar) {
    const pubs = await pb.get(url("Disponibles"));
    if (!pubs) return;
    if (!pubs.length) return pb.notify("No hay consultas publicadas para su rol activo (" + pb.sesion.rolActivo + ")", "warning");
    if (codigo && !pubs.some(q => q.codigo === codigo)) return pb.notify("La consulta " + codigo + " no está publicada o su rol activo no puede ejecutarla", "warning");
    const data = { q: codigo || null, db: null };
    let form = null, def = null, campos = null;
    const items = () => {
      const base = [
        { dataField: "q", label: { text: "Consulta" }, editorType: "dxSelectBox", isRequired: true, editorOptions: {
          dataSource: pubs.map(q => ({ k: q.codigo, t: q.codigo + " · " + q.nombre })), valueExpr: "k", displayExpr: "t", searchEnabled: true,
          onValueChanged: async e => { data.q = e.value; await cargar(); form.option("items", items()); } } },
        { dataField: "db", label: { text: "Base de datos" }, editorType: "dxSelectBox", isRequired: true,
          editorOptions: { items: def ? def.conexiones : [], valueExpr: "codigo", displayExpr: "nombre" } }];
      if (!def) return base;
      return base.concat([{ colSpan: 2, template: () => $('<div class="mdtitle" style="margin:4px 0 0">').text("Parámetros de " + def.procedimiento) }])
        .concat(campos.items.length ? campos.items : [{ colSpan: 2, template: () => $('<p class="muted" style="margin:0">').text("El procedimiento no tiene parámetros.") }])
        .concat([{ colSpan: 2, template: () => $('<p class="muted" style="margin:0">').text(aprobacionTexto(def.aprobadores) +
          " Tiempo máximo " + def.tiempoMaximoSeg + " s · hasta " + def.limiteFilas.toLocaleString("es-EC") + " filas.") }]);
    };
    async function cargar() {
      def = null; campos = null;
      Object.keys(data).filter(k => /^[pv]\d+$/.test(k)).forEach(k => delete data[k]);
      if (!data.q) return;
      def = await pb.get(url("ParaEjecutar", "codigo=" + encodeURIComponent(data.q)));
      if (!def) return;
      data.db = def.conexiones.length === 1 ? def.conexiones[0].codigo : null;
      campos = camposParametros(def.parametros, def.variables, data, () => form);
    }
    await cargar();
    pb.ui.openPopup("Ejecutar consulta", $f => {
      form = $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, colCountByScreen: { lg: 2, md: 2, sm: 1, xs: 1 }, items: items() }).dxForm("instance");
      return form;
    }, async () => {
      if (!def) return false;
      if (!form.validate().isValid) return false;
      const r = await pb.post(url("Ejecutar"), { consultaCodigo: data.q, conexion: data.db, parametros: campos.valores() });
      if (r === undefined) return false;
      if (alTerminar) alTerminar();
      if (r.estado === "Pendiente de aprobación") { pb.notify("Ejecución " + r.ejecucion + " registrada: espera la aprobación de " + def.aprobadores.join(", ")); return true; }
      setTimeout(() => resultados(r, def.codigo + " · " + def.nombre), 300);
      return true;
    }, "Ejecutar", 680, { icono: "runner" });
  }

  // ---------- Resultados con grilla y exportación (SQL-05) ----------
  function resultados(r, titulo) {
    const dt = t => ({ numero: "number", entero: "number", fecha: "date", fechahora: "datetime", bool: "boolean" }[t] || "string");
    const fmt = t => ({ numero: "#,##0.00", entero: "#,##0", fecha: "dd/MM/yyyy", fechahora: "dd/MM/yyyy HH:mm" }[t]);
    const exportar = f => { window.location.href = url("Exportar", "codigo=" + encodeURIComponent(r.ejecucion) + "&formato=" + f); pb.notify("Exportando " + r.ejecucion + " a " + f.toUpperCase()); };
    pb.ui.openPopup("Resultados · " + r.ejecucion + (titulo ? " · " + titulo : ""), $f => {
      $f.append($('<div class="chips">').append([filas(r.filas.length), "Duración " + (r.duracionMs / 1000).toFixed(1).replace(".", ",") + " s"]
        .concat(r.truncado ? ["Truncado al límite de filas"] : []).map((t, i) => $("<span>").addClass(i === 2 ? "pill pill-a" : "pill pill-b").css("margin", "0 6px 6px 0").text(t))));
      $f.append($('<p class="muted" style="margin:0 0 8px">').text("Parámetros: " + (r.parametrosResueltos || "sin parámetros")));
      const numericas = r.columnas.map((c, i) => ({ c, i })).filter(x => r.tipos[x.i] === "numero");
      $("<div>").appendTo($f).dxDataGrid({
        dataSource: r.filas, showBorders: true, showRowLines: true, columnAutoWidth: true, allowColumnResizing: true, height: 400, rowAlternationEnabled: true,
        filterRow: { visible: true }, headerFilter: { visible: true }, searchPanel: { visible: true, width: 220, placeholder: "Buscar en resultados…" },
        groupPanel: { visible: !pb.isPhone(), emptyPanelText: "Arrastre una columna para agrupar" }, paging: { pageSize: 20 }, pager: { showInfo: true, infoText: "Página {0} de {1} ({2} filas)" },
        noDataText: "La consulta no devolvió filas.",
        // Las fechas llegan como texto ISO: se convierten para que la grilla las muestre, ordene y filtre como fechas
        columns: r.columnas.map((c, i) => ({ name: "c" + i, caption: c, dataType: dt(r.tipos[i]), format: fmt(r.tipos[i]),
          calculateCellValue: d => (r.tipos[i] === "fecha" || r.tipos[i] === "fechahora") && d[c] ? new Date(d[c]) : d[c] })),
        summary: { totalItems: (r.columnas.length ? [{ column: "c0", summaryType: "count", customizeText: e => filas(e.value) }] : [])
          .concat(numericas.filter(x => x.i > 0).map(x => ({ column: "c" + x.i, summaryType: "sum", valueFormat: "#,##0.00", displayFormat: "{0}" }))) },
        toolbar: { items: ["groupPanel", pb.can("ejecuciones", "exportar") ? { location: "after", widget: "dxDropDownButton", options: {
          text: "Exportar", icon: "export", displayExpr: "text", keyExpr: "id", useSelectMode: false, dropDownOptions: { width: 190 }, items: FORMATOS,
          elementAttr: { "aria-label": "Exportar resultados" }, onItemClick: e => exportar(e.itemData.id) } } : null, "searchPanel"].filter(Boolean) }
      });
      return null;
    }, () => true, "Cerrar", 1040);
  }

  async function verResultado(codigo, titulo) {
    const r = await pb.get(url("Resultado", "codigo=" + encodeURIComponent(codigo)));
    if (r) resultados(r, titulo);
  }

  window.cq = { url, iso, deDdMm, filas, corte, FORMATOS, camposParametros, ejecutar, resultados, verResultado, aprobacionTexto };
})();
