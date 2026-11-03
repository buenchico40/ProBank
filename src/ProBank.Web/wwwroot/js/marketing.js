// Marketing: piezas comunes del Panel y de Marketing › Imágenes promocionales y Comunicados (como en el prototipo):
// diapositiva del carrusel, lectura de un comunicado (resumen, secciones y contacto) y saneado del contenido enriquecido.
(function () {
  "use strict";
  const url = (a, q) => "/Marketing/Datos/" + a + (q ? "?" + q : "");
  // Tipo de comunicado: icono y color de acento
  const COMTYPE = { "Mantenimiento": { k: "mant", i: "preferences" }, "Aviso": { k: "aviso", i: "info" }, "Noticia": { k: "noti", i: "message" }, "Campaña": { k: "camp", i: "favorites" } };
  const UBICACIONES = ["Panel principal", "Inicio de sesión"];
  const TIPOS = Object.keys(COMTYPE);

  // HTML del editor sin scripts, estilos incrustados, iframes ni atributos on* / javascript:
  function sanear(html) {
    const doc = new DOMParser().parseFromString(html || "", "text/html");
    doc.querySelectorAll("script,style,iframe,object,embed,link,meta,form").forEach(n => n.remove());
    doc.querySelectorAll("*").forEach(n => [...n.attributes].forEach(a => { if (/^on/i.test(a.name) || (/^(href|src)$/i.test(a.name) && /^\s*javascript:/i.test(a.value))) n.removeAttribute(a.name); }));
    return doc.body.innerHTML;
  }
  const vigencia = r => r.desde ? "Del " + pb.fecha(r.desde) + (r.hasta ? " al " + pb.fecha(r.hasta) : "") : "Sin vigencia definida";

  const imagen = p => url("Imagen", "token=" + p.token);
  const sinImagen = p => $('<div class="promo-slide">').append($("<small>").text("Campaña" + (p.hasta ? " · vigente hasta el " + pb.fecha(p.hasta) : "")))
    .append($("<strong>").text(p.titulo)).append($("<span>").text("Imagen administrada por Marketing"));

  // Vista previa de la ficha: la imagen subida (con su enlace) o, sin imagen, el diseño de campaña del prototipo
  function slide(p) {
    if (!p.tieneImagen) return sinImagen(p);
    const $img = $('<img class="promo-img">').attr({ src: imagen(p), alt: p.textoAlternativo || p.titulo });
    return p.enlace ? $('<a target="_blank" rel="noopener noreferrer">').attr("href", p.enlace).append($img) : $img;
  }

  // Diapositiva del carrusel del Panel: la imagen completa (sin recortar) sobre un fondo desenfocado de ella misma,
  // con el título, la vigencia y el enlace encima
  function hero(p) {
    const $s = $('<div class="hero-slide">');
    if (!p.tieneImagen) return $s.append(sinImagen(p));
    $s.append($('<div class="hero-fondo" aria-hidden="true">').css("background-image", "url('" + imagen(p) + "')"));
    $s.append($('<img class="hero-img">').attr({ src: imagen(p), alt: p.textoAlternativo || p.titulo }));
    // Franja propia debajo de la imagen: título y, en una línea, vigencia y enlace
    const $l = $('<div class="hero-linea">').append($("<span>").text(p.hasta ? "Vigente hasta el " + pb.fecha(p.hasta) : "Campaña vigente"));
    $('<div class="hero-texto">').appendTo($s).append($("<strong>").text(p.titulo)).append($l);
    if (p.enlace) $l.append($('<a target="_blank" rel="noopener noreferrer">').attr("href", p.enlace).text("Conocer más ›"));
    return $s;
  }

  // Lectura del comunicado (comPopup del prototipo): resumen destacado, datos, secciones y contacto
  function leer(r) {
    const ty = COMTYPE[r.tipo] || COMTYPE.Aviso;
    pb.ui.openPopup(r.titulo, $f => {
      const $w = $('<div class="com-read">').addClass("t-" + ty.k).appendTo($f);
      $w.append($('<div class="com-top">').append($('<span class="com-ico dx-icon dx-icon-' + ty.i + '">')).append($('<span class="com-type">').text(r.tipo))
        .append(r.destacado ? $('<span class="com-flag">').text("Destacado") : null));
      $w.append($('<p class="com-lead">').text(r.resumen));
      const $m = $('<dl class="com-facts">').appendTo($w);
      [["Vigencia", vigencia(r)], ["Dirigido a", r.audiencia === "Todos" ? "Todo el personal" : r.audiencia], ["Publicado por", r.publicadoPor || "Marketing"]]
        .forEach(x => $m.append($("<dt>").text(x[0])).append($("<dd>").text(x[1])));
      if (r.contenidoHtml) $('<section class="com-sec">').appendTo($w).html(sanear(r.contenidoHtml));
      if (r.contacto) $w.append($('<p class="com-contact">').text("Consultas: " + r.contacto));
      return null;
    }, () => true, "Cerrar", 640);
  }

  window.mkt = { url, COMTYPE, UBICACIONES, TIPOS, sanear, vigencia, slide, hero, leer };
})();
