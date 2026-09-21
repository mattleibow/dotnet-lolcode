export default {
  start() {
    const navMetadata = document.querySelector('meta[name="docfx:navrel"]');
    if (navMetadata) {
      navMetadata.setAttribute("content", "");
    }

    document.documentElement.classList.add("lolcode-docs");

    const relativeRoot =
      document.querySelector('meta[name="docfx:rel"]')?.getAttribute("content") ?? "";
    const brandLink = document.querySelector(".navbar-brand");

    if (brandLink) {
      brandLink.setAttribute("href", `${relativeRoot}index.html`);
    }

    const main = document.querySelector("body > main");
    const tocMetadata = document.querySelector('meta[name="docfx:tocrel"]');
    if (main && tocMetadata?.getAttribute("content") && !document.querySelector("#toc")) {
      const sidebar = document.createElement("aside");
      sidebar.id = "docs-sidebar";
      sidebar.className = "toc-offcanvas offcanvas-md offcanvas-start";
      sidebar.tabIndex = -1;
      sidebar.setAttribute("aria-label", "Documentation navigation");
      sidebar.innerHTML = `
        <div class="offcanvas-header d-md-none">
          <h2 class="offcanvas-title h5">Documentation</h2>
          <button class="btn-close" type="button" data-bs-dismiss="offcanvas"
            data-bs-target="#docs-sidebar" aria-label="Close documentation navigation"></button>
        </div>
        <nav id="toc" aria-label="Documentation"></nav>`;
      main.prepend(sidebar);

      const actionbar = main.querySelector(".actionbar");
      if (actionbar) {
        const toggle = document.createElement("button");
        toggle.className = "lol-toc-toggle btn d-md-none";
        toggle.type = "button";
        toggle.setAttribute("data-bs-toggle", "offcanvas");
        toggle.setAttribute("data-bs-target", "#docs-sidebar");
        toggle.setAttribute("aria-controls", "docs-sidebar");
        toggle.innerHTML = '<i class="bi bi-list" aria-hidden="true"></i> Browse documentation';
        actionbar.prepend(toggle);
      }
    }

    const navbar = document.querySelector("#navbar");
    if (navbar && !navbar.querySelector(".lol-utility-links")) {
      const utilities = document.createElement("nav");
      utilities.className = "lol-utility-links";
      utilities.setAttribute("aria-label", "Documentation utilities");

      const playground = document.createElement("a");
      playground.href = `${relativeRoot}../playground/`;
      playground.textContent = "Playground";

      const github = document.createElement("a");
      github.href = "https://github.com/mattleibow/dotnet-lolcode";
      github.textContent = "GitHub";

      utilities.append(playground, github);
      navbar.append(utilities);
    }
  },
};
