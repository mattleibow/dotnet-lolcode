export default {
  start() {
    const navMetadata = document.querySelector('meta[name="docfx:navrel"]');
    if (navMetadata) {
      navMetadata.setAttribute("content", "");
    }

    document.documentElement.classList.add("lolcode-docs");

    const relativeRoot =
      document.querySelector('meta[name="docfx:rel"]')?.getAttribute("content") ?? "";
    const docsRoot = new URL(`${relativeRoot}index.html`, window.location.href);
    const docsRootPath = docsRoot.pathname.slice(0, -"index.html".length);
    const documentPath = window.location.pathname.slice(docsRootPath.length);
    const isDocsHome = documentPath === "" || documentPath === "index.html";
    const brandLink = document.querySelector(".navbar-brand");
    const navbarElement = document.querySelector(".navbar");
    const navbarToggle = document.querySelector(
      "button[data-bs-toggle='collapse'][data-bs-target='#navpanel']");

    if (brandLink) {
      brandLink.setAttribute("href", `${relativeRoot}index.html`);
    }
    navbarElement?.classList.replace("navbar-expand-md", "navbar-expand-lg");
    navbarToggle?.classList.replace("d-md-none", "d-lg-none");

    const main = document.querySelector("body > main");
    const tocMetadata = document.querySelector('meta[name="docfx:tocrel"]');
    if (isDocsHome) {
      document.body.classList.add("docs-home");
      tocMetadata?.setAttribute("content", "");
      main?.querySelector(".toc-offcanvas")?.remove();
    } else if (main && tocMetadata?.getAttribute("content") && !document.querySelector("#toc")) {
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
    if (navbar && !navbar.querySelector(".lol-primary-links")) {
      const primary = document.createElement("nav");
      primary.className = "lol-primary-links";
      primary.setAttribute("aria-label", "Documentation sections");

      const sections = [
        ["Learn", "learn/index.html"],
        ["Language & SDK", "language/index.html"],
        ["Build the compiler", "compiler-course/index.html"],
        ["API", "api/Lolcode.CodeAnalysis.html"],
      ];
      const activeSection =
        documentPath === "language/samples.html" ||
        ["learn/", "getting-started/", "tutorials/"].some((prefix) => documentPath.startsWith(prefix))
          ? "Learn"
          : ["language/", "projects/", "reference/"].some((prefix) => documentPath.startsWith(prefix))
            ? "Language & SDK"
            : documentPath.startsWith("compiler-course/")
              ? "Build the compiler"
              : documentPath.startsWith("api/")
                ? "API"
                : null;

      for (const [name, href] of sections) {
        const link = document.createElement("a");
        link.href = `${relativeRoot}${href}`;
        link.textContent = name;
        if (name === activeSection) {
          link.classList.add("active");
          link.setAttribute("aria-current", "page");
        }
        primary.append(link);
      }

      navbar.prepend(primary);
    }

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
