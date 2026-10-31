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
    let form, editor, hitos = [];
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
      if (data.item) setTimeout(cargarItem, 0);
      return form;
    }, async f => {
      if (!f.validate().isValid) return false;
      const r = await pb.post(url("CrearTarea"), { itemToken: data.item, hitoId: data.hitoId, titulo: data.titulo, area: data.area, tecnicoId: data.tecnicoId, orden: data.orden,
        fechaInicio: iso(data.inicio), fechaTerminacion: iso(data.fecha), aprobadorIds: data.aprob, descripcionHtml: editor.option("value") || null });
      if (r === undefined) return false;
      const tec = users.find(u => u.usuarioId === data.tecnicoId), it = abiertos.find(i => i.token === data.item);
      const nom = id => (users.find(u => u.usuarioId === id) || {}).nombre;
      pb.notify("Tarea " + r + " creada en " + it.codigo + " y asignada a " + tec.nombre + (data.aprob.length > 1 ? " · requiere la aprobación de sus " + data.aprob.length + " aprobadores" : data.aprob.length ? " · la aprueba " + nom(data.aprob[0]) : " · sin aprobadores"));
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
    // El técnico asignado envía y completa su tarea; los demás necesitan poder editar tareas (lo valida la API)
    const puede = t.tecnicoId === pb.perfil.usuarioId || pb.can("tareas", "editar");
    if (!listo || !puede) return;
    pb.ui.mdActions($e, [
      { text: "Enviar a aprobar", icon: "send", type: "default", disabled: t.estado === "EnRevision" || t.estado === "Completada", onClick: () => accion(t, "enviar", listo) },
      { text: "Marcar completada", icon: "check", disabled: t.estado !== "EnCurso", onClick: () => accion(t, "completar", listo) }
    ]);
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

  window.pry = { url, AREAS, ESTADO, iso, fechaLocal, hoy, usuarios, titulo, tareaPopup, acuerdoPopup, detalleTarea, accion };
})();
