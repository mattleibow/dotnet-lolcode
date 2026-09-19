document.documentElement.classList.add("lolcode-docs");

const relativeRoot =
  document.querySelector('meta[name="docfx:rel"]')?.getAttribute("content") ?? "";
const brandLink = document.querySelector(".navbar-brand");

if (brandLink) {
  brandLink.setAttribute("href", `${relativeRoot}index.html`);
}
