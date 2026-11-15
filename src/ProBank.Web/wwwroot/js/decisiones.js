// Decisiones: piezas comunes de Registro de decisiones y Mis aprobaciones (DEC-01 a DEC-07 en spec/modulos/decisiones.md):
// detalle con aprobadores, notificados, versiones y trazabilidad; firma (aprobar, rechazar o modificar) con la clave del
// aprobador y PDF firmado por el banco.
(function () {
  "use strict";
  const url = (a, q) => "/Decisiones/Datos/" + a + (q ? "?" + q : "");
  const corto = h => h ? h.substring(0, 16) + "…" : "—";
  const avance = d => d.totalAprobadores ? d.aprobadas + " de " + d.totalAprobadores : "—";
  // Descripción con formato (editor enriquecido). El servidor ya la guardó limpia; aquí se vuelve a limpiar antes de mostrarla y el
  // texto escrito sin editor se muestra como párrafos.
  function sanear(html) {
    const doc = new DOMParser().parseFromString(html || "", "text/html");
    doc.querySelectorAll("script,style,iframe,object,embed,link,meta,form,img").forEach(n => n.remove());
    doc.querySelectorAll("*").forEach(n => [...n.attributes].forEach(a => { if (/^on/i.test(a.name) || a.name === "style" || (/^(href|src)$/i.test(a.name) && /^\s*javascript:/i.test(a.value))) n.removeAttribute(a.name); }));
    doc.querySelectorAll("a").forEach(a => { a.setAttribute("target", "_blank"); a.setAttribute("rel", "noopener noreferrer"); });
    return doc.body.innerHTML;
  }
  const aHtml = s => /<[a-z][^>]*>/i.test(s || "") ? sanear(s) : (s || "").split(/\r?\n/).map(l => "<p>" + pb.esc(l) + "</p>").join("");
  const editorDescripcion = alto => ({ height: alto, elementAttr: { "aria-label": "Descripción de la decisión" }, toolbar: { multiline: true, items: [
    { name: "header", acceptedValues: [false, 3, 4] }, "separator", "bold", "italic", "underline", "strike", "separator",
    "bulletList", "orderedList", "blockquote", "separator", "link", "separator", "insertTable", "insertRowBelow", "deleteRow", "insertColumnRight", "deleteColumn", "separator", "undo", "redo"] },
    tableContextMenu: { enabled: true } });
  const iso = v => { if (!v) return null; const x = v instanceof Date ? v : pb.aFecha(v); return x.getFullYear() + "-" + String(x.getMonth() + 1).padStart(2, "0") + "-" + String(x.getDate()).padStart(2, "0"); };
  let usuarios = null;
  const cargarUsuarios = async () => usuarios || (usuarios = (await pb.get(url("Usuarios"))) || []);

  // Detalle desplegado o ficha. acciones: botones propios de la pantalla; listo: refrescar al cambiar algo; cargado: el detalle
  // que la ficha ya pidió (cada pedido queda como una consulta en la trazabilidad).
  async function detalle($e, d, acciones, listo, cargado) {
    pb.ui.mdHead($e, [d.codigo, d.titulo, d.tipo, d.area, "Fecha " + pb.fecha(d.fecha),
      d.fechaEnvio ? "Versión " + d.version : ""].filter(Boolean), d.estado);
    const x = cargado || await pb.get(url("Decision", "token=" + d.token));
    if (!x) return;
    const abierta = d.estado === "Borrador" || d.estado === "En aprobación";
    const gestiona = d.soyCreador && pb.can("decisiones", "editar");

    $e.append($('<div class="mdtitle">').text("Descripción"));
    $e.append($('<div class="dec-desc">').html(aHtml(x.descripcion)));
    if (x.motivoAnulacion) pb.ui.mdNote($e, "Motivo de la anulación: " + x.motivoAnulacion);

    // DEC-08: archivos de respaldo; el nombre abre la descarga (queda en la trazabilidad)
    $e.append($('<div class="mdtitle">').text("Archivos de respaldo"));
    const $arch = $('<div class="stack" style="gap:4px;margin:0 0 6px">').appendTo($e);
    if (!x.adjuntos.length) $arch.append($('<p class="muted" style="margin:0">').text(d.estado === "Borrador" ? "Sin archivos: agréguelos antes de enviarla." : "Sin archivos de respaldo."));
    x.adjuntos.forEach(a => $arch.append($("<div>")
      .append($('<a class="lnk">').attr("href", url("Archivo", "token=" + a.token)).text(a.nombre))
      .append($('<span class="muted">').text("  ·  " + tamano(a.tamano) + "  ·  SHA-256 " + corto(a.hash)))));
    if (gestiona && d.estado === "Borrador") {
      const b = [{ text: "Agregar archivos", icon: "attach", onClick: () => subir(d, listo) }];
      if (x.adjuntos.length) b.push({ text: "Quitar archivo", icon: "minus", onClick: () => quitarArchivo(d, x.adjuntos, listo) });
      pb.ui.mdActions($e, b);
      pb.ui.mdNote($e, "Al enviarla, los archivos quedan fijos: ya no se agregan ni se quitan.");
    }

    pb.ui.mdGrid($e, "Aprobadores y firmas" + (d.secuencial ? " (en secuencia)" : " (todos a la vez)"),
      [["o", "Orden"], ["a", "Aprobador"], ["r", "Resultado"], ["f", "Firmada"], ["c", "Comentario"], ["s", "Sello SHA-256"], ["v", "Verificación"]],
      x.aprobadores.map(a => ({ o: a.orden, a: a.nombre + " (" + a.usuario + ")" + (a.origen === "Agregado" ? " · agregado" : ""), r: a.resultado,
        f: a.fecha ? pb.fechaHora(a.fecha) + " · " + (a.ip || "—") : "—", c: a.comentario || "—", s: corto(a.sello),
        v: a.selloValido == null ? "—" : a.selloValido ? "Válido" : "No coincide" })),
      "Sin aprobadores: agregue al menos uno antes de enviarla.");
    if (gestiona && abierta) {
      const b = [{ text: "Agregar aprobador", icon: "add", onClick: () => agregar(d, "aprobadores", x, listo) }];
      if (!x.tieneFirmas && x.aprobadores.length) b.push({ text: "Quitar aprobador", icon: "minus", onClick: () => quitar(d, "aprobadores", x.aprobadores, listo) });
      pb.ui.mdActions($e, b);
      if (x.tieneFirmas) pb.ui.mdNote($e, "Ya hay firmas en esta decisión: los aprobadores no se quitan, solo se agregan.");
    }

    pb.ui.mdGrid($e, "Notificados (solo la ven)", [["n", "Notificado"], ["d", "Desde"], ["p", "Agregado por"], ["u", "Última consulta"]],
      x.notificados.map(n => ({ n: n.nombre + " (" + n.usuario + ")", d: pb.fechaHora(n.desde), p: n.agregadoPor, u: n.ultimaConsulta ? pb.fechaHora(n.ultimaConsulta) : "Aún no la abre" })),
      "Sin notificados.");
    if (gestiona && d.estado !== "Anulada") {
      const b = [{ text: "Agregar notificado", icon: "add", onClick: () => agregar(d, "notificados", x, listo) }];
      if (d.estado === "Borrador" && x.notificados.length) b.push({ text: "Quitar notificado", icon: "minus", onClick: () => quitar(d, "notificados", x.notificados, listo) });
      pb.ui.mdActions($e, b);
    }

    if (x.huella) {
      const $i = $('<div class="cred">').appendTo($e);
      $i.append($('<div class="cred-l">').append($("<b>").text("Huella de la versión " + d.version)).append($('<span class="cred-v" style="letter-spacing:0">').text(corto(x.huella))));
      $i.append($('<div class="cred-m">').text(x.integra ? "Coincide con el contenido actual: no cambió desde que se selló" : "NO coincide con el contenido actual: el contenido cambió por fuera de ProBank"));
      $i.append($("<span>").addClass(x.integra ? "pill pill-g" : "pill pill-r").text(x.integra ? "Íntegra" : "Alterada"));
    }
    if (x.versiones.length > 1) {
      pb.ui.mdGrid($e, "Versiones", [["v", "Versión"], ["f", "Fecha"], ["p", "Por"], ["m", "Motivo"], ["t", "Título"], ["h", "Huella"]],
        x.versiones.map(v => ({ v: "v" + v.version, f: pb.fechaHora(v.fechaRegistro), p: v.registradaPor, m: v.motivo || "—", t: v.titulo, h: corto(v.huella) })));
      if (x.firmasAnteriores.length) pb.ui.mdGrid($e, "Firmas de versiones anteriores", [["v", "Versión"], ["a", "Aprobador"], ["r", "Resultado"], ["f", "Fecha"], ["s", "Sello"], ["ok", "Verificación"]],
        x.firmasAnteriores.map(f => ({ v: "v" + f.version, a: f.nombre, r: f.resultado, f: pb.fechaHora(f.fecha), s: corto(f.sello), ok: f.selloValido ? "Válido" : "No coincide" })));
    }
    pb.ui.mdGrid($e, "Trazabilidad", [["f", "Fecha y hora"], ["u", "Usuario"], ["e", "Evento"], ["d", "Detalle"]],
      x.eventos.slice(0, 50).map(ev => ({ f: pb.fechaHora(ev.fecha), u: ev.nombre, e: ev.evento, d: ev.detalle || "—" })));

    const acc = (acciones || []).slice();
    if (d.soyCreador || d.soyAprobador || pb.can("decisiones", "exportar")) acc.push({ text: "PDF firmado", icon: "textdocument", onClick: () => pdf(d) });
    pb.ui.mdActions($e, acc);
    return x;
  }

  const tamano = n => n >= 1048576 ? (n / 1048576).toFixed(1).replace(".", ",") + " MB" : Math.max(1, Math.round(n / 1024)) + " KB";

  // DEC-08: subir uno o varios archivos (la API verifica tipo, tamaño y contenido)
  function subir(d, listo) {
    $('<input type="file" multiple>').on("change", async function () {
      const archivos = Array.from(this.files || []);
      let n = 0;
      for (const f of archivos) {
        const fd = new FormData(); fd.append("archivo", f, f.name);
        const r = await fetch(url("SubirArchivo", "token=" + d.token), { method: "POST", headers: { RequestVerificationToken: pb.token() }, body: fd }).then(pb._leer);
        if (r !== undefined) n++;
      }
      if (n) { pb.notify(n + (n === 1 ? " archivo agregado" : " archivos agregados") + " a " + d.codigo); if (listo) listo(); }
    }).trigger("click");
  }

  function quitarArchivo(d, archivos, listo) {
    const data = { token: null };
    pb.ui.openPopup("Quitar archivo · " + d.codigo, $f => $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
      { dataField: "token", label: { text: "Archivo" }, editorType: "dxSelectBox", isRequired: true,
        editorOptions: { items: archivos.map(a => ({ id: a.token, t: a.nombre })), valueExpr: "id", displayExpr: "t" } }] }).dxForm("instance"),
    async f => {
      if (!f.validate().isValid) return false;
      const r = await pb.post(url("QuitarArchivo", "token=" + data.token));
      if (r === undefined) return false;
      pb.notify(d.codigo + " · " + r); if (listo) listo();
    }, "Quitar", 460, { icono: "minus" });
  }

  // Agregar un aprobador (con su turno) o un notificado
  async function agregar(d, rol, x, listo) {
    const us = await cargarUsuarios();
    const ya = new Set((rol === "aprobadores" ? x.aprobadores : x.notificados).map(p => p.usuarioId));
    const data = { usuarioId: null, orden: (x.aprobadores.length ? Math.max(...x.aprobadores.map(a => a.orden)) : 0) + 1 };
    pb.ui.openPopup((rol === "aprobadores" ? "Agregar aprobador · " : "Agregar notificado · ") + d.codigo, $f => $("<div>").appendTo($f).dxForm({ formData: data,
      labelLocation: "top", showColonAfterLabel: false, items: [
        { template: () => $('<p class="muted" style="margin:0">').text(rol === "aprobadores"
            ? "El aprobador agregado firma esta versión; en secuencia, según su orden. Queda en la trazabilidad y ya no se podrá quitar si alguien firmó."
            : "El notificado podrá ver la decisión (sin firmar); cada vez que la abra queda en la trazabilidad.") },
        { dataField: "usuarioId", label: { text: rol === "aprobadores" ? "Aprobador" : "Notificado" }, editorType: "dxSelectBox", isRequired: true,
          editorOptions: { items: us.filter(u => !ya.has(u.usuarioId)).map(u => ({ id: u.usuarioId, t: u.nombre + " (" + u.usuario + ")" })), valueExpr: "id", displayExpr: "t", searchEnabled: true } },
        { dataField: "orden", label: { text: "Orden (turno)" }, editorType: "dxNumberBox", visible: rol === "aprobadores", editorOptions: { min: 1, max: 99, showSpinButtons: true } }
      ] }).dxForm("instance"), async f => {
      if (!f.validate().isValid) return false;
      const r = await pb.post(url("AgregarParticipante", "token=" + d.token + "&rol=" + rol), { usuarioId: data.usuarioId, orden: rol === "aprobadores" ? data.orden : null });
      if (r === undefined) return false;
      pb.notify(d.codigo + " · " + r); if (listo) listo();
    }, "Agregar", 480, { icono: "add" });
  }

  function quitar(d, rol, personas, listo) {
    const data = { usuarioId: null };
    pb.ui.openPopup((rol === "aprobadores" ? "Quitar aprobador · " : "Quitar notificado · ") + d.codigo, $f => $("<div>").appendTo($f).dxForm({ formData: data,
      labelLocation: "top", showColonAfterLabel: false, items: [
        { dataField: "usuarioId", label: { text: rol === "aprobadores" ? "Aprobador" : "Notificado" }, editorType: "dxSelectBox", isRequired: true,
          editorOptions: { items: personas.map(p => ({ id: p.usuarioId, t: p.nombre + " (" + p.usuario + ")" })), valueExpr: "id", displayExpr: "t" } }
      ] }).dxForm("instance"), async f => {
      if (!f.validate().isValid) return false;
      const r = await pb.post(url("QuitarParticipante", "token=" + d.token + "&rol=" + rol + "&usuarioId=" + data.usuarioId));
      if (r === undefined) return false;
      pb.notify(d.codigo + " · " + r); if (listo) listo();
    }, "Quitar", 460, { icono: "minus" });
  }

  // DEC-03: firmar con la clave del usuario; al rechazar, el motivo es obligatorio
  function firmar(d, aprobar, listo) {
    const data = { clave: "", comentario: "" };
    pb.ui.openPopup((aprobar ? "Aprobar · " : "Rechazar · ") + d.codigo, $f => {
      $f.append($('<p class="muted" style="margin:0 0 8px">').text((aprobar ? "Firma la aprobación" : "Firma el rechazo") + " de la versión " + d.version + " de «" + d.titulo +
        "». Su clave confirma que es usted quien firma; la firma queda sellada con la huella del contenido, la fecha, la hora y su IP, y no se puede deshacer."));
      return $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
        { dataField: "comentario", label: { text: aprobar ? "Comentario (opcional)" : "Motivo del rechazo" }, editorType: "dxTextArea", isRequired: !aprobar,
          editorOptions: { height: 70, maxLength: 500 } },
        { dataField: "clave", label: { text: "Su clave de ProBank" }, isRequired: true, editorOptions: { mode: "password", inputAttr: { autocomplete: "current-password" } } }
      ] }).dxForm("instance");
    }, async f => {
      if (!f.validate().isValid) return false;
      const r = await pb.post(url("Firmar", "token=" + d.token), { aprobar, clave: data.clave, comentario: data.comentario || null });
      data.clave = "";
      if (r === undefined) return false;
      pb.notify(d.codigo + " · " + r);
      if (listo) listo();
    }, aprobar ? "Firmar aprobación" : "Firmar rechazo", 520, { icono: aprobar ? "check" : "close" });
  }

  // DEC-07: el aprobador corrige el contenido; nace una versión nueva y todos vuelven a firmar
  async function modificar(d, listo) {
    const x = await pb.get(url("Decision", "token=" + d.token));
    if (!x) return;
    const data = { titulo: d.titulo, descripcion: aHtml(x.descripcion), area: d.area, fecha: pb.aFecha(d.fecha), motivo: "", clave: "" };
    pb.ui.openPopup("Modificar · " + d.codigo + " (versión " + d.version + " → " + (d.version + 1) + ")", $f => {
      $f.append($('<p class="muted" style="margin:0 0 8px">').text("Corrija el contenido e indique el motivo. Al guardar nace la versión " + (d.version + 1) +
        " con una huella nueva: las firmas de la versión actual quedan en el historial y todos los aprobadores vuelven a firmar, desde el primero."));
      return $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, colCount: 2, items: [
        { dataField: "titulo", label: { text: "Título" }, colSpan: 2, isRequired: true, editorOptions: { maxLength: 200 } },
        { dataField: "area", label: { text: "Área" }, isRequired: true, editorOptions: { maxLength: 100 } },
        { dataField: "fecha", label: { text: "Fecha de la decisión" }, editorType: "dxDateBox", isRequired: true, editorOptions: { displayFormat: "dd/MM/yyyy" } },
        { dataField: "descripcion", label: { text: "Descripción (alcance, montos y condiciones)" }, colSpan: 2, isRequired: true, editorType: "dxHtmlEditor", editorOptions: editorDescripcion(220) },
        { dataField: "motivo", label: { text: "Motivo de la modificación" }, colSpan: 2, isRequired: true, editorType: "dxTextArea", editorOptions: { height: 60, maxLength: 500 } },
        { dataField: "clave", label: { text: "Su clave de ProBank" }, isRequired: true, editorOptions: { mode: "password", inputAttr: { autocomplete: "current-password" } } }
      ] }).dxForm("instance");
    }, async f => {
      if (!f.validate().isValid) return false;
      const r = await pb.post(url("Modificar", "token=" + d.token), { titulo: data.titulo, descripcion: data.descripcion, area: data.area,
        fecha: iso(data.fecha), motivo: data.motivo, clave: data.clave });
      data.clave = "";
      if (r === undefined) return false;
      pb.notify(d.codigo + " · " + r);
      if (listo) listo();
    }, "Firmar modificación", 760, { icono: "edit" });
  }

  // Acciones del aprobador cuando le toca firmar
  const accionesFirma = (d, listo) => d.esMiTurno && pb.can("pendientes", "aprobar") ? [
    { text: "Aprobar", icon: "check", type: "default", onClick: () => firmar(d, true, listo) },
    { text: "Rechazar", icon: "close", onClick: () => firmar(d, false, listo) },
    { text: "Modificar", icon: "edit", onClick: () => modificar(d, listo) }] : [];

  // DEC-04: PDF con el detalle, las firmas y la trazabilidad, firmado con el certificado del banco
  function pdf(d) {
    const src = q => url("Pdf", "token=" + d.token + (q || ""));
    pb.ui.openPopup("Decisión " + d.codigo + " · PDF firmado", $f => {
      $("<iframe>").attr({ src: src(), title: "Decisión " + d.codigo })
        .css({ width: "100%", height: "min(72vh, 760px)", border: "1px solid var(--line)", borderRadius: "8px", background: "#fff" }).appendTo($f);
      return null;
    }, () => true, "Cerrar", 1100, { extra: [
      { text: "Descargar PDF", icon: "download", onClick: () => { window.location.href = src("&descargar=true"); } },
      { text: "Abrir en otra pestaña", icon: "export", onClick: () => window.open(src(), "_blank") }
    ] });
  }

  window.decisiones = { url, iso, avance, cargarUsuarios, aHtml, editorDescripcion, detalle, firmar, modificar, accionesFirma, pdf };
})();
