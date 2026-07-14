/* GHOST project page — behavior
   - Swaps missing images/videos for labeled placeholders (auto-recovers when
     you drop the real file into assets/).
   - Copy-to-clipboard for BibTeX.
   - Active-section highlighting in the nav.
   - Disables the Paper button until assets/pdf/paper.pdf exists.
*/
(function () {
  "use strict";

  /* ---------- Missing-media placeholders ---------- */
  function markMissing(el) {
    var media = el.closest(".media");
    if (!media || media.classList.contains("is-missing")) return;
    var label = el.getAttribute("data-fallback") || el.getAttribute("alt") || "Media";
    var hint = el.getAttribute("data-src-hint");
    media.classList.add("is-missing");
    media.setAttribute("data-ph", hint ? label + "\n" + hint : label);
  }

  var figs = document.querySelectorAll(".fig");
  figs.forEach(function (el) {
    if (el.tagName === "IMG") {
      if (el.complete && el.naturalWidth === 0) markMissing(el);
      el.addEventListener("error", function () { markMissing(el); });
    } else if (el.tagName === "VIDEO") {
      // A <video> with an unresolved <source> fires 'error' on the source.
      var src = el.querySelector("source");
      if (src) src.addEventListener("error", function () { markMissing(el); });
      el.addEventListener("error", function () { markMissing(el); });
      // Fallback: if no dimensions become available shortly, treat as missing.
      setTimeout(function () {
        if (el.readyState === 0 && !el.videoWidth) {
          // only mark missing if the poster is the placeholder svg (i.e. no real video)
          if ((el.currentSrc || "").indexOf(".mp4") === -1) markMissing(el);
        }
      }, 1500);
    }
  });

  /* ---------- Copy BibTeX ---------- */
  var copyBtn = document.querySelector("[data-copy]");
  if (copyBtn) {
    copyBtn.addEventListener("click", function () {
      var src = document.getElementById("bibtex-src");
      if (!src) return;
      var text = src.textContent;
      var done = function () {
        copyBtn.textContent = "Copied ✓";
        copyBtn.classList.add("copied");
        setTimeout(function () {
          copyBtn.textContent = "Copy";
          copyBtn.classList.remove("copied");
        }, 1800);
      };
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(done).catch(fallbackCopy);
      } else {
        fallbackCopy();
      }
      function fallbackCopy() {
        var ta = document.createElement("textarea");
        ta.value = text; document.body.appendChild(ta); ta.select();
        try { document.execCommand("copy"); done(); } catch (e) {}
        document.body.removeChild(ta);
      }
    });
  }

  /* ---------- Active section highlighting ---------- */
  var navLinks = Array.prototype.slice.call(document.querySelectorAll(".nav__links a"));
  var idToLink = {};
  navLinks.forEach(function (a) {
    var id = a.getAttribute("href").slice(1);
    if (id) idToLink[id] = a;
  });
  var sections = Object.keys(idToLink)
    .map(function (id) { return document.getElementById(id); })
    .filter(Boolean);

  if ("IntersectionObserver" in window && sections.length) {
    var current = null;
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) {
          if (current) current.classList.remove("is-active");
          current = idToLink[e.target.id];
          if (current) current.classList.add("is-active");
        }
      });
    }, { rootMargin: "-45% 0px -50% 0px", threshold: 0 });
    sections.forEach(function (s) { io.observe(s); });
  }

  /* ---------- Disable Paper button until the PDF exists ---------- */
  var pdfBtn = document.querySelector("[data-optional-pdf]");
  if (pdfBtn && location.protocol.indexOf("http") === 0) {
    fetch(pdfBtn.getAttribute("href"), { method: "HEAD" })
      .then(function (r) {
        if (!r.ok) disablePdf();
      })
      .catch(disablePdf);
  }
  function disablePdf() {
    if (!pdfBtn) return;
    var span = document.createElement("span");
    span.className = "btn btn--disabled";
    span.title = "Paper available after review";
    span.setAttribute("aria-disabled", "true");
    span.innerHTML = '<span class="btn__icon" aria-hidden="true">📄</span> Paper <em>· soon</em>';
    pdfBtn.replaceWith(span);
  }
})();
