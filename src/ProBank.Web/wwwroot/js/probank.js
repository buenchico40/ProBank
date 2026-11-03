// ProBank: cabecera, menú, pie y componentes comunes con DevExtreme, iguales a la maqueta (prototype/ProBank_DevExtreme.html).
// Las vistas esperan a pb.listo (perfil y permisos del rol activo) antes de dibujar.
(function () {
  "use strict";
  const S = JSON.parse(document.getElementById("pb-sesion")?.textContent || "{}");

  // Menú del prototipo. "url" apunta a la pantalla real; las opciones aún no construidas abren su ficha de módulo.
  const MENU = [
    { id: "seg", title: "Seguridad", icon: "lock", items: [["usuarios", "Usuarios", "/Seguridad/Usuarios"], ["roles", "Roles y permisos", "/Seguridad/Roles"], ["params", "Parámetros de acceso", "/Seguridad/Parametros"], ["auditoria", "Auditoría", "/Seguridad/Auditoria"]] },
    { id: "coa", title: "Coactivas", icon: "money", items: [["oficios", "Oficios", "/Coactivas/Oficios"], ["coactivas", "Coactivas", "/Coactivas/Coactivas"], ["clientes", "Coactivas por cliente", "/Coactivas/Clientes"]] },
    { id: "pry", title: "Portafolio", icon: "folder", items: [["proyectos", "Proyectos, mejoras y bugs", "/Portafolio/Proyectos"], ["gantt", "Cronograma (Gantt)", "/Portafolio/Cronograma"],
      ["tareas", "Mis tareas", "/Portafolio/Tareas"], ["aprobaciones", "Aprobaciones", "/Portafolio/Aprobaciones"], ["acuerdos", "Acuerdos semanales", "/Portafolio/Acuerdos"],
      ["informes", "Informes de seguimiento", "/Portafolio/Informes"], ["paramproy", "Parametrización", "/Portafolio/Parametrizacion"]] },
    { id: "com", title: "Presupuesto", icon: "cart", items: [["presupuesto", "Presupuesto por área", "/Presupuesto/Partidas"], ["movimientos", "Ajustes y movimientos", "/Presupuesto/Movimientos"], ["renovaciones", "Renovaciones de servicios", "/Presupuesto/Renovaciones"], ["solicitudes", "Solicitudes de compra", "/Presupuesto/Solicitudes"], ["pagos", "Pagos", "/Presupuesto/Pagos"], ["areaspres", "Áreas del presupuesto", "/Presupuesto/Areas"]] },
    { id: "cum", title: "Cumplimiento y auditoría", icon: "taskcomplete", items: [["controles", "Controles regulatorios"], ["auditorias", "Auditorías"], ["hallazgos", "Hallazgos y planes de acción"]] },
    { id: "cred", title: "Bóveda", icon: "key", items: [["grupos", "Grupos de acceso", "/Boveda/Grupos"], ["certificados", "Certificados digitales", "/Boveda/Certificados"], ["cuentas", "Cuentas técnicas", "/Boveda/Cuentas"], ["secretos", "Secretos", "/Boveda/Secretos"]] },
    { id: "mkt", title: "Marketing", icon: "image", items: [["promos", "Imágenes promocionales", "/Marketing/Promociones"], ["comunicados", "Comunicados", "/Marketing/Comunicados"]] },
    { id: "sql", title: "Consultas dinámicas", icon: "datafield", items: [["consultas", "Catálogo de consultas", "/ConsultasDinamicas/Catalogo"], ["ejecuciones", "Ejecuciones y aprobaciones", "/ConsultasDinamicas/Ejecuciones"], ["audejec", "Auditoría de ejecuciones", "/ConsultasDinamicas/Auditoria"], ["conexiones", "Bases de datos", "/ConsultasDinamicas/Conexiones"]] },
    { id: "rep", title: "Reportes", icon: "chart", items: [["reportes", "Reportería por módulo"], ["catreportes", "Catálogo de reportes"]] },
    { id: "cfg", title: "Configuración", icon: "preferences", items: [["config", "Parámetros generales", "/Configuracion/Parametros"]] }
  ];
  const urlDe = it => it[2] || "/Home/Opcion?clave=" + it[0];

  const PILL = {
    "pill pill-g": ["Ejecutada", "Publicada", "Exitosa", "Registrada", "Al día", "Cumple", "Vigente", "Renovado", "Publicado", "Habilitado", "Activo", "Aprobada", "Completado", "Completada", "Clasificado", "Pagado", "Sí", "Transferido", "Exitoso", "Cumplido", "A tiempo", "Presupuestado", "Finalizado", "Atendido", "Transferida", "Ejecutado", "Total"],
    "pill pill-a": ["Pendiente de aprobación", "Próxima a evaluar", "Cumple parcialmente", "Medio", "Abierto", "Por rotar", "Por vencer", "Pendiente", "En curso", "EnCurso", "En revisión", "EnRevision", "Sin clasificar", "Parcial", "Bloqueado", "Bloqueo parcial", "No presupuestado", "Clave temporal", "Parcialmente atendido"],
    "pill pill-r": ["Fallida", "Anulada", "Evaluación vencida", "No cumple", "Alto", "Vencido", "Rotación vencida", "Retirado", "Inactivo", "Anulado", "Rechazada", "No", "Sin bloqueo", "Fallido", "Vencida", "No permitido"],
    "pill pill-b": ["Bajo", "Planificada", "Cerrada", "Requerimiento enviado", "Cerrado", "Borrador", "Liberado", "Programado", "Levantada", "Transferido y liberado"]
  };
  const pillCls = t => { for (const k in PILL) if (PILL[k].indexOf(t) >= 0) return k; return ""; };
  const esc = s => String(s == null ? "" : s).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const pad = n => String(n).padStart(2, "0");
  // "yyyy-MM-dd" (fechas sin hora) se toma como fecha local; el navegador la leería en UTC y la correría un día.
  const aFecha = v => v instanceof Date ? v : typeof v === "string" && /^\d{4}-\d{2}-\d{2}$/.test(v) ? new Date(+v.slice(0, 4), +v.slice(5, 7) - 1, +v.slice(8, 10)) : new Date(v);
  const fecha = v => { if (!v) return "—"; const d = aFecha(v); return isNaN(d) ? String(v) : pad(d.getDate()) + "/" + pad(d.getMonth() + 1) + "/" + d.getFullYear(); };
  const fechaHora = v => { if (!v) return "—"; const d = aFecha(v); return isNaN(d) ? String(v) : fecha(v) + " " + pad(d.getHours()) + ":" + pad(d.getMinutes()); };
  // Montos siempre en moneda: $ 1.500,00 (COA-05)
  const dinero = v => { if (v == null || v === "") return "—"; const n = Number(v), p = Math.abs(n).toFixed(2).split("."); return (n < 0 ? "-" : "") + "$ " + p[0].replace(/\B(?=(\d{3})+(?!\d))/g, ".") + "," + p[1]; };
  const hm = () => { const d = new Date(); return pad(d.getHours()) + ":" + pad(d.getMinutes()); };
  const isPhone = () => window.matchMedia("(max-width: 640px)").matches;

  function notify(msg, type) {
    const la = document.getElementById("lastAction"); if (la) la.textContent = hm() + " · " + msg;
    DevExpress.ui.notify({ message: msg, type: type || "success", displayTime: 3500, width: 360, maxWidth: "90vw" }, { position: "top right", direction: "down-push" });
  }

  async function leer(r) {
    const txt = await r.text();
    let json = null; try { json = txt ? JSON.parse(txt) : null; } catch { json = txt; }
    if (r.status === 401) { window.location.href = "/Cuenta/Expirada"; return undefined; }
    if (r.status === 403 && json && json.title === "cambiar_clave") { cambiarClave(true); return undefined; }
    if (!r.ok) { notify((json && (json.detail || json.title)) || ("Error " + r.status), "error"); return undefined; }
    return json;
  }
  const enviar = (metodo, url, body) => fetch(url, { method: metodo, headers: { "Content-Type": "application/json", RequestVerificationToken: pb.token() }, body: JSON.stringify(body || {}) }).then(leer);

  // ---------- Botones y ventanas (mismo estilo del prototipo) ----------
  function button($el, o) {
    return $el.dxButton({ text: o.text, icon: o.icon, type: o.type || "normal", stylingMode: "contained", disabled: !!o.disabled,
      elementAttr: o.aria ? { "aria-label": o.aria } : undefined, hint: o.hint, onClick: o.onClick }).dxButton("instance");
  }
  let popup = null;
  // onSave puede devolver una promesa; si resuelve false, la ventana sigue abierta. opciones.forzado: sin cerrar ni cancelar.
  function openPopup(title, build, onSave, saveText, width, opciones) {
    opciones = opciones || {};
    if (popup) { popup.dispose(); $("#dlgPopup").remove(); }
    const $p = $('<div id="dlgPopup">').appendTo("body");
    let form = null;
    const cerrar = saveText === "Cerrar";
    popup = $p.dxPopup({
      title: title, visible: true, width: width || 520, maxWidth: "94vw", height: "auto", showCloseButton: !opciones.forzado, dragEnabled: false, hideOnOutsideClick: false,
      contentTemplate: c => { form = build($("<div>").appendTo(c)); },
      toolbarItems: (opciones.extra || []).map(b => ({ widget: "dxButton", toolbar: "bottom", location: "after", options: Object.assign({ stylingMode: "contained" }, b) })).concat([
        { widget: "dxButton", toolbar: "bottom", location: "after", visible: !cerrar && !opciones.forzado, options: { text: "Cancelar", icon: "close", stylingMode: "contained", onClick: () => popup.hide() } },
        { widget: "dxButton", toolbar: "bottom", location: "after", options: { text: saveText || "Guardar", icon: cerrar ? "close" : (opciones.icono || "save"), type: cerrar ? "normal" : "default", stylingMode: "contained",
          onClick: async e => { e.component.option("disabled", true); try { if ((await onSave(form)) !== false) popup.hide(); } finally { e.component.option("disabled", false); } } } }
      ]),
      onHidden: () => { setTimeout(() => { if (popup) { popup.dispose(); popup = null; $p.remove(); } }, 0); }
    }).dxPopup("instance");
    return popup;
  }
  function confirmar(titulo, html, texto, tipo, icono) {
    return DevExpress.ui.dialog.custom({
      title: titulo, messageHtml: '<p style="margin:0;max-width:380px;line-height:1.5">' + html + "</p>",
      buttons: [{ text: "Cancelar", icon: "close", stylingMode: "contained", type: "normal", onClick: () => false },
                { text: texto, icon: icono || "check", stylingMode: "contained", type: tipo || "default", onClick: () => true }]
    }).show();
  }
  // Clave temporal: se muestra una sola vez, seleccionable para copiarla
  function mostrarClave(usuario, clave) {
    openPopup("Clave temporal · " + usuario, $f => {
      $f.append($('<p class="muted" style="margin:0 0 8px">').text("Entrégala al usuario por un canal seguro. Se muestra una sola vez y deberá cambiarla al ingresar."));
      $f.append($('<div class="cred-show">').text(clave));
      return null;
    }, () => true, "Cerrar", 440, { extra: [{ text: "Copiar", icon: "copy", onClick: () => navigator.clipboard?.writeText(clave).then(() => notify("Clave copiada")) }] });
  }

  // ---------- Migas, títulos y detalle desplegable ----------
  function head($c, mod, title, extra) {
    $c.append($('<nav class="crumbs" aria-label="Migas de pan">').html(esc(mod) + " / " + (extra ? esc(title) + " / <b>" + esc(extra) + "</b>" : "<b>" + esc(title) + "</b>")));
  }
  function mdHead($el, parts, pill) {
    const $s = $('<div class="cov">').appendTo($el);
    parts.forEach((p, i) => $s.append(i === 0 ? $("<b>").text(p) : $("<span>").text(p)));
    if (pill) $s.append($("<span>").addClass(pillCls(pill) || "pill pill-b").text(pill));
  }
  function mdGrid($el, title, cols, data, empty) {
    if (title) $el.append($('<div class="mdtitle">').text(title));
    if (isPhone()) {
      const $w = $('<div class="mdcards">').appendTo($el);
      if (!data.length) $w.append($('<p class="muted">').text(empty || "Sin registros."));
      data.forEach(d => { const $c = $('<div class="mdcard">').appendTo($w); cols.forEach((c, ci) => { const $r = $('<div class="mdrow">').appendTo($c); if (ci > 0) $r.append($('<span class="mdk">').text(c[1])); const v = d[c[0]] == null || d[c[0]] === "" ? "—" : String(d[c[0]]); const pc = pillCls(v); $r.append($('<span class="mdv">').append(pc ? $("<span>").addClass(pc).text(v) : document.createTextNode(v))); if (ci === 0) $r.addClass("mdfirst"); }); });
      return;
    }
    $("<div>").appendTo($el).dxDataGrid({
      dataSource: data, showBorders: true, showRowLines: true, columnAutoWidth: false, wordWrapEnabled: true, paging: { enabled: false }, noDataText: empty || "Sin registros.",
      columns: cols.map(c => ({ dataField: c[0], caption: c[1], alignment: c[2] || "left", cssClass: "wrapcell",
        cellTemplate: ($c, i) => { const v = i.value == null || i.value === "" ? "—" : String(i.value); const pc = pillCls(v); if (pc) $c.append($("<span>").addClass(pc).text(v)); else $c.text(v); } }))
    });
  }
  function mdActions($el, list) { const $a = $('<div class="actions">').appendTo($el); list.forEach(b => button($("<div>").appendTo($a), b)); }
  function mdNote($el, text) { $el.append($('<p class="muted">').text(text)); }

  // ---------- Grid estándar (dxToolbar + dxDataGrid), como renderGrid del prototipo ----------
  // cfg: { $c, modulo, titulo, opcion, clave, columnas: [{ campo, titulo, pill, enlace, formato, valor, ancho }], cargar: async () => filas,
  //        botones: [{ text, icon, type, accion, fila, onClick(fila) }], filtros: [items de dxToolbar], detalle($e, fila), alAbrir(fila), exportar }
  function grid(cfg) {
    const $c = cfg.$c;
    head($c, cfg.modulo, cfg.titulo);
    $c.append($("<h1>").text(cfg.titulo));
    if (cfg.antes) cfg.antes($c);
    let g = null, filas = [];
    const sel = () => g && g.getSelectedRowsData()[0];
    const permitidos = (cfg.botones || []).filter(b => !b.accion || pb.can(cfg.opcion, b.accion));
    const items = (cfg.filtros || []).concat(permitidos.map(b => ({
      location: "before", locateInMenu: b.type === "default" ? "never" : "auto", widget: "dxButton", showText: "always",
      options: { text: b.text, icon: b.icon, type: b.type || "normal", stylingMode: "contained",
        onClick: () => { const f = sel(); if (b.fila && !f) return notify("Seleccione una fila", "warning"); b.onClick(f); } }
    })));
    const exportable = cfg.exportar !== false && pb.can(cfg.opcion, "exportar");
    if (exportable) items.push({ location: "after", locateInMenu: "never", widget: "dxDropDownButton", options: {
      text: "Exportar", icon: "export", stylingMode: "contained", displayExpr: "text", keyExpr: "id", useSelectMode: false, dropDownOptions: { width: 160 },
      items: [{ id: "xlsx", text: "Excel", icon: "exportxlsx" }, { id: "csv", text: "CSV", icon: "export" }], elementAttr: { "aria-label": "Exportar " + cfg.titulo },
      onItemClick: e => e.itemData.id === "xlsx" ? exportarExcel() : exportarCsv() } });
    if (!permitidos.length && !exportable && !(cfg.filtros || []).length) items.push({ location: "before", template: () => $('<span class="muted">').text("Solo consulta con el rol " + S.rolActivo) });
    $('<div class="gridbar">').appendTo($c).dxToolbar({ items: items, multiline: false });

    const $card = $('<div class="card gridcard">').appendTo($c);
    $("<div>").appendTo($('<div class="gridsearch">').appendTo($card)).dxTextBox({ mode: "search", placeholder: "Buscar en " + cfg.titulo.toLowerCase() + "…", valueChangeEvent: "input",
      inputAttr: { "aria-label": "Buscar en " + cfg.titulo }, onValueChanged: e => g && g.searchByText(e.value || "") });
    const $g = $("<div>").appendTo($card);
    const valor = (col, d) => col.valor ? col.valor(d) : col.formato === "fechaHora" ? fechaHora(d[col.campo]) : col.formato === "fecha" ? fecha(d[col.campo])
      : col.formato === "dinero" ? dinero(d[col.campo]) : d[col.campo];
    const columnas = [{
      caption: "", width: 56, allowSorting: false, allowFiltering: false, allowSearch: false, allowExporting: false, alignment: "center", name: "sel",
      headerCellTemplate: $h => $h.append('<span class="dx-datagrid-hidden-column" style="position:absolute;width:1px;height:1px;overflow:hidden">Selección</span>'),
      cellTemplate: ($cell, info) => $('<input type="radio" name="fila" class="radio">').attr("aria-label", "Seleccionar " + (info.data[cfg.columnas[0].campo] || ""))
        .prop("checked", !!info.row.isSelected).on("click", ev => { ev.stopPropagation(); g.selectRows([info.key], false); }).appendTo($cell)
    }].concat(cfg.columnas.map((col, i) => ({
      dataField: col.campo, caption: col.titulo, minWidth: 90, width: col.ancho, alignment: col.formato === "dinero" ? "right" : undefined,
      calculateCellValue: d => valor(col, d), calculateSortValue: col.formato ? col.campo : undefined,
      cellTemplate: ($cell, info) => {
        const v = info.value == null || info.value === "" ? "—" : info.value;
        // Primera columna: enlace que abre la ficha, o que despliega el detalle (expandir, como «Coactivas por cliente»)
        if (i === 0 && (cfg.alAbrir || cfg.expandir)) {
          $('<button type="button" class="lnk">').text(v).on("click", ev => {
            ev.stopPropagation();
            if (cfg.expandir) { const gr = info.component; if (gr.isRowExpanded(info.key)) gr.collapseRow(info.key); else gr.expandRow(info.key); } else cfg.alAbrir(info.data);
          }).appendTo($cell);
          return;
        }
        const pc = col.pill ? pillCls(String(v)) : "";
        if (pc) $("<span>").addClass(pc).text(v).appendTo($cell); else $cell.text(v);
      }
    })));
    const $f = $('<div class="gridfoot">');
    let ant = null, sig = null, $num = null, $cnt = null;
    g = $g.dxDataGrid({
      dataSource: [], keyExpr: cfg.clave, columns: columnas, showBorders: false, showRowLines: true, hoverStateEnabled: true, wordWrapEnabled: true,
      allowColumnResizing: true, columnHidingEnabled: isPhone(), selection: { mode: "single" }, sorting: { mode: "single" },
      paging: { enabled: true, pageSize: 50 }, pager: { visible: false }, scrolling: { mode: "standard", showScrollbar: "onHover" },
      searchPanel: { visible: false, highlightSearchText: true }, noDataText: cfg.vacio || "No hay registros que coincidan con la búsqueda.",
      masterDetail: { enabled: !!cfg.detalle, template: (el, info) => cfg.detalle($('<div class="md">').appendTo(el), info.data) },
      onRowPrepared: e => { if (e.rowType === "data" && cfg.anulada && cfg.anulada(e.data)) e.rowElement.addClass("rowvoid"); },
      onSelectionChanged: e => { const k = e.selectedRowKeys[0]; setTimeout(() => e.element.find('input[name="fila"]').each(function () { this.checked = $(this).closest("tr").hasClass("dx-selection"); }), 0); if (cfg.alSeleccionar) cfg.alSeleccionar(k); },
      onContentReady: () => pie()
    }).dxDataGrid("instance");

    // Pie: paginador centrado y conteo de registros
    $f.appendTo($card);
    const $pg = $('<div class="pager" role="navigation" aria-label="Paginación">').appendTo($f);
    ant = button($("<div>").appendTo($pg), { icon: "chevronleft", aria: "Página anterior", hint: "Página anterior", onClick: () => g.pageIndex(g.pageIndex() - 1) });
    $num = $('<span class="pg-num">').appendTo($pg);
    sig = button($("<div>").appendTo($pg), { icon: "chevronright", aria: "Página siguiente", hint: "Página siguiente", onClick: () => g.pageIndex(g.pageIndex() + 1) });
    $cnt = $('<span class="pg-count">').appendTo($f);
    pie();
    function pie() {
      if (!g || !ant) return;
      const pc = Math.max(1, g.pageCount()), pi = g.pageIndex(), n = g.totalCount();
      $num.html("Página <b>" + (pi + 1) + "</b> de " + pc);
      ant.option("disabled", pi <= 0); sig.option("disabled", pi >= pc - 1);
      $cnt.text(n === filas.length ? filas.length + " registros" : n + " de " + filas.length + " registros");
    }
    function exportarExcel() {
      const wb = new ExcelJS.Workbook(), ws = wb.addWorksheet(cfg.titulo.substring(0, 31));
      DevExpress.excelExporter.exportDataGrid({ component: g, worksheet: ws, autoFilterEnabled: true })
        .then(() => wb.xlsx.writeBuffer()).then(b => { saveAs(new Blob([b], { type: "application/octet-stream" }), cfg.titulo + ".xlsx"); notify("Archivo Excel de " + cfg.titulo + " generado"); });
    }
    function exportarCsv() {
      const q = v => '"' + String(v == null ? "" : v).replace(/"/g, '""') + '"';
      const lineas = [cfg.columnas.map(c => q(c.titulo)).join(";")].concat(filas.map(d => cfg.columnas.map(c => q(valor(c, d))).join(";")));
      saveAs(new Blob(["﻿" + lineas.join("\r\n")], { type: "text/csv;charset=utf-8" }), cfg.titulo + ".csv");
      notify("Archivo CSV de " + cfg.titulo + " generado");
    }
    async function recargar() {
      const r = await cfg.cargar(); if (r === undefined) return;
      // Conserva los detalles desplegados al recargar (por ejemplo tras un bloqueo hecho desde el detalle)
      const abiertas = filas.map(f => f[cfg.clave]).filter(k => g.isRowExpanded(k));
      filas = r; g.option("dataSource", filas); g.clearSelection();
      if (abiertas.length) setTimeout(() => abiertas.forEach(k => { if (filas.some(f => f[cfg.clave] === k)) g.expandRow(k); }), 0);
    }
    recargar();
    return { recargar, seleccion: sel, grid: () => g };
  }

  // ---------- Ficha de detalle (renderDetalle del prototipo): nuevo, consulta o edición ----------
  // cfg: { $c, modulo, titulo, modo, nombre, token, items, datos, extra($c, ro), alGuardar: async (datos) => bool, alVolver }
  function detalle(cfg) {
    const $c = cfg.$c.empty(), ro = cfg.modo === "consulta";
    const ml = { consulta: "Consulta", nuevo: "Nuevo", edicion: "Edición" }[cfg.modo];
    head($c, cfg.modulo, cfg.titulo, ml);
    $c.append($("<h1>").text((cfg.modo === "nuevo" ? "Nuevo registro" : (cfg.nombre || "Registro")) + " · " + ml));
    if (cfg.token) $c.append($('<div class="route">').html("Ruta: <code>/detalle/" + esc(cfg.token) + "</code> · identificador opaco, el ID no se muestra en la URL"));
    const $fc = $('<div class="card pad">').appendTo($c);
    const form = $("<div>").appendTo($fc).dxForm({
      formData: cfg.datos, readOnly: ro, labelLocation: "top", showColonAfterLabel: false, colCountByScreen: { lg: 3, md: 2, sm: 1, xs: 1 },
      screenByWidth: w => (w < 600 ? "xs" : w < 900 ? "sm" : w < 1200 ? "md" : "lg"), items: cfg.items(ro)
    }).dxForm("instance");
    if (cfg.extra) cfg.extra($c, ro);
    const $act = $('<div class="actions">').appendTo($c);
    button($("<div>").appendTo($act), { text: "Guardar", icon: "save", type: "default", disabled: ro || cfg.soloLectura, onClick: async e => {
      const v = form.validate(); if (v && !v.isValid) return notify("Complete los campos obligatorios", "warning");
      e.component.option("disabled", true);
      try { await cfg.alGuardar(form.option("formData")); } finally { e.component.option("disabled", false); }
    } });
    if (ro && !cfg.soloLectura) button($("<div>").appendTo($act), { text: "Editar", icon: "edit", onClick: () => cfg.alEditar ? cfg.alEditar() : detalle(Object.assign({}, cfg, { modo: "edicion" })) });
    button($("<div>").appendTo($act), { text: "Volver al listado", icon: "arrowleft", onClick: cfg.alVolver });
    button($("<div>").appendTo($act), { text: "Cancelar", icon: "close", onClick: () => { cfg.alVolver(); if (!ro) notify("Cambios descartados", "info"); } });
    return form;
  }

  // ---------- Cabecera, perfil, menú y pie ----------
  function cambiarClave(forzado) {
    const data = { actual: "", nueva: "", confirmar: "" };
    openPopup(forzado ? "Cambia tu contraseña temporal" : "Cambiar contraseña", $f => {
      $f.append($('<p class="muted" style="margin:0 0 8px">').text((forzado ? "Tu contraseña es temporal: cámbiala para continuar. " : "") + "Mínimo 14 caracteres y sin tu nombre de usuario. Al guardar se cierran tus sesiones y vuelves a ingresar con la nueva."));
      return $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
        { dataField: "actual", label: { text: forzado ? "Contraseña temporal" : "Contraseña actual" }, isRequired: true, editorOptions: { mode: "password", inputAttr: { autocomplete: "current-password" } } },
        { dataField: "nueva", label: { text: "Nueva contraseña" }, isRequired: true, editorOptions: { mode: "password", inputAttr: { autocomplete: "new-password" } },
          validationRules: [{ type: "stringLength", min: 14, message: "Al menos 14 caracteres" }] },
        { dataField: "confirmar", label: { text: "Confirmar la nueva contraseña" }, isRequired: true, editorOptions: { mode: "password", inputAttr: { autocomplete: "new-password" } },
          validationRules: [{ type: "compare", comparisonTarget: () => data.nueva, message: "No coincide con la nueva contraseña" }] }
      ] }).dxForm("instance");
    }, async form => {
      if (!form.validate().isValid) return false;
      const r = await enviar("POST", "/Seguridad/Perfil/CambiarClave", { actual: data.actual, nueva: data.nueva });
      if (r === undefined) return false;
      window.location.href = r.redirect;
    }, "Cambiar contraseña", 460, { forzado: !!forzado, icono: "key" });
  }

  function rolePicker() {
    const roles = S.roles, data = { rol: S.rolActivo, predeterminado: false };
    openPopup("Cambiar de rol", $f => {
      $f.append($('<p class="muted" style="margin:0 0 8px">').text("Tiene " + roles.length + " roles asignados. El menú y las acciones disponibles dependen del rol activo; puede cambiarlo en cualquier momento desde su perfil."));
      return $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
        { dataField: "rol", label: { text: "Rol" }, editorType: "dxRadioGroup", editorOptions: { items: roles, layout: "vertical" } },
        { dataField: "predeterminado", label: { visible: false }, editorType: "dxCheckBox", editorOptions: { text: "Usarlo como rol predeterminado al ingresar" } }] }).dxForm("instance");
    }, async () => {
      if (data.rol === S.rolActivo && !data.predeterminado) return true;
      const r = await enviar("POST", "/Seguridad/Perfil/CambiarRol", data);
      if (r === undefined) return false;
      window.location.reload();
    }, "Cambiar de rol", 440);
  }

  function perfilTpl() {
    const p = pb.perfil || {};
    const $c = $('<div class="adcard" role="dialog" aria-label="Perfil del usuario">');
    $c.append($('<div class="adhead">').append($(document.querySelector(".avsvg").outerHTML)).append($("<div>").append($("<b>").text(S.nombre)).append($("<span>").text(S.usuario + (p.correo ? " · " + p.correo : "")))));
    const dl = rows => { const $d = $('<dl class="adlist">'); rows.forEach(r => $d.append($("<dt>").text(r[0])).append($("<dd>").text(r[1] || "—"))); return $d; };
    $c.append('<div class="adsec">Cuenta de ProBank</div>');
    $c.append(dl([["Usuario", S.usuario], ["Correo", p.correo], ["Rol predeterminado", p.rolPredeterminado], ["Clave cambiada", fecha(p.claveCambiada)]]));
    $c.append('<div class="adsec">Roles asignados</div>');
    const $g = $('<div class="adgroups">').appendTo($c); S.roles.forEach(r => $g.append($("<span>").addClass(r === S.rolActivo ? "pill pill-g" : "pill pill-b").text(r)));
    $c.append('<div class="adsec">Sesión</div>');
    $c.append(dl([["Rol activo", S.rolActivo], ["Última conexión", fechaHora(p.conexionAnterior)], ["IP anterior", p.ipAnterior], ["IP actual", p.ultimaIp]]));
    $c.append('<p class="muted" style="margin:0">Tus datos y roles los administra Seguridad › Usuarios.</p>');
    const $b = $('<div class="actions">').appendTo($c);
    if (S.roles.length > 1) $("<div>").appendTo($b).dxButton({ text: "Cambiar de rol", icon: "group", type: "default", stylingMode: "contained", onClick: () => { perfilPop.hide(); rolePicker(); } });
    $("<div>").appendTo($b).dxButton({ text: "Cambiar contraseña", icon: "key", stylingMode: "contained", onClick: () => { perfilPop.hide(); cambiarClave(false); } });
    $("<div>").appendTo($b).dxButton({ text: "Cerrar sesión", icon: "runner", stylingMode: "contained", onClick: () => document.getElementById("frmSalir").submit() });
    return $c;
  }

  let perfilPop = null, accordion = null;
  function menu() {
    const actual = window.location.pathname.toLowerCase(), opcion = S.modulo;
    const visibles = MENU.map(g => Object.assign({}, g, { items: g.items.filter(i => pb.can(i[0], "ver")) })).filter(g => g.items.length);
    const activo = it => (opcion && it[0] === opcion) || (!opcion && it[2] && actual.indexOf(it[2].toLowerCase()) === 0);
    accordion = $("#accordion").dxAccordion({
      dataSource: visibles, collapsible: true, multiple: true, animationDuration: 180, focusStateEnabled: true,
      selectedItems: visibles.filter(g => g.items.some(activo)),
      itemTitleTemplate: d => $("<span>").append('<span class="dx-icon dx-icon-' + d.icon + '"></span>').append($("<span>").text(d.title)),
      itemTemplate: d => { const $w = $("<div>"); d.items.forEach(it => { const $a = $('<a class="navitem sub">').attr({ href: urlDe(it), "data-k": it[0] }).text(it[1]).appendTo($w); if (activo(it)) $a.attr("aria-current", "page"); }); return $w; }
    }).dxAccordion("instance");
    const home = document.getElementById("navHome");
    if (actual === "/" || actual === "/home" || actual === "/home/index") home.setAttribute("aria-current", "page");
    if (!pb.can("inicio", "ver")) home.hidden = true;
  }

  function cabecera() {
    $("#btnMenu").dxButton({ icon: "menu", stylingMode: "outlined", elementAttr: { "aria-label": "Mostrar u ocultar el menú", "aria-expanded": "true" },
      onClick: e => { const nav = $("#nav"); const hide = !nav.prop("hidden"); nav.prop("hidden", hide); e.element.attr("aria-expanded", String(!hide)); } });
    if (isPhone()) { $("#nav").prop("hidden", true); $("#btnMenu").attr("aria-expanded", "false"); }
    perfilPop = $("<div>").appendTo("body").dxPopover({ target: "#btnProfile", position: "bottom", width: 360, maxWidth: "94vw", showTitle: false, hideOnOutsideClick: true,
      onShowing: e => $(e.component.content()).empty().append(perfilTpl()) }).dxPopover("instance");
    $("#btnProfile").on("click", () => perfilPop.toggle());
    const reloj = () => { const d = new Date(); $("#clock").text(pad(d.getDate()) + "/" + pad(d.getMonth() + 1) + "/" + d.getFullYear() + " " + pad(d.getHours()) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds())); };
    reloj(); setInterval(reloj, 1000);
  }

  window.pb = {
    sesion: S, perfil: null, menu: MENU, esc, pillCls, fecha, fechaHora, aFecha, dinero, isPhone,
    token: () => document.querySelector('input[name="__RequestVerificationToken"]')?.value,
    can: (opcion, accion) => !!pb.perfil && pb.perfil.permisos.indexOf(opcion + ":" + (accion || "ver")) >= 0,
    notify, aviso: notify,
    get: url => fetch(url, { headers: { Accept: "application/json" } }).then(leer),
    post: (url, body) => enviar("POST", url, body),
    put: (url, body) => enviar("PUT", url, body),
    _leer: leer,
    ui: { button, openPopup, confirmar, mostrarClave, head, mdHead, mdGrid, mdActions, mdNote, grid, detalle, cambiarClave },
    listo: null
  };

  // El perfil trae los permisos del rol activo: con ellos se arma el menú y las vistas habilitan sus botones.
  pb.listo = new Promise(resolve => {
    $(async () => {
      if (!document.getElementById("app")) return resolve();
      cabecera();
      const p = await pb.get("/Seguridad/Perfil/Datos");
      pb.perfil = p || { permisos: [] };
      if (p) {
        $("#connAnterior").text("Última conexión: " + fechaHora(p.conexionAnterior) + " · IP anterior " + (p.ipAnterior || "—"));
        $("#connActual").text("IP actual: " + (p.ultimaIp || "—"));
      }
      menu();
      if (S.cambiarClave || (p && p.debeCambiarClave)) cambiarClave(true);
      resolve();
    });
  });
})();
