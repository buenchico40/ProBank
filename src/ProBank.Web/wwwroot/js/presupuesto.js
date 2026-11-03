// Presupuesto: piezas comunes de sus pantallas (partidas para elegir, aprobar o rechazar con motivo, ventana de ajuste o
// movimiento y documentos de solicitudes y pagos). Reglas COM-01 a COM-04 en spec/modulos/presupuesto.md.
(function () {
  "use strict";
  const url = (a, q) => "/Presupuesto/Datos/" + a + (q ? "?" + q : "");
  // Áreas solicitantes de una compra (las áreas que llevan presupuesto se configuran: ver areas())
  const AREAS = ["Gerencia de TI", "Aplicaciones", "Base de datos", "Core", "Soporte de aplicaciones", "Redes", "Servidores", "Helpdesk"];
  const DIAS_AVISO = [15, 30, 45, 60, 90, 120];
  // Documentos: tipo → extensiones (las verifica la API por su contenido)
  const DOCS = { "Contrato": [".pdf", ".doc", ".docx"], "Factura": [".pdf", ".xml"], "Distribución": [".xlsx", ".xls", ".csv", ".pdf"] };
  const iso = v => { if (!v) return null; const x = v instanceof Date ? v : pb.aFecha(v); return x.getFullYear() + "-" + String(x.getMonth() + 1).padStart(2, "0") + "-" + String(x.getDate()).padStart(2, "0"); };
  const tamano = n => n >= 1048576 ? (n / 1048576).toFixed(1).replace(".", ",") + " MB" : Math.max(1, Math.round(n / 1024)) + " KB";

  // Áreas que llevan presupuesto (Presupuesto › Áreas del presupuesto); por defecto solo las activas
  async function areas(todas) {
    return (await pb.get(url("Areas", todas ? "" : "soloActivas=true"))) || [];
  }

  // Partidas activas para elegir (con su área y disponible); las renovaciones aceptan también las del borrador del año siguiente
  async function partidas(incluirBorrador) {
    const ps = (await pb.get(url("Partidas"))) || [];
    return ps.filter(p => p.estado === "Activo" || (incluirBorrador && p.estado === "Borrador"))
      .map(p => ({ token: p.token, texto: p.codigo + " · " + p.area + " · " + p.rubro + " · disponible " + pb.dinero(p.disponible) + (p.estado === "Borrador" ? " (borrador)" : ""), p }));
  }

  // Aprobar (con confirmación) o rechazar (con motivo obligatorio): movimientos y solicitudes
  async function resolver(accionUrl, codigo, aprobar, listo) {
    if (aprobar) {
      if (!await pb.ui.confirmar("Aprobar " + codigo, "¿Desea aprobar <b>" + pb.esc(codigo) + "</b>?", "Aprobar", "default", "check")) return;
      const r = await pb.post(accionUrl, { aprobar: true });
      if (r !== undefined) { pb.notify(codigo + " · " + r); listo(); }
      return;
    }
    const data = { comentario: "" };
    pb.ui.openPopup("Rechazar " + codigo, $f => $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
      { dataField: "comentario", label: { text: "Motivo del rechazo" }, editorType: "dxTextArea", isRequired: true, editorOptions: { height: 80, maxLength: 500 } }] }).dxForm("instance"),
    async f => {
      if (!f.validate().isValid) return false;
      const r = await pb.post(accionUrl, { aprobar: false, comentario: data.comentario });
      if (r === undefined) return false;
      pb.notify(codigo + " · " + r); listo();
    }, "Rechazar", 480, { icono: "close" });
  }

  // Registrar o corregir un ajuste o movimiento (movimientoPopup del prototipo)
  async function movimientoPopup(m, partidaInicial, listo) {
    const ps = await partidas(false);
    if (!ps.length) return pb.notify("No hay partidas activas: apruebe primero el presupuesto", "warning");
    const data = m ? { tipo: m.tipo, origen: m.origenToken, destino: m.destinoToken, monto: m.monto, motivo: m.motivo, fecha: pb.aFecha(m.fecha) }
                   : { tipo: "Reasignación", origen: partidaInicial || null, destino: null, monto: null, motivo: "", fecha: new Date() };
    let form;
    const vis = () => { form.itemOption("origen", "visible", data.tipo !== "Incremento"); form.itemOption("destino", "visible", data.tipo !== "Reducción"); };
    pb.ui.openPopup(m ? "Corregir movimiento · " + m.codigo : "Registrar ajuste o movimiento de presupuesto", $f => (form = $("<div>").appendTo($f).dxForm({
      formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
        { dataField: "tipo", label: { text: "Tipo" }, editorType: "dxRadioGroup", editorOptions: { items: ["Reasignación", "Incremento", "Reducción"], layout: "horizontal", onValueChanged: () => setTimeout(vis, 0) } },
        { dataField: "origen", label: { text: "Partida origen (de donde sale el saldo)" }, editorType: "dxSelectBox", visible: data.tipo !== "Incremento",
          editorOptions: { dataSource: ps, valueExpr: "token", displayExpr: "texto", searchEnabled: true } },
        { dataField: "destino", label: { text: "Partida destino (a donde entra)" }, editorType: "dxSelectBox", visible: data.tipo !== "Reducción",
          editorOptions: { dataSource: ps, valueExpr: "token", displayExpr: "texto", searchEnabled: true } },
        { dataField: "monto", label: { text: "Monto" }, editorType: "dxNumberBox", isRequired: true, editorOptions: { format: "$ #,##0.00", min: 0.01 } },
        { dataField: "fecha", label: { text: "Fecha" }, editorType: "dxDateBox", isRequired: true, editorOptions: { displayFormat: "dd/MM/yyyy", max: new Date() } },
        { dataField: "motivo", label: { text: "Motivo" }, editorType: "dxTextArea", isRequired: true, editorOptions: { height: 60, maxLength: 500 } },
        { template: () => $('<p class="muted" style="margin:0">').text("Queda pendiente de aprobación; al aprobarse cambia el presupuesto de las partidas.") }
      ] }).dxForm("instance")), async f => {
      if (!f.validate().isValid) return false;
      const origen = data.tipo === "Incremento" ? null : data.origen, destino = data.tipo === "Reducción" ? null : data.destino;
      if ((data.tipo !== "Incremento" && !origen) || (data.tipo !== "Reducción" && !destino) || (origen && origen === destino))
        { pb.notify(data.tipo === "Reasignación" ? "Indique una partida origen y una destino distintas" : "Indique la partida", "warning"); return false; }
      const g = await pb.post(url("GuardarMovimiento", m ? "token=" + m.token : ""), { fecha: iso(data.fecha), tipo: data.tipo, origen, destino, monto: data.monto, motivo: data.motivo });
      if (g === undefined) return false;
      pb.notify(m ? "Movimiento " + m.codigo + " corregido" : "Movimiento registrado: queda pendiente de aprobación");
      if (listo) listo();
    }, m ? "Guardar" : "Registrar", 560);
  }

  // Documentos de una solicitud (contrato) o de un pago (facturas y distribución): lista, descarga, quitar y adjuntar al momento
  function documentos($el, o) {   // o: { dueno: "solicitudes" | "pagos", token, tipos: [...], docs: [...], editable, listo }
    if (!o.token) { $el.append($('<p class="muted" style="margin:0">').text("Guarde el registro para adjuntar sus documentos.")); return; }
    if (o.docs.length) pb.ui.mdGrid($el, "", [["t", "Tipo"], ["n", "Documento"], ["s", "Tamaño", "right"], ["u", "Subido por"], ["f", "Fecha"]],
      o.docs.map(d => ({ t: d.tipo, n: d.nombre, s: tamano(d.tamano), u: d.subidoPor, f: pb.fechaHora(d.fechaRegistro) })));
    else $el.append($('<p class="muted" style="margin:0">').text("Sin documentos adjuntos."));
    if (o.docs.length) {
      const $a = $('<div class="actions">').appendTo($el);
      o.docs.forEach(d => {
        pb.ui.button($("<div>").appendTo($a), { text: d.nombre, icon: "download", onClick: () => { window.location.href = url("Documento", "token=" + d.token); } });
        if (o.editable) pb.ui.button($("<div>").appendTo($a), { icon: "trash", hint: "Quitar " + d.nombre, aria: "Quitar " + d.nombre, onClick: async () => {
          if (!await pb.ui.confirmar("Quitar documento", "¿Desea quitar <b>" + pb.esc(d.nombre) + "</b>?", "Quitar", "danger", "trash")) return;
          if (await pb.post(url("EliminarDocumento", "token=" + d.token)) !== undefined) { pb.notify("Documento " + d.nombre + " quitado"); o.listo(); }
        } });
      });
    }
    if (!o.editable) return;
    const sel = { tipo: o.tipos[0] };
    let archivo = null, uploader;
    const $f = $('<div class="actions" style="align-items:center">').appendTo($el);
    if (o.tipos.length > 1) $("<div>").appendTo($f).dxSelectBox({ items: o.tipos, value: sel.tipo, width: 170, inputAttr: { "aria-label": "Tipo de documento" },
      onValueChanged: e => { sel.tipo = e.value; uploader.option({ accept: DOCS[sel.tipo].join(","), allowedFileExtensions: DOCS[sel.tipo] }); uploader.reset(); } });
    uploader = $("<div>").appendTo($f).dxFileUploader({ multiple: false, uploadMode: "useForm", accept: DOCS[sel.tipo].join(","), allowedFileExtensions: DOCS[sel.tipo],
      maxFileSize: 10 * 1048576, selectButtonText: "Elegir archivo", labelText: "", invalidFileExtensionMessage: "Tipo no permitido", invalidMaxFileSizeMessage: "Supera 10 MB",
      onValueChanged: e => { const f = (e.value || [])[0]; archivo = f && DOCS[sel.tipo].some(x => f.name.toLowerCase().endsWith(x)) && f.size <= 10 * 1048576 ? f : null; } }).dxFileUploader("instance");
    pb.ui.button($("<div>").appendTo($f), { text: "Adjuntar", icon: "upload", onClick: async () => {
      if (!archivo) return pb.notify("Elija el archivo (" + DOCS[sel.tipo].join(", ") + ")", "warning");
      const fd = new FormData(); fd.append("archivo", archivo, archivo.name);
      const r = await fetch(url("SubirDocumento", "dueno=" + o.dueno + "&token=" + o.token + "&tipo=" + encodeURIComponent(sel.tipo)),
        { method: "POST", headers: { RequestVerificationToken: pb.token() }, body: fd }).then(pb._leer);
      if (r !== undefined) { pb.notify(sel.tipo + " " + r.nombre + " adjuntado"); o.listo(); }
    } });
    $el.append($('<p class="muted" style="margin:4px 0 0">').text(o.tipos.map(t => t + ": " + DOCS[t].join(", ")).join(" · ") + " · hasta 10 MB"));
  }

  window.pres = { url, areas, AREAS, DIAS_AVISO, iso, partidas, resolver, movimientoPopup, documentos };
})();
