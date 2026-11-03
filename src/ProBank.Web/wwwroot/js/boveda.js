// Bóveda: piezas comunes de Cuentas técnicas y Secretos (como credPanel, revealPopup y rotarPopup del prototipo).
// El valor nunca viene en los listados: se pide al mostrar o copiar (queda en la auditoría) y no se guarda en la página.
(function () {
  "use strict";
  const url = (a, q) => "/Boveda/Datos/" + a + (q ? "?" + q : "");
  const IDENTIDAD = "Identidad administrada de Azure";
  const VIGENCIAS = [30, 60, 90, 180, 365];
  const dias = n => n == null ? "—" : n < 0 ? "Vencido hace " + (-n) + (n === -1 ? " día" : " días") : n === 0 ? "Vence hoy" : n + (n === 1 ? " día" : " días");
  const grupo = r => r.grupoCodigo ? r.grupoCodigo + " · " + r.grupoNombre : "—";

  // Generador local (crypto del navegador): el valor no sale de la página hasta guardarlo
  function generar(n) {
    const c = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!#$%*+-_";
    const a = new Uint32Array(n || 24); window.crypto.getRandomValues(a);
    return Array.from(a, x => c[x % c.length]).join("");
  }

  const cfg = clase => clase === "cuentas"
    ? { opcion: "cuentas", etiqueta: "Contraseña", cambiar: "Cambiar contraseña", dias: "Próxima rotación en (días)", defecto: 90, donde: "Entra ID, el AD on-premise o la base de datos" }
    : { opcion: "secretos", etiqueta: "Valor del secreto", cambiar: "Cambiar valor", dias: "Vigencia (días)", defecto: 180, donde: "el almacén y el sistema que lo usa" };

  // Panel de la credencial en el detalle desplegado y en la ficha. alCambiar: refresca la lista o la ficha.
  async function panel($el, clase, r, alCambiar) {
    const c = cfg(clase);
    if (clase === "cuentas" && r.tipo === IDENTIDAD) { pb.ui.mdNote($el, "Identidad administrada de Azure: no tiene contraseña que guardar ni rotar."); return; }
    const $box = $('<div class="cred">').appendTo($el);
    $box.append($('<div class="cred-l">').append($("<b>").text(c.etiqueta)).append($('<span class="cred-v">').text(r.tieneValor ? "•••••••••••••••••" : "sin valor guardado")));
    $box.append($('<div class="cred-m">').text("Cifrada con Always Encrypted (SQL Server) · " +
      (r.tieneValor ? "versión " + r.version + " · actualizada " + pb.fechaHora(r.fechaVersion) : "aún sin versión") + " · grupo " + grupo(r)));
    $box.append($('<span class="pill pill-b">').text("Always Encrypted"));
    if (!r.esMiembro) {
      $box.append($('<span class="pill pill-r">').text("Sin acceso"));
      pb.ui.mdNote($el, "Solo los miembros del grupo " + (r.grupoCodigo || "de acceso") + " pueden ver, copiar o cambiar esta credencial. Solicite el acceso al administrador del grupo.");
    }
    const acc = [
      { text: "Mostrar", icon: "find", type: "default", disabled: !r.esMiembro || !r.tieneValor || !pb.can(c.opcion, "mostrar"), onClick: () => mostrar(clase, r, alCambiar) },
      { text: c.cambiar, icon: "key", disabled: !r.esMiembro || !r.activo || !pb.can(c.opcion, "editar"), onClick: () => cambiar(clase, r, alCambiar) }
    ];
    pb.ui.mdActions($el, acc);
    const h = await pb.get(url("Historial", "clase=" + clase + "&token=" + r.token));
    if (!h) return;
    pb.ui.mdGrid($el, "Accesos a la credencial (registrados en Auditoría)", [["f", "Fecha y hora"], ["u", "Usuario"], ["a", "Acción"], ["v", "Versión"], ["ip", "IP"]],
      h.accesos.map(a => ({ f: pb.fechaHora(a.fecha), u: a.nombre + " (" + a.usuario + ")", a: a.accion, v: "v" + a.version, ip: a.ip || "—" })), "Nadie ha consultado esta credencial.");
    return h;
  }

  // CRE-02: cada entrega (mostrar o copiar) la registra la base antes de devolver el valor; se oculta sola a los 30 s.
  async function mostrar(clase, r, alCambiar) {
    const c = cfg(clase);
    const v = await pb.post(url("Mostrar", "clase=" + clase + "&token=" + r.token));
    if (v === undefined) return;
    let timer = null, valor = v.valor;
    const copiar = pb.can(c.opcion, "copiar") ? [{ text: "Copiar", icon: "copy", type: "default", onClick: async () => {
      const x = await pb.post(url("Copiar", "clase=" + clase + "&token=" + r.token));
      if (x === undefined) return;
      try { await navigator.clipboard.writeText(x.valor); pb.notify("Copiado al portapapeles (registrado en Auditoría); límpielo después de usarlo", "info"); }
      catch (e) { pb.notify("No se pudo copiar; seleccione el texto", "warning"); }
    } }] : [];
    const p = pb.ui.openPopup("Mostrar credencial · " + r.nombre, $f => {
      $f.append($('<div class="cred-show">').text(valor));
      const $cd = $('<p class="muted" style="margin:6px 0 0">').appendTo($f);
      let s = 30;
      const tick = () => {
        if (!$f.closest("body").length) { clearInterval(timer); return; }
        $cd.text("Se ocultará en " + s + " s. Consulta registrada en Auditoría (versión " + v.version + "). No la comparta por correo ni chat.");
        if (s-- <= 0) { clearInterval(timer); p && p.hide(); }
      };
      tick(); timer = setInterval(tick, 1000);
      return null;
    }, () => true, "Cerrar", 540, { extra: copiar });
    p.on("hidden", () => { clearInterval(timer); valor = null; if (alCambiar) alCambiar(); });
  }

  function cambiar(clase, r, alCambiar) {
    const c = cfg(clase);
    if (!r.esMiembro) return pb.notify("No pertenece al grupo " + (r.grupoCodigo || "") + ": no puede cambiar esta credencial", "warning");
    if (!r.activo) return pb.notify("La credencial está inactiva: actívela antes de cambiar su valor", "warning");
    const data = { valor: "", confirma: "", dias: c.defecto, nota: "" };
    let form = null;
    pb.ui.openPopup(c.cambiar + " · " + r.nombre, $f => (form = $("<div>").appendTo($f).dxForm({ formData: data, labelLocation: "top", showColonAfterLabel: false, items: [
      { template: () => $('<p class="muted" style="margin:0">').text("El driver de SQL Server cifra el valor con Always Encrypted antes de enviarlo a la base; solo lo pueden ver los miembros del grupo " +
          (r.grupoCodigo || "") + ". La versión anterior queda en el historial sin su valor. Recuerde aplicarlo también en " + c.donde + ".") },
      { dataField: "valor", label: { text: clase === "cuentas" ? "Nueva contraseña" : "Nuevo valor" }, isRequired: true,
        editorOptions: { mode: "password", maxLength: 4000, inputAttr: { autocomplete: "new-password" }, buttons: [
          { name: "ver", location: "after", options: { icon: "eyeopen", hint: "Ver u ocultar", stylingMode: "text", onClick: () => {
            const e = form.getEditor("valor"); e.option("mode", e.option("mode") === "text" ? "password" : "text"); } } },
          { name: "gen", location: "after", options: { icon: "refresh", hint: "Generar", stylingMode: "text", onClick: () => { const g = generar(24); form.updateData({ valor: g, confirma: g }); } } }] },
        validationRules: [{ type: "stringLength", min: 16, message: "Use al menos 16 caracteres" }] },
      { dataField: "confirma", label: { text: "Confirmar" }, isRequired: true, editorOptions: { mode: "password", maxLength: 4000, inputAttr: { autocomplete: "new-password" } },
        validationRules: [{ type: "compare", comparisonTarget: () => data.valor, message: "Los valores no coinciden" }] },
      { dataField: "dias", label: { text: c.dias }, editorType: "dxSelectBox", editorOptions: { items: VIGENCIAS } },
      { dataField: "nota", label: { text: "Nota (opcional)" }, editorOptions: { maxLength: 200, placeholder: "Por ejemplo: rotación programada" } }
    ] }).dxForm("instance")), async f => {
      if (!f.validate().isValid) return false;
      const x = await pb.post(url("Cambiar", "clase=" + clase + "&token=" + r.token), { valor: data.valor, dias: data.dias, nota: data.nota || null });
      data.valor = data.confirma = "";
      if (x === undefined) return false;
      pb.notify((clase === "cuentas" ? "Contraseña de " + r.nombre + " cifrada y guardada" : "Valor de " + r.nombre + " cifrado y guardado") + " · versión " + x.version + " · " +
        (clase === "cuentas" ? "próxima rotación " : "expira ") + pb.fecha(x.vence));
      if (alCambiar) alCambiar();
    }, "Guardar cifrado", 520, { icono: "key" });
  }

  window.boveda = { url, IDENTIDAD, dias, grupo, panel, mostrar, cambiar };
})();
