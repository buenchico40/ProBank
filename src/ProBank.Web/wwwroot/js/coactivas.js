// Coactivas: componentes del prototipo compartidos por Oficios y Coactivas (productPanel, bloqueoPopup, ejecutarPopup,
// cuartillaPopup y carga masiva). Oficio (1) → coactivas (N, de distintas entidades) → bloqueos por producto del cliente (N).
(function () {
  "use strict";
  const ORD = {
    Bloqueo: { largo: "Bloqueo de fondos", corto: "Bloqueo", pill: "pill pill-a" },
    Levantamiento: { largo: "Levantamiento de bloqueo", corto: "Levantamiento", pill: "pill pill-b" },
    Transferencia: { largo: "Transferencia de fondos", corto: "Transferencia", pill: "pill pill-g" }
  };
  const PRODTIPOS = ["Cuenta de ahorros", "Cuenta corriente", "Depósito a plazo fijo", "Inversión"];
  const TIPOSID = ["Cédula", "RUC", "Pasaporte"];
  const $$ = pb.dinero;
  const api = (c, a, q) => "/Coactivas/" + c + "/" + a + (q ? "?" + q : "");
  const listaProd = s => String(s || "").split(",").map(x => x.trim()).filter(Boolean);
  const permTxt = s => listaProd(s).length ? listaProd(s).join(", ") : "Todos los productos";
  const iso = d => { const x = d instanceof Date ? d : new Date(d); return x.getFullYear() + "-" + String(x.getMonth() + 1).padStart(2, "0") + "-" + String(x.getDate()).padStart(2, "0"); };
  const ordenesResumen = o => [["bloqueos", "bloqueo"], ["levantamientos", "levantamiento"], ["transferencias", "transferencia"]].filter(x => o[x[0]] > 0).map(x => o[x[0]] + " " + x[1]).join(" · ") || "—";

  // ---------- Detalle de la coactiva: productos del cliente, bloqueos y movimientos (productPanel del prototipo) ----------
  async function panel($el, token, ctx, alCambiar) {
    ctx = ctx || {};
    const d = await pb.get(api("Coactivas", "Detalle", "token=" + token));
    if (!d) return;
    const anulada = d.estado === "Anulado", falta = Math.max(0, d.monto - d.bloqueado - d.transferido);
    const recargar = () => { $el.empty(); panel($el, token, ctx, alCambiar); if (alCambiar) alCambiar(); };
    const $w = $('<div class="stack" style="gap:10px">').appendTo($el.empty());
    const $sum = $('<div class="cov">').appendTo($w);
    $sum.append($("<span>").html("<b>" + pb.esc(d.numeroProceso) + "</b> · " + pb.esc(d.entidad) + " · " + pb.esc(d.cliente) + " (" + pb.esc(d.identificacion) + ")"));
    $sum.append($("<span>").text("Monto: " + $$(d.monto)));
    $sum.append($("<span>").text("Productos a bloquear: " + permTxt(d.productosPermitidos)));
    $sum.append($("<span>").text("Bloqueado vigente: " + $$(d.bloqueado)));
    if (d.levantado > 0) $sum.append($("<span>").text("Levantado: " + $$(d.levantado)));
    if (d.transferido > 0) $sum.append($("<span>").text("Transferido: " + $$(d.transferido)));
    if (!anulada) $sum.append($("<span>").addClass(falta > 0 ? "pill pill-a" : "pill pill-b").text(falta > 0 ? "Por cubrir: " + $$(falta) : "Cubierta"));
    else $sum.append($('<span class="pill pill-r">').text("Anulado"));
    const pct = d.monto ? Math.min(100, Math.round((d.bloqueado + d.transferido) / d.monto * 100)) : 0;
    $("<div>").appendTo($w).dxProgressBar({ min: 0, max: 100, value: pct, showStatus: true, statusFormat: r => "Cobertura " + Math.round(r * 100) + " %", elementAttr: { "aria-label": "Cobertura de la coactiva" } });

    const perm = listaProd(d.productosPermitidos);
    const permitido = t => !perm.length || perm.indexOf(t) >= 0;
    const prods = d.productos.map(p => ({
      productoId: p.productoId, producto: p.tipo, cuenta: p.numero, saldo: p.saldoReferencial, otros: p.bloqueadoTotal - p.bloqueadoEsta,
      disponible: permitido(p.tipo) ? Math.max(0, p.saldoReferencial - p.bloqueadoTotal) : 0, esta: p.bloqueadoEsta,
      estado: p.estados ? [...new Set(p.estados.split(","))].join(", ") : permitido(p.tipo) ? "—" : "No permitido", _p: p
    }));
    pb.ui.mdGrid($w, "Productos del cliente", [["producto", "Producto"], ["cuenta", "Cuenta"], ["s", "Saldo referencial", "right"], ["o", "Bloqueado por otras coactivas", "right"],
      ["di", "Disponible para bloquear", "right"], ["e", "Bloqueado en esta coactiva", "right"], ["estado", "Estado"]],
      prods.map(p => Object.assign({}, p, { s: $$(p.saldo), o: p.otros > 0 ? $$(p.otros) : "—", di: $$(p.disponible), e: p.esta > 0 ? $$(p.esta) : "—" })),
      "El cliente no tiene productos registrados: regístrelos con «Registrar producto».");
    pb.ui.mdGrid($w, "Bloqueos de la coactiva", [["codigo", "Bloqueo"], ["cu", "Producto y cuenta"], ["m", "Bloqueado", "right"], ["l", "Levantado", "right"], ["t", "Transferido", "right"], ["v", "Vigente", "right"], ["estado", "Estado"]],
      d.bloqueos.map(b => ({ codigo: b.codigo, cu: b.producto + " " + b.cuenta, m: $$(b.monto), l: b.levantado > 0 ? $$(b.levantado) : "—", t: b.transferido > 0 ? $$(b.transferido) : "—", v: $$(b.vigente), estado: b.estado })),
      "Aún no hay bloqueos registrados.");
    pb.ui.mdGrid($w, "Movimientos por orden judicial", [["fecha", "Fecha"], ["tipo", "Operación"], ["oficio", "Oficio"], ["cuenta", "Cuenta"], ["m", "Monto", "right"], ["usuario", "Usuario"], ["detalle", "Detalle"]],
      d.movimientos.map(m => ({ fecha: pb.fecha(m.fecha), tipo: (ORD[m.tipo] || {}).largo || m.tipo, oficio: m.oficio || "—", cuenta: m.cuenta, m: $$(m.monto), usuario: m.usuario, detalle: m.detalle || "—" })),
      "Sin movimientos.");

    const tq = ctx.tipo || "Bloqueo", acciones = [];
    if (pb.can("coactivas", "bloquear")) acciones.push({ text: "Bloquear fondos", icon: "lock", type: tq === "Bloqueo" ? "default" : "normal", disabled: anulada || falta <= 0, onClick: () => bloqueoPopup(d, prods, falta, recargar) });
    if (pb.can("coactivas", "levantar")) acciones.push({ text: "Levantar bloqueo", icon: "unlock", type: tq === "Levantamiento" ? "default" : "normal", disabled: anulada || d.bloqueado <= 0, onClick: () => ejecutarPopup("Levantamiento", d, ctx.oficio, recargar) });
    if (pb.can("coactivas", "transferir")) acciones.push({ text: "Transferir fondos a la entidad", icon: "money", type: tq === "Transferencia" ? "default" : "normal", disabled: anulada || d.bloqueado <= 0, onClick: () => ejecutarPopup("Transferencia", d, ctx.oficio, recargar) });
    if (pb.can("coactivas", "editar")) acciones.push({ text: "Registrar producto", icon: "add", disabled: anulada, onClick: () => productoPopup(d, null, recargar) });
    if (acciones.length) pb.ui.mdActions($w, acciones);
    $w.append('<p class="muted">Cada operación se registra con el oficio que la ordena. Saldos referenciales y bloqueos se registran manualmente: en esta fase no hay conexión al core bancario.</p>');
  }

  function bloqueoPopup(d, prods, falta, listo) {
    const items = prods.map(p => ({ key: p.productoId, tipo: p.producto, disp: p.disponible,
      text: p.producto + " " + p.cuenta + (p.estado === "No permitido" ? " · no permitido por la orden" : " · disponible " + $$(p.disponible)), disabled: p.estado === "No permitido" || p.disponible <= 0 }));
    if (!items.length) return pb.notify("Registre primero los productos del cliente", "warning");
    const primero = items.find(x => !x.disabled);
    const data = { productoId: primero ? primero.key : null, monto: primero ? Math.min(falta, primero.disp) : null, observacion: "" };
    let form;
    pb.ui.openPopup("Registrar bloqueo · " + d.numeroProceso, $f => (form = $("<div>").appendTo($f).dxForm({
      formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
        { template: () => $('<p class="muted" style="margin:0">').text(d.entidad + " · " + d.cliente + " · por cubrir " + $$(falta) + " · productos permitidos: " + permTxt(d.productosPermitidos)) },
        { dataField: "productoId", label: { text: "Producto del cliente" }, editorType: "dxSelectBox", isRequired: true, editorOptions: { dataSource: items, valueExpr: "key", displayExpr: "text",
          onValueChanged: e => { const it = items.find(x => x.key === e.value); if (it) form.updateData("monto", Math.min(falta, it.disp)); } } },
        { dataField: "monto", label: { text: "Monto a bloquear" }, editorType: "dxNumberBox", isRequired: true, editorOptions: { format: "$ #,##0.00", min: 0.01 },
          validationRules: [{ type: "custom", message: "Supera el saldo por cubrir o el disponible del producto", validationCallback: o => { const it = items.find(x => x.key === data.productoId); return !!it && o.value <= falta + 0.001 && o.value <= it.disp + 0.001; } }] },
        { dataField: "observacion", label: { text: "Observación" }, editorType: "dxTextArea", editorOptions: { height: 70, maxLength: 500 } }
      ] }).dxForm("instance")), async f => {
      if (!f.validate().isValid) return false;
      const r = await pb.post(api("Coactivas", "Bloqueo", "token=" + d.token), data);
      if (r === undefined) return false;
      pb.notify("Bloqueo " + r + " de " + $$(data.monto) + " registrado"); listo();
    }, "Registrar bloqueo");
  }

  // Levantamiento o transferencia: se elige el oficio (orden) que lo ordena y el monto por cada bloqueo vigente
  function ejecutarPopup(tipo, d, oficioPreferido, listo) {
    const lev = tipo === "Levantamiento";
    const ordenes = d.ordenes.filter(o => o.tipo === tipo && o.estado !== "Ejecutada" && o.estado !== "Anulado");
    if (!ordenes.length) return pb.notify("Registre primero la orden de " + (lev ? "levantamiento" : "transferencia") + " de " + d.numeroProceso + " en el oficio que la ordena (Oficios › Agregar coactiva)", "warning");
    const vig = d.bloqueos.filter(b => b.vigente > 0.004);
    if (!vig.length) return pb.notify("La coactiva " + d.numeroProceso + " no tiene fondos bloqueados", "warning");
    const pref = ordenes.find(o => o.oficio === oficioPreferido) || ordenes[ordenes.length - 1];
    const data = { ordenId: pref.ordenId, fecha: new Date(), forma: "Transferencia interbancaria", destino: d.cuentaDestino || "", comprobante: "", nota: "" };
    const repartir = () => {
      const o = ordenes.find(x => x.ordenId === data.ordenId);
      let resto = o && o.montoOrdenado ? o.montoOrdenado - o.ejecutado : Infinity;
      vig.forEach((b, i) => { const v = Math.min(b.vigente, resto); data["b" + i] = Math.round(v * 100) / 100; resto -= v; });
    };
    repartir();
    let form;
    const items = [
      { template: () => $('<p class="muted" style="margin:0">').text(d.numeroProceso + " · " + d.entidad + " · " + d.cliente + " · bloqueado vigente " + $$(d.bloqueado)), colSpan: 2 },
      { dataField: "ordenId", label: { text: "Oficio que lo ordena" }, editorType: "dxSelectBox", isRequired: true,
        editorOptions: { dataSource: ordenes.map(o => ({ k: o.ordenId, t: o.oficio + " · " + o.remitidoPor + " · " + pb.fecha(o.recibido) + (o.montoOrdenado ? " · ordenado " + $$(o.montoOrdenado) + (o.ejecutado > 0 ? " (ejecutado " + $$(o.ejecutado) + ")" : "") : " · total") })),
          valueExpr: "k", displayExpr: "t", onValueChanged: () => { repartir(); form && form.option("formData", data); } } },
      { dataField: "fecha", label: { text: lev ? "Fecha del levantamiento" : "Fecha de la transferencia" }, editorType: "dxDateBox", isRequired: true, editorOptions: { displayFormat: "dd/MM/yyyy", max: new Date() } },
      { itemType: "group", caption: lev ? "Monto a levantar por cuenta" : "Monto a transferir por cuenta", colSpan: 2, colCount: 2, items: vig.map((b, i) => ({
        dataField: "b" + i, label: { text: b.codigo + " · " + b.producto + " " + b.cuenta + " (vigente " + $$(b.vigente) + ")" }, editorType: "dxNumberBox",
        editorOptions: { format: "$ #,##0.00", min: 0 }, validationRules: [{ type: "custom", message: "No puede superar lo bloqueado vigente", validationCallback: o => (o.value || 0) <= b.vigente + 0.004 }] })) }];
    if (!lev) items.push({ dataField: "forma", label: { text: "Forma de pago" }, editorType: "dxSelectBox", editorOptions: { items: ["Transferencia interbancaria", "Cheque certificado"] } },
      { dataField: "destino", label: { text: "Cuenta de la entidad" }, isRequired: true, editorOptions: { maxLength: 150 } }, { dataField: "comprobante", label: { text: "N.º de comprobante" }, editorOptions: { maxLength: 60 } });
    items.push({ dataField: "nota", label: { text: "Observación" }, editorType: "dxTextArea", editorOptions: { height: 56, maxLength: 500 }, colSpan: 2 });
    pb.ui.openPopup((lev ? "Levantar bloqueo · " : "Transferir fondos · ") + d.numeroProceso, $f => (form = $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, colCountByScreen: { lg: 2, md: 2, sm: 1, xs: 1 }, items }).dxForm("instance")), async f => {
      if (!f.validate().isValid) return false;
      const montos = vig.map((b, i) => ({ bloqueoId: b.bloqueoId, monto: data["b" + i] || 0 })).filter(m => m.monto > 0);
      if (!montos.length) { pb.notify("Indique el monto en al menos una cuenta", "warning"); return false; }
      const r = await pb.post(api("Coactivas", "Ejecutar"), { ordenId: data.ordenId, fecha: iso(data.fecha), montos, formaPago: lev ? null : data.forma,
        cuentaDestino: lev ? null : data.destino, comprobante: lev ? null : data.comprobante, observacion: data.nota });
      if (r === undefined) return false;
      const of = ordenes.find(o => o.ordenId === data.ordenId);
      pb.notify((lev ? "Levantamiento de " : "Transferencia de ") + $$(r) + " registrado en " + d.numeroProceso + " con el oficio " + of.oficio + (lev ? ": los fondos quedan disponibles para el cliente" : " a " + d.entidad));
      listo();
    }, lev ? "Registrar levantamiento" : "Registrar transferencia", 680);
  }

  function productoPopup(d, p, listo) {
    const data = { productoId: p ? p.productoId : null, tipo: p ? p.tipo : (listaProd(d.productosPermitidos)[0] || PRODTIPOS[0]), numero: p ? p.numero : "", saldo: p ? p.saldoReferencial : null };
    pb.ui.openPopup("Registrar producto · " + d.cliente, $f => {
      $f.append($('<p class="muted" style="margin:0 0 8px">').text("Saldo referencial informado por el área de operaciones: en esta fase no hay conexión al core bancario."));
      return $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
        { dataField: "tipo", label: { text: "Producto" }, editorType: "dxSelectBox", isRequired: true, editorOptions: { items: PRODTIPOS } },
        { dataField: "numero", label: { text: "Número de cuenta" }, isRequired: true, editorOptions: { maxLength: 30 } },
        { dataField: "saldo", label: { text: "Saldo referencial" }, editorType: "dxNumberBox", isRequired: true, editorOptions: { format: "$ #,##0.00", min: 0 } }
      ] }).dxForm("instance");
    }, async f => {
      if (!f.validate().isValid) return false;
      if (await pb.post(api("Coactivas", "Producto", "token=" + d.token), data) === undefined) return false;
      pb.notify("Producto " + data.tipo + " " + data.numero + " registrado"); listo();
    }, "Guardar", 460);
  }

  // ---------- Agregar coactiva al oficio (cuartillaPopup del prototipo) ----------
  async function ordenPopup(oficio, listo) {
    const [entidades, coactivas] = await Promise.all([pb.get(api("Oficios", "Entidades")), pb.get(api("Coactivas", "Datos"))]);
    if (!entidades || !coactivas) return;
    const activas = entidades.filter(e => e.estado === "Activo");
    const data = { tipo: "Bloqueo", numeroProceso: "", entidadId: null, tipoIdentificacion: "Cédula", identificacion: "", cliente: "", monto: null, productos: [], coactivaToken: null, parcial: false, montoOrdenado: null, observacion: "" };
    const cands = () => coactivas.filter(c => c.estado !== "Anulado" && c.bloqueado > 0).map(c => ({ k: c.token, t: c.numeroProceso + " · " + c.entidad + " · " + c.cliente + " · bloqueado " + $$(c.bloqueado) }));
    let form;
    const vis = () => { const b = data.tipo === "Bloqueo"; ["numeroProceso", "entidadId", "tipoIdentificacion", "identificacion", "cliente", "monto", "productos"].forEach(k => form.itemOption(k, "visible", b));
      ["coactivaToken", "parcial"].forEach(k => form.itemOption(k, "visible", !b)); form.itemOption("montoOrdenado", "visible", !b && data.parcial); };
    pb.ui.openPopup("Agregar coactiva al oficio " + oficio.numero, $f => (form = $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, colCountByScreen: { lg: 2, md: 2, sm: 1, xs: 1 }, items: [
      { dataField: "tipo", label: { text: "Orden" }, colSpan: 2, editorType: "dxRadioGroup", editorOptions: { dataSource: Object.keys(ORD).map(k => ({ k, t: ORD[k].largo })), valueExpr: "k", displayExpr: "t", layout: "vertical", onValueChanged: () => setTimeout(vis, 0) } },
      { dataField: "numeroProceso", label: { text: "N.º de proceso" }, isRequired: true, editorOptions: { maxLength: 50 } },
      { dataField: "entidadId", label: { text: "Entidad" }, editorType: "dxSelectBox", isRequired: true, editorOptions: { dataSource: activas, valueExpr: "entidadId", displayExpr: e => e && (e.nombre + " · " + e.ruc), searchEnabled: true, noDataText: "Registre las entidades en Oficios › Entidades" } },
      { dataField: "tipoIdentificacion", label: { text: "Tipo de identificación" }, editorType: "dxSelectBox", editorOptions: { items: TIPOSID } },
      { dataField: "identificacion", label: { text: "Identificación del cliente" }, isRequired: true, editorOptions: { maxLength: 20 } },
      { dataField: "cliente", label: { text: "Cliente / empresa" }, isRequired: true, colSpan: 2, editorOptions: { maxLength: 150 } },
      { dataField: "monto", label: { text: "Monto de la coactiva" }, editorType: "dxNumberBox", isRequired: true, editorOptions: { format: "$ #,##0.00", min: 0.01 } },
      { dataField: "productos", label: { text: "Productos a bloquear (vacío = todos)" }, editorType: "dxTagBox", editorOptions: { items: PRODTIPOS, showSelectionControls: true, applyValueMode: "instantly", placeholder: "Todos los productos del cliente" } },
      { dataField: "coactivaToken", visible: false, colSpan: 2, label: { text: "Coactiva (N.º de proceso) a la que se refiere" }, editorType: "dxSelectBox", isRequired: true, editorOptions: { dataSource: cands(), valueExpr: "k", displayExpr: "t", searchEnabled: true, noDataText: "No hay coactivas con fondos bloqueados" } },
      { dataField: "parcial", visible: false, label: { text: "Por un monto parcial" }, editorType: "dxSwitch", editorOptions: { switchedOnText: "Sí", switchedOffText: "No", onValueChanged: () => setTimeout(vis, 0) } },
      { dataField: "montoOrdenado", visible: false, label: { text: "Monto ordenado" }, editorType: "dxNumberBox", isRequired: true, editorOptions: { format: "$ #,##0.00", min: 0.01 } },
      { dataField: "observacion", label: { text: "Observación" }, colSpan: 2, editorType: "dxTextArea", editorOptions: { height: 50, maxLength: 500 } }
    ] }).dxForm("instance")), async f => {
      if (!f.validate().isValid) return false;
      const b = data.tipo === "Bloqueo";
      const cuerpo = b ? { tipo: "Bloqueo", numeroProceso: data.numeroProceso, entidadId: data.entidadId, tipoIdentificacion: data.tipoIdentificacion, identificacion: data.identificacion,
                           cliente: data.cliente, monto: data.monto, productos: data.productos, observacion: data.observacion }
                       : { tipo: data.tipo, coactivaToken: data.coactivaToken, montoOrdenado: data.parcial ? data.montoOrdenado : null, observacion: data.observacion };
      const r = await pb.post(api("Oficios", "Orden", "token=" + oficio.token), cuerpo);
      if (r === undefined) return false;
      pb.notify(b ? "Coactiva " + r + " agregada al oficio " + oficio.numero : "Orden de " + ORD[data.tipo].corto.toLowerCase() + " de " + r + " agregada al oficio " + oficio.numero);
      listo();
    }, "Agregar", 640);
  }

  // ---------- Carga masiva (COA-04): se lee la hoja «Coactivas» del formato desde la fila 6 ----------
  function texto(celda) {
    const v = celda.value;
    if (v == null) return "";
    if (v instanceof Date) return String(v.getUTCDate()).padStart(2, "0") + "/" + String(v.getUTCMonth() + 1).padStart(2, "0") + "/" + v.getUTCFullYear();
    if (typeof v === "object") return v.richText ? v.richText.map(t => t.text).join("") : v.text != null ? String(v.text) : v.result != null ? String(v.result) : "";
    return String(v).trim();
  }
  async function leerExcel(archivo) {
    const wb = new ExcelJS.Workbook();
    await wb.xlsx.load(await archivo.arrayBuffer());
    const ws = wb.getWorksheet("Coactivas");
    if (!ws) throw new Error("El archivo no tiene la hoja «Coactivas» del formato.");
    const filas = [];
    for (let i = 6; i <= ws.rowCount; i++) {
      const r = ws.getRow(i), c = n => texto(r.getCell(n));
      const vals = Array.from({ length: 14 }, (_, k) => c(k + 1));
      if (vals.every(v => !v)) continue;
      filas.push({ fila: i, oficio: vals[0], recibido: vals[1], remitidoPor: vals[2], orden: vals[3], proceso: vals[4], ruc: vals[5], entidad: vals[6],
        tipoIdentificacion: vals[7], identificacion: vals[8], cliente: vals[9], monto: vals[10], productos: vals[11], montoOrdenado: vals[12], observacion: vals[13] });
    }
    return filas;
  }
  function cargaPopup(listo) {
    let archivo = null;
    pb.ui.openPopup("Carga masiva de oficios y coactivas (Excel)", $f => {
      $f.append($('<p class="muted" style="margin:0 0 8px">').text("Use el formato oficial: una fila por coactiva desde la fila 6 de la hoja «Coactivas». Las filas con el mismo N.º de oficio forman un solo oficio; si una fila tiene errores, su oficio no se carga."));
      const $a = $('<div class="actions" style="margin-bottom:10px">').appendTo($f);
      pb.ui.button($("<div>").appendTo($a), { text: "Descargar formato", icon: "download", onClick: () => { window.location.href = "/formatos/Formato_carga_masiva_coactivas.xlsx"; } });
      $("<div>").appendTo($f).dxFileUploader({ selectButtonText: "Elegir archivo", labelText: "o arrástrelo aquí", accept: ".xlsx", uploadMode: "useForm", multiple: false,
        allowedFileExtensions: [".xlsx"], invalidFileExtensionMessage: "Solo archivos .xlsx", onValueChanged: e => { archivo = (e.value || [])[0] || null; } });
      return null;
    }, async () => {
      if (!archivo) { pb.notify("Elija el archivo Excel", "warning"); return false; }
      let filas;
      try { filas = await leerExcel(archivo); } catch (e) { pb.notify(e.message || "No se pudo leer el archivo", "error"); return false; }
      if (!filas.length) { pb.notify("El archivo no tiene filas desde la fila 6", "warning"); return false; }
      const r = await pb.post(api("Oficios", "Carga"), { filas });
      if (r === undefined) return false;
      setTimeout(() => resultadoCarga(r, filas.length), 0);
      if (r.oficios > 0) listo();
    }, "Cargar", 560, { icono: "upload" });
  }
  function resultadoCarga(r, leidas) {
    pb.ui.openPopup("Resultado de la carga masiva", $f => {
      const $k = $('<div class="kpis">').appendTo($f);
      [["Filas leídas", leidas], ["Oficios cargados", r.oficios], ["Coactivas cargadas", r.ordenes], ["Filas con error", new Set(r.errores.map(e => e.fila)).size]]
        .forEach(x => $k.append($('<div class="card kpi">').append($("<span>").text(x[0])).append($('<strong class="kpim">').text(x[1]))));
      if (r.errores.length) pb.ui.mdGrid($f, "Filas con error (su oficio no se cargó)", [["fila", "Fila"], ["oficio", "Oficio"], ["motivo", "Motivo"]], r.errores);
      else $f.append('<p class="muted">Todas las filas se cargaron correctamente.</p>');
      return null;
    }, () => true, "Cerrar", 760);
    pb.notify(r.oficios + " oficio(s) y " + r.ordenes + " coactiva(s) cargados" + (r.errores.length ? " · " + r.errores.length + " error(es)" : ""), r.errores.length ? "warning" : "success");
  }

  // ---------- Catálogo de entidades (RUC, nombre y cuenta para transferencias) ----------
  async function entidadesPopup() {
    const datos = await pb.get(api("Oficios", "Entidades"));
    if (!datos) return;
    const editable = pb.can("oficios", "editar");
    pb.ui.openPopup("Entidades que emiten coactivas", $f => {
      $f.append($('<p class="muted" style="margin:0 0 8px">').text("El RUC identifica a la entidad en la carga masiva. La cuenta se propone al registrar una transferencia."));
      $("<div>").appendTo($f).dxDataGrid({
        dataSource: datos, keyExpr: "entidadId", showBorders: true, showRowLines: true, paging: { enabled: false }, height: 380, searchPanel: { visible: true, placeholder: "Buscar…" },
        editing: { mode: "row", allowAdding: editable, allowUpdating: editable, allowDeleting: false, useIcons: true },
        onInitNewRow: e => { e.data.estado = "Activo"; },
        onRowInserting: e => { e.cancel = guardar(null, e.data).then(ok => !ok); },
        onRowUpdating: e => { e.cancel = guardar(e.key, Object.assign({}, e.oldData, e.newData)).then(ok => !ok); },
        columns: [
          { dataField: "ruc", caption: "RUC", validationRules: [{ type: "required" }, { type: "pattern", pattern: /^\d{13}$/, message: "13 dígitos" }] },
          { dataField: "nombre", caption: "Entidad", validationRules: [{ type: "required" }] },
          { dataField: "cuentaDestino", caption: "Cuenta para transferencias" },
          { dataField: "estado", caption: "Estado", lookup: { dataSource: ["Activo", "Inactivo"] }, width: 110 }
        ]
      });
      return null;
    }, () => true, "Cerrar", 820);
    async function guardar(id, e) {
      const r = await pb.post(api("Oficios", "GuardarEntidad", id ? "id=" + id : ""), { ruc: e.ruc, nombre: e.nombre, cuentaDestino: e.cuentaDestino || null, estado: e.estado || "Activo" });
      if (r !== undefined) pb.notify("Entidad " + e.nombre + " guardada");
      return r !== undefined;
    }
  }

  window.coa = { ORD, PRODTIPOS, panel, ordenPopup, cargaPopup, entidadesPopup, ordenesResumen, listaProd, permTxt };
})();
