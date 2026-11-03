// Portafolio: componentes del prototipo compartidos por sus pantallas (tareaPopup, acuerdoPopup, detalle de la tarea).
(function () {
  "use strict";
  const url = (a, q) => "/Portafolio/Datos/" + a + (q ? "?" + q : "");
  const AREAS = ["Base de datos", "Aplicaciones", "Core", "Soporte de aplicaciones", "Redes", "Servidores", "Helpdesk"];
  const ESTADO = { Pendiente: "Pendiente", EnRevision: "En revisión", EnCurso: "En curso", Completada: "Completada" };
  const iso = v => { if (!v) return null; const x = v instanceof Date ? v : new Date(v); return x.getFullYear() + "-" + String(x.getMonth() + 1).padStart(2, "0") + "-" + String(x.getDate()).padStart(2, "0"); };
  // Fechas "yyyy-MM-dd" de la API como fecha local (sin corrimiento por zona horaria)
  const fechaLocal = s => { if (!s) return null; const p = String(s).substring(0, 10).split("-"); return new Date(+p[0], +p[1] - 1, +p[2]); };
  const hoy = () => { const d = new Date(); return new Date(d.getFullYear(), d.getMonth(), d.getDate()); };
  let usuariosCache = null;
  const usuarios = async () => usuariosCache || (usuariosCache = (await pb.get(url("Usuarios"))) || []);
  const titulo = i => i.codigo + " · " + i.titulo.replace(/^[^:]+: /, "") + " (" + i.tipo + ")";

  // ---------- Nueva tarea (tareaPopup del prototipo): PRY-03 y PRY-04 ----------
  async function tareaPopup(itemToken, listo) {
    const [items, users] = await Promise.all([pb.get(url("Items", "abiertos=true")), usuarios()]);
    if (!items) return;
    const abiertos = items.filter(i => i.estado !== "Borrador");
    if (!abiertos.length) return pb.notify("No hay proyectos, mejoras o bugs abiertos para agregar tareas", "warning");
    const data = { item: abiertos.some(i => i.token === itemToken) ? itemToken : null, hitoId: null, titulo: "", area: null, tecnicoId: null, orden: 1,
                   inicio: hoy(), fecha: null, aprob: [] };
    let form, editor, docs, hitos = [];
    const cargarItem = async () => {
      if (!data.item) return;
      const [d, ts] = await Promise.all([pb.get(url("Item", "token=" + data.item)), pb.get(url("Tareas", "item=" + data.item))]);
      hitos = d ? d.hitos : [];
      const ed = form && form.getEditor("hitoId"); if (ed) { ed.option("dataSource", [{ hitoId: null, nombre: "(Sin hito)" }].concat(hitos)); ed.option("value", null); }
      if (ts) form.updateData("orden", ts.length + 1);
    };
    const puedeAprob = true; // el SP valida «Exigir aprobación» (PRY-09) al guardar
    pb.ui.openPopup("Nueva tarea", $f => {
      form = $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, colCountByScreen: { lg: 2, md: 2, sm: 1, xs: 1 }, items: [
        { dataField: "item", label: { text: "Proyecto, mejora o bug" }, editorType: "dxSelectBox", isRequired: true, colSpan: 2,
          editorOptions: { dataSource: abiertos.map(i => ({ k: i.token, t: titulo(i) })), valueExpr: "k", displayExpr: "t", searchEnabled: true, onValueChanged: () => setTimeout(cargarItem, 0) } },
        { dataField: "titulo", label: { text: "Título de la tarea" }, isRequired: true, colSpan: 2, editorOptions: { maxLength: 200 } },
        { dataField: "hitoId", label: { text: "Hito (opcional)" }, editorType: "dxSelectBox", editorOptions: { dataSource: [{ hitoId: null, nombre: "(Sin hito)" }], valueExpr: "hitoId", displayExpr: "nombre" } },
        { dataField: "orden", label: { text: "Orden" }, editorType: "dxNumberBox", isRequired: true, editorOptions: { min: 1, showSpinButtons: true } },
        { dataField: "area", label: { text: "Área" }, editorType: "dxSelectBox", isRequired: true, editorOptions: { items: AREAS } },
        { dataField: "tecnicoId", label: { text: "Técnico asignado" }, editorType: "dxSelectBox", isRequired: true, editorOptions: { dataSource: users, valueExpr: "usuarioId", displayExpr: "nombre", searchEnabled: true } },
        { dataField: "inicio", label: { text: "Fecha de inicio" }, editorType: "dxDateBox", isRequired: true, editorOptions: { displayFormat: "dd/MM/yyyy" } },
        { dataField: "fecha", label: { text: "Fecha de terminación" }, editorType: "dxDateBox", isRequired: true, editorOptions: { displayFormat: "dd/MM/yyyy", min: hoy() },
          validationRules: [{ type: "custom", message: "No puede ser anterior al inicio", validationCallback: o => !o.value || !data.inicio || o.value >= data.inicio }] },
        { dataField: "aprob", label: { text: "Aprobadores de la tarea (opcional)" }, editorType: "dxTagBox", colSpan: 2, visible: puedeAprob,
          editorOptions: { dataSource: users, valueExpr: "usuarioId", displayExpr: "nombre", showSelectionControls: true, applyValueMode: "instantly", searchEnabled: true, placeholder: "Ninguno: la tarea no requiere aprobación" } },
        { colSpan: 2, template: () => $('<p class="muted" style="margin:0">').text("Puede no tener aprobadores o tener varios. Si tiene más de uno, todos deben aprobar, en cualquier orden, para que la tarea pueda ejecutarse.") }
      ] }).dxForm("instance");
      $f.append('<span class="lbl" style="display:block;margin-top:8px">Descripción</span>');
      editor = $("<div>").appendTo($f).dxHtmlEditor({ height: 120, toolbar: { items: ["bold", "italic", "underline", "separator", "bulletList", "orderedList", "separator", "link"] } }).dxHtmlEditor("instance");
      // Documentos de la tarea (opcionales): se suben en cuanto la tarea se crea
      $f.append('<span class="lbl" style="display:block;margin-top:12px">Documentos adjuntos (opcional)</span>');
      docs = adjuntos($("<div>").appendTo($f), { ambito: "Tarea", token: null, editable: true });
      if (data.item) setTimeout(cargarItem, 0);
      return form;
    }, async f => {
      if (!f.validate().isValid || !docs.validar()) return false;
      const r = await pb.post(url("CrearTarea"), { itemToken: data.item, hitoId: data.hitoId, titulo: data.titulo, area: data.area, tecnicoId: data.tecnicoId, orden: data.orden,
        fechaInicio: iso(data.inicio), fechaTerminacion: iso(data.fecha), aprobadorIds: data.aprob, descripcionHtml: editor.option("value") || null });
      if (r === undefined) return false;
      const tec = users.find(u => u.usuarioId === data.tecnicoId), it = abiertos.find(i => i.token === data.item);
      const nom = id => (users.find(u => u.usuarioId === id) || {}).nombre;
      pb.notify("Tarea " + r.codigo + " creada en " + it.codigo + " y asignada a " + tec.nombre + (data.aprob.length > 1 ? " · requiere la aprobación de sus " + data.aprob.length + " aprobadores" : data.aprob.length ? " · la aprueba " + nom(data.aprob[0]) : " · sin aprobadores"));
      // La tarea ya existe: si algún documento falla, se avisa y se puede adjuntar después desde la tarea
      if (!await docs.subir(r.token)) pb.notify("La tarea " + r.codigo + " se creó, pero algún documento no se adjuntó: adjúntelo desde la tarea", "warning");
      if (listo) listo();
    }, "Crear tarea", 680);
  }

  // ---------- Revisión semanal con sus acuerdos (acuerdoPopup del prototipo): PRY-08 ----------
  async function acuerdoPopup(itemToken, listo) {
    const [items, users] = await Promise.all([pb.get(url("Items", "abiertos=true")), usuarios()]);
    if (!items) return;
    const elegibles = items.filter(i => i.estado !== "Anulado" && i.estado !== "Borrador");
    if (!elegibles.length) return pb.notify("No hay proyectos, mejoras o bugs activos", "warning");
    const data = { item: elegibles.some(i => i.token === itemToken) ? itemToken : null, fecha: hoy(), temas: "" };
    const filas = [{ id: 1, desc: "", resp: null, fecha: null }];
    let grid = null, form = null;
    pb.ui.openPopup("Registrar acuerdo semanal", $f => {
      form = $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, colCountByScreen: { lg: 2, md: 2, sm: 1, xs: 1 }, items: [
        { dataField: "item", label: { text: "Proyecto, mejora o bug" }, editorType: "dxSelectBox", isRequired: true, editorOptions: { dataSource: elegibles.map(i => ({ k: i.token, t: titulo(i) })), valueExpr: "k", displayExpr: "t", searchEnabled: true } },
        { dataField: "fecha", label: { text: "Fecha de la revisión" }, editorType: "dxDateBox", isRequired: true, editorOptions: { displayFormat: "dd/MM/yyyy" } },
        { dataField: "temas", label: { text: "Temas tratados (queda en la bitácora del ítem)" }, editorType: "dxTextArea", colSpan: 2, editorOptions: { height: 56, maxLength: 2000 } }
      ] }).dxForm("instance");
      $f.append('<div class="mdtitle" style="margin-top:6px">Acuerdos</div>');
      grid = $("<div>").appendTo($f).dxDataGrid({
        dataSource: filas, keyExpr: "id", showBorders: true, showRowLines: true, paging: { enabled: false }, columnAutoWidth: false, wordWrapEnabled: true,
        editing: { mode: "cell", allowAdding: true, allowUpdating: true, allowDeleting: true, confirmDelete: false, newRowPosition: "last", texts: { addRow: "Agregar acuerdo" } },
        toolbar: { items: [{ name: "addRowButton", showText: "always" }] },
        onInitNewRow: e => { e.data.id = Math.max(0, ...filas.map(x => x.id)) + 1; },
        columns: [{ dataField: "desc", caption: "Acuerdo", minWidth: 220, validationRules: [{ type: "required" }] },
          { dataField: "resp", caption: "Responsable", width: 190, lookup: { dataSource: users, valueExpr: "usuarioId", displayExpr: "nombre" }, validationRules: [{ type: "required" }] },
          { dataField: "fecha", caption: "Fecha de cumplimiento", dataType: "date", format: "dd/MM/yyyy", width: 150, validationRules: [{ type: "required" }] },
          { type: "buttons", width: 50, buttons: ["delete"] }]
      }).dxDataGrid("instance");
      $f.append($('<p class="muted" style="margin:8px 0 0">').text("Cada acuerdo queda en la bitácora del ítem y entra en el informe semanal. Si vence sin cumplirse, aparece como Vencido."));
      return form;
    }, async () => {
      if (!form.validate().isValid) return false;
      await grid.saveEditData();
      const ok = filas.filter(x => String(x.desc || "").trim());
      if (!ok.length) { pb.notify("Agregue al menos un acuerdo", "warning"); return false; }
      if (ok.some(x => !x.resp || !x.fecha)) { pb.notify("Cada acuerdo necesita responsable y fecha de cumplimiento", "warning"); return false; }
      const n = await pb.post(url("RegistrarAcuerdos"), { itemToken: data.item, fecha: iso(data.fecha), temas: data.temas || null,
        acuerdos: ok.map(x => ({ descripcion: x.desc.trim(), responsableId: x.resp, fecha: iso(x.fecha) })) });
      if (n === undefined) return false;
      const it = elegibles.find(i => i.token === data.item);
      pb.notify(n + " acuerdo(s) registrado(s) para " + it.codigo + " en la revisión del " + pb.fecha(data.fecha));
      if (listo) listo();
    }, "Registrar acuerdos", 760);
  }

  // ---------- Detalle desplegable de la tarea ----------
  async function detalleTarea($e, t, listo) {
    const est = ESTADO[t.estado] || t.estado;
    pb.ui.mdHead($e, [t.codigo, "Ítem " + t.item, "Hito: " + (t.hito || "—"), "Área: " + t.area, "Técnico: " + t.tecnico,
      "Del " + pb.fecha(fechaLocal(t.fechaInicio)) + " al " + pb.fecha(fechaLocal(t.fechaTerminacion)), "Avance " + t.avance + " %"], est);
    const d = await pb.get(url("Tarea", "token=" + t.token));
    if (!d) return;
    const desc = d.descripcionHtml ? (new DOMParser().parseFromString(d.descripcionHtml, "text/html").body.textContent || "").trim() : "";
    pb.ui.mdNote($e, (desc ? "Descripción: " + desc + ". " : "") + (t.aprobadores.length > 1 ? "Aprobadores: " + t.aprobadores.join(", ") + " · deben aprobar todos antes de ejecutarla."
      : t.aprobadores.length ? "Aprobador: " + t.aprobadores[0] + "." : "Sin aprobadores: se ejecuta sin aprobación."));
    if (d.aprobadores.length) pb.ui.mdGrid($e, "Aprobadores (" + t.avanceAprobacion + ")", [["a", "Aprobador"], ["u", "Usuario"], ["d", "Decisión"], ["f", "Fecha"], ["c", "Comentario"]],
      d.aprobadores.map(a => ({ a: a.nombre, u: a.usuario, d: a.decision || "Pendiente", f: a.fecha ? pb.fechaHora(a.fecha) : "—", c: a.comentario || "—" })));
    pb.ui.mdGrid($e, "Historial", [["f", "Fecha"], ["e", "Evento"], ["d", "Detalle"], ["u", "Usuario"]],
      d.historial.map(h => ({ f: pb.fechaHora(h.fecha), e: h.evento, d: h.detalle || "—", u: h.usuario })), "Sin eventos.");
    // El técnico asignado envía y completa su tarea y maneja sus documentos; los demás necesitan poder editar tareas (lo valida la API)
    const puede = t.tecnicoId === pb.perfil.usuarioId || pb.can("tareas", "editar");
    $e.append($('<div class="mdtitle">').text("Documentos adjuntos"));
    adjuntos($("<div>").appendTo($e), { ambito: "Tarea", token: t.token, editable: puede, inmediato: true });
    if (!listo || !puede) return;
    // Sin aprobadores, «enviar» no pide aprobación: la tarea pasa directo a En curso
    const sinAprob = !t.aprobadores.length;
    pb.ui.mdActions($e, [
      { text: sinAprob ? "Iniciar tarea" : "Enviar a aprobar", icon: sinAprob ? "video" : "send", type: "default", disabled: t.estado !== "Pendiente", onClick: () => accion(t, "enviar", listo) },
      { text: "Marcar completada", icon: "check", disabled: t.estado !== "EnCurso", onClick: () => accion(t, "completar", listo) }
    ]);
    const motivo = {
      Pendiente: sinAprob ? "Para completarla, pulse primero «Iniciar tarea»: no tiene aprobadores, así que pasa directo a En curso. Cambiar el avance a 100 % no la completa."
                          : "Para completarla, primero envíela a aprobar; cuando la aprueben todos sus aprobadores pasa a En curso.",
      EnRevision: "Está en revisión: se podrá completar cuando la aprueben todos sus aprobadores (Portafolio › Aprobaciones)."
    }[t.estado];
    if (motivo) pb.ui.mdNote($e, motivo);
  }

  async function accion(t, a, listo, cuerpo) {
    const r = await pb.post(url("AccionTarea", "token=" + t.token + "&accion=" + a), cuerpo || null);
    if (r === undefined) return;
    const msj = {
      enviar: r === "En curso" ? "La tarea " + t.codigo + " no tiene aprobadores asignados: pasa directamente a En curso"
        : "Tarea " + t.codigo + " enviada a aprobación: " + (t.aprobadores.length > 1 ? "la deben aprobar todos sus " + t.aprobadores.length + " aprobadores (" + t.aprobadores.join(", ") + ")" : "la aprueba " + t.aprobadores[0]),
      completar: "Tarea " + t.codigo + " completada",
      aprobar: "Su aprobación quedó registrada (" + r + ")",
      rechazar: "Tarea " + t.codigo + " rechazada: vuelve al técnico con el comentario"
    }[a];
    pb.notify(msj);
    listo();
  }

  // Por qué un ítem no admite tareas ni acuerdos (null = sí los admite)
  function motivoSinTareas(estado) {
    if (estado === "Borrador") return "Este ítem está en Borrador: para agregar tareas o acuerdos, edítelo y cambie su estado a Pendiente o En curso.";
    if (estado === "Completado" || estado === "Anulado") return "Este ítem está " + estado + ": ya no admite tareas ni acuerdos nuevos.";
    if (!pb.can("tareas", "crear")) return "Su rol activo (" + pb.sesion.rolActivo + ") no tiene permiso para crear tareas (Seguridad › Roles y permisos).";
    return null;
  }

  // ---------- Documentos adjuntos de un ítem o de una tarea (tipos y tamaño según Portafolio › Parametrización) ----------
  const tamano = n => n >= 1048576 ? (n / 1048576).toFixed(1).replace(".", ",") + " MB" : Math.max(1, Math.round(n / 1024)) + " KB";
  let parametrosCache = null;
  const parametrosAdjunto = async () => parametrosCache || (parametrosCache = await pb.get(url("ParametrosAdjunto")));

  // o: { ambito: "Item" | "Tarea", token (null = ítem nuevo), editable, inmediato }. Sin «inmediato», los archivos
  // elegidos se suben al guardar el formulario con subir(token); con «inmediato», con su propio botón «Adjuntar».
  // Cada documento lleva su descripción (obligatoria): validar() la revisa antes de guardar.
  function adjuntos($el, o) {
    let pendientes = [], uploader = null, token = o.token, $pend = null;   // pendientes: [{ f: File, d: descripción }]
    const $lista = $("<div>").appendTo($el);
    const editable = () => o.editable && (o.ambito === "Tarea" || pb.can("proyectos", "editar"));
    async function cargar() {
      if (!token) return;
      const docs = await pb.get(url("Adjuntos", "ambito=" + o.ambito + "&token=" + token));
      if (!docs) return;
      $lista.empty();
      if (!docs.length) { $lista.append($('<p class="muted" style="margin:0">').text("Sin documentos adjuntos.")); return; }
      $("<div>").appendTo($lista).dxDataGrid({
        dataSource: docs, keyExpr: "token", showBorders: true, showRowLines: true, wordWrapEnabled: true, paging: { enabled: false },
        columns: [
          { dataField: "nombre", caption: "Documento", cellTemplate: ($c, i) => $('<a class="lnk">').attr("href", url("Descargar", "token=" + i.data.token)).text(i.value).appendTo($c) },
          { dataField: "descripcion", caption: "Descripción", minWidth: 200 },
          { dataField: "tamano", caption: "Tamaño", width: 100, alignment: "right", calculateCellValue: d => tamano(d.tamano) },
          { dataField: "subidoPor", caption: "Subido por" },
          { dataField: "fechaRegistro", caption: "Fecha", width: 150, calculateCellValue: d => pb.fechaHora(d.fechaRegistro) }
        ].concat(editable() ? [{ type: "buttons", width: 84, buttons: [
          { hint: "Editar descripción", icon: "edit", onClick: e => describir(e.row.data) },
          { hint: "Quitar documento", icon: "trash", onClick: e => quitar(e.row.data) }] }] : [])
      });
    }
    function describir(d) {
      const data = { descripcion: d.descripcion };
      pb.ui.openPopup("Descripción · " + d.nombre, $f => $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
        { dataField: "descripcion", label: { text: "Descripción del documento" }, editorType: "dxTextArea", isRequired: true, editorOptions: { height: 90, maxLength: 300 } }
      ] }).dxForm("instance"), async f => {
        if (!f.validate().isValid) return false;
        if (await pb.post(url("ActualizarAdjunto", "token=" + d.token), { descripcion: data.descripcion }) === undefined) return false;
        pb.notify("Descripción de " + d.nombre + " actualizada"); cargar();
      }, "Guardar", 480);
    }
    // Lista de documentos elegidos, cada uno con su descripción
    function pintarPendientes() {
      $pend.empty();
      pendientes.forEach(x => {
        const $r = $('<div class="stack" style="gap:4px;margin:6px 0">').appendTo($pend);
        $r.append($("<b>").text(x.f.name + " (" + tamano(x.f.size) + ")"));
        $("<div>").appendTo($r).dxTextBox({ value: x.d, maxLength: 300, placeholder: "Descripción del documento (obligatoria)",
          inputAttr: { "aria-label": "Descripción de " + x.f.name }, valueChangeEvent: "input", onValueChanged: e => { x.d = e.value || ""; } });
      });
    }
    function validar() {
      const sin = pendientes.filter(x => !String(x.d || "").trim());
      if (sin.length) pb.notify("Escriba la descripción de: " + sin.map(x => x.f.name).join(", "), "warning");
      return !sin.length;
    }
    async function quitar(d) {
      if (!await pb.ui.confirmar("Quitar documento", "¿Desea quitar el documento <b>" + pb.esc(d.nombre) + "</b>? Esta acción no se puede deshacer.", "Quitar", "danger", "trash")) return;
      if (await pb.post(url("EliminarAdjunto", "token=" + d.token)) !== undefined) { pb.notify("Documento " + d.nombre + " quitado"); cargar(); }
    }
    // Sube los documentos elegidos; devuelve false si alguno falló (el aviso ya se mostró)
    async function subir(t) {
      token = t;
      if (!validar()) return false;
      let ok = true;
      for (const { f, d } of pendientes) {
        const fd = new FormData(); fd.append("archivo", f, f.name); fd.append("descripcion", d.trim());
        const r = await fetch(url("SubirAdjunto", "ambito=" + o.ambito + "&token=" + t), { method: "POST", headers: { RequestVerificationToken: pb.token() }, body: fd }).then(pb._leer);
        if (r === undefined) ok = false;
      }
      if (pendientes.length && ok) pb.notify(pendientes.length + " documento(s) adjuntado(s)");
      pendientes = []; if (uploader) uploader.reset(); if ($pend) $pend.empty();
      return ok;
    }
    if (o.editable) parametrosAdjunto().then(p => {
      const cfg = p && p.ambitos.find(x => x.ambito === o.ambito);
      if (!cfg) return;
      const exts = cfg.extensiones, max = cfg.maximoMB * 1048576;
      $el.append($('<p class="muted" style="margin:8px 0 0">').text("Tipos permitidos: " + exts.join(", ") + " · hasta " + cfg.maximoMB + " MB cada uno."
        + (o.inmediato ? "" : " Los documentos elegidos se suben al guardar o crear el registro.")));
      uploader = $("<div>").appendTo($el).dxFileUploader({
        multiple: true, uploadMode: "useForm", accept: exts.join(","), allowedFileExtensions: exts, maxFileSize: max,
        selectButtonText: "Elegir documentos", labelText: "o arrástrelos aquí",
        invalidFileExtensionMessage: "Tipo no permitido", invalidMaxFileSizeMessage: "Supera " + cfg.maximoMB + " MB",
        onValueChanged: e => {
          // Conserva la descripción ya escrita de los archivos que siguen elegidos
          pendientes = (e.value || []).filter(f => exts.some(x => f.name.toLowerCase().endsWith(x)) && f.size <= max)
            .map(f => ({ f, d: (pendientes.find(x => x.f === f) || { d: "" }).d }));
          pintarPendientes();
        }
      }).dxFileUploader("instance");
      $pend = $("<div>").appendTo($el);
      if (o.inmediato) pb.ui.mdActions($el, [{ text: "Adjuntar", icon: "upload", onClick: async () => {
        if (!pendientes.length) return pb.notify("Elija uno o varios documentos", "warning");
        await subir(token); cargar();
      } }]);
    });
    cargar();
    return { subir, validar };
  }

  // Tareas de un ítem: cada fila se despliega con los documentos de esa tarea (los adjunta su técnico o quien edita tareas)
  function tareasDelItem($el, ts, titulo) {
    if (titulo) $el.append($('<div class="mdtitle">').text(titulo));
    if (!ts.length) { $el.append($('<p class="muted">').text("Aún no tiene tareas registradas.")); return; }
    $("<div>").appendTo($el).dxDataGrid({
      dataSource: ts, keyExpr: "token", showBorders: true, showRowLines: true, wordWrapEnabled: true, paging: { enabled: false }, columnHidingEnabled: pb.isPhone(),
      columns: [
        { caption: "Tarea", calculateCellValue: x => x.codigo + " · " + x.titulo, minWidth: 200 },
        { caption: "Hito", calculateCellValue: x => x.hito || "—" }, { dataField: "area", caption: "Área" }, { dataField: "tecnico", caption: "Técnico" },
        { caption: "Terminación", calculateCellValue: x => pb.fecha(x.fechaTerminacion) },
        { caption: "Avance", alignment: "right", calculateCellValue: x => x.avance + " %" },
        { caption: "Estado", calculateCellValue: x => ESTADO[x.estado] || x.estado, cellTemplate: ($c, i) => $("<span>").addClass(pb.pillCls(i.value)).text(i.value).appendTo($c) }
      ],
      masterDetail: { enabled: true, template: (el, i) => {
        const $d = $('<div class="md">').appendTo(el);
        $d.append($('<div class="mdtitle">').text("Documentos de " + i.data.codigo));
        adjuntos($("<div>").appendTo($d), { ambito: "Tarea", token: i.data.token, inmediato: true,
          editable: i.data.tecnicoId === pb.perfil.usuarioId || pb.can("tareas", "editar") });
      } }
    });
    $el.append($('<p class="muted">').text("Despliegue una tarea con ›  para ver o adjuntar sus documentos."));
  }

  window.pry = { url, AREAS, ESTADO, iso, fechaLocal, hoy, usuarios, titulo, tareaPopup, acuerdoPopup, detalleTarea, accion, motivoSinTareas, adjuntos, tareasDelItem };
})();
