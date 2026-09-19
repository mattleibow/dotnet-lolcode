# Design System

## Product Character

dotnet-lolcode pairs the friendliness and absurdity of LOLCODE with the
confidence of real developer tooling. The visual system should feel like a
well-made compiler workbench that happens to speak in memes: warm, exact,
slightly mischievous, and never novelty-first.

## Visual Foundation

The documentation extends the established browser playground:

- near-black charcoal backgrounds rather than neutral blue-gray;
- warm cream text rather than stark white;
- amber as the primary action, focus, and selection color;
- green, red, and blue reserved for semantic or syntax roles;
- thin warm-gray borders and layered terminal-like surfaces;
- the `:3` mark as the compact brand signature;
- conventional documentation navigation and reading behavior.

The system is dark-first because that is the incumbent product identity. Light
mode remains fully supported and translates the same warm palette to paper-like
surfaces instead of becoming a generic white documentation theme.

## Typography

Use one highly legible sans-serif family for interface and prose, with a
monospace family for code, syntax labels, compact metadata, and small LOLCODE
accents. Headings should be sturdy and editorial rather than cartoonish.

Long-form content should stay near 70 characters per line. Reference tables may
use wider measures where comparison benefits from density.

## Layout

The site uses familiar DocFX structure: global header, collapsible section
navigation, article content, table of contents, search, breadcrumbs, and
previous/next navigation. Custom styling must improve hierarchy and character
without changing those learned affordances.

The landing page may use a broader editorial grid, but every interior page
prioritizes reading flow. On small screens, navigation collapses cleanly and
content becomes a single column without horizontal page scrolling.

## Components

- **Brand mark:** a compact amber `:3` tile paired with `dotnet-lolcode`.
- **Primary action:** solid amber, dark text, modest radius, clear focus ring.
- **Secondary action:** bordered or quiet surface treatment.
- **Code sample:** terminal-like framed surface with filename or concept label,
  copy affordance, and syntax highlighting.
- **Callout:** semantic border and icon treatment; meaning never depends only on
  color.
- **Learning card:** concise outcome, difficulty or time metadata when factual,
  and a clear next action.
- **Reference table:** dense but readable, with sticky or emphasized headers
  where the template permits.
- **Status treatment:** green for supported, amber for partial or proposed, red
  for unsupported or hazardous behavior.

## Motion

Motion is limited to 150–250 ms state transitions for navigation, search,
hover, copy confirmation, and expandable content. No decorative page-load
sequence. Respect `prefers-reduced-motion`.

## Voice

Lead with clear technical English. Use LOLCODE phrasing for memorable headings,
examples, and small moments of delight, not for instructions whose meaning
could become ambiguous. Never trade correctness for a joke.

## Accessibility

Maintain WCAG AA contrast, visible keyboard focus, semantic landmarks, usable
skip links, appropriately labeled controls, reduced-motion behavior, and code
highlighting with non-color cues where possible.
