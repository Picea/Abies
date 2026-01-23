# Abies Conference Brand Palette (Fluent-inspired) — Web Components Edition

This document defines a conference-grade brand palette for Abies, optimized for Fluent-style visuals and implementation in Web Components (CSS custom properties + design tokens).

---

## 1. Goals (Conference variant)

Conference branding differs from product UI:
- Higher saturation and contrast for stage/screens
- Strong hero gradients for covers and banners
- Predictable accessibility on light/dark backgrounds
- Fluent-adjacent look: clean neutrals + crisp accent color behavior

Brand personality:
- Reactive systems / runtime tooling
- Optimistic green energy
- Modern developer ergonomics

---

## 2. Abies Logo-derived Core Color Ramp

Primary taken from the Abies logo:
- Abies Green 500: #89B42B

Expanded ramp for consistent usage across states:
| Token                | Hex      | Suggested use |
|---------------------|----------|---------------|
| --abies-brand-50     | #F4F8EA  | subtle tints / background wash |
| --abies-brand-100    | #E7F1CC  | soft panels, hover backgrounds |
| --abies-brand-200    | #D0E39B  | secondary highlight surfaces |
| --abies-brand-300    | #B7D56A  | soft accent, chips |
| --abies-brand-400    | #9FC744  | highlight, emphasis |
| --abies-brand-500    | #89B42B  | primary CTA / links / active |
| --abies-brand-600    | #71922D  | hover (primary) |
| --abies-brand-700    | #6D9026  | pressed/active, strong headings |
| --abies-brand-800    | #4C641A  | deep accents |
| --abies-brand-900    | #2E3D10  | hero depth / dark tint |

---

## 3. Fluent-style Neutrals (Foundation)

Fluent visuals rely on high-quality neutrals. Use these for layout + readability:
| Token                  | Hex      | Use |
|------------------------|----------|-----|
| --abies-neutral-0       | #FFFFFF  | base |
| --abies-neutral-50      | #FAFAFA  | elevated surfaces |
| --abies-neutral-100     | #F2F4F7  | light panels |
| --abies-neutral-200     | #E4E7EC  | borders |
| --abies-neutral-300     | #D0D5DD  | strong borders |
| --abies-neutral-400     | #98A2B3  | subtle text/icons |
| --abies-neutral-500     | #667085  | muted text |
| --abies-neutral-600     | #475467  | secondary text |
| --abies-neutral-700     | #344054  | strong body text |
| --abies-neutral-800     | #1D2939  | headings on light |
| --abies-neutral-900     | #101828  | primary text |

Recommended deck backgrounds:
- Light deck bg:  #F8FAFC
- Dark deck bg:   #0B1220

---

## 4. Conference Accent Colors (Fluent-friendly)

Secondary accents used for emphasis and contrast next to green.

### Accent A — Azure (Fluent-compatible)
| Token                     | Hex      | Use |
|---------------------------|----------|-----|
| --abies-accent-azure-500  | #2E90FA  | links, info callouts |
| --abies-accent-azure-600  | #1570EF  | hover/active |

### Accent B — Amber (Signal)
| Token                     | Hex      | Use |
|---------------------------|----------|-----|
| --abies-accent-amber-500  | #F79009  | schedule emphasis, CTA highlight |
| --abies-accent-amber-600  | #DC6803  | hover/active |

### Accent C — Magenta (Optional)
Use sparingly, mostly for social/marketing cards.
| Token                        | Hex      |
|------------------------------|----------|
| --abies-accent-magenta-500   | #D444F1  |

---

## 5. Semantic colors (Meaning-driven)

These should not drift with branding:
| Token                  | Hex      |
|------------------------|----------|
| --abies-success-500     | #12B76A  |
| --abies-warning-500     | #F79009  |
| --abies-danger-500      | #F04438  |
| --abies-info-500        | #2E90FA  |

---

## 6. Hero Gradients (Conference assets)

### Hero Gradient 1 — Abies Hero
For banners, title slides:
linear-gradient(135deg,
  #2E3D10 0%,
  #6D9026 25%,
  #89B42B 55%,
  #B7D56A 100%)

### Hero Gradient 2 — Fluent Tech
For dev tooling vibe:
linear-gradient(135deg,
  #0B1220 0%,
  #1D2939 25%,
  #1570EF 55%,
  #89B42B 100%)

### Background Wash — Mica Soft
For subtle panels:
radial-gradient(circle at 20% 20%,
  rgba(137,180,43,0.18) 0%,
  rgba(46,144,250,0.10) 35%,
  rgba(0,0,0,0.00) 70%)

---

## 7. Web Components Token Contract (CSS Custom Properties)

### 7.1 Global tokens (put on :root or theme host)
:root {
  /* Brand ramp */
  --abies-brand-50: #F4F8EA;
  --abies-brand-100: #E7F1CC;
  --abies-brand-200: #D0E39B;
  --abies-brand-300: #B7D56A;
  --abies-brand-400: #9FC744;
  --abies-brand-500: #89B42B;
  --abies-brand-600: #71922D;
  --abies-brand-700: #6D9026;
  --abies-brand-800: #4C641A;
  --abies-brand-900: #2E3D10;

  /* Neutrals */
  --abies-neutral-0: #FFFFFF;
  --abies-neutral-50: #FAFAFA;
  --abies-neutral-100: #F2F4F7;
  --abies-neutral-200: #E4E7EC;
  --abies-neutral-300: #D0D5DD;
  --abies-neutral-400: #98A2B3;
  --abies-neutral-500: #667085;
  --abies-neutral-600: #475467;
  --abies-neutral-700: #344054;
  --abies-neutral-800: #1D2939;
  --abies-neutral-900: #101828;

  /* Accents */
  --abies-accent-azure-500: #2E90FA;
  --abies-accent-azure-600: #1570EF;

  --abies-accent-amber-500: #F79009;
  --abies-accent-amber-600: #DC6803;

  --abies-accent-magenta-500: #D444F1;

  /* Semantic */
  --abies-success-500: #12B76A;
  --abies-warning-500: #F79009;
  --abies-danger-500: #F04438;
  --abies-info-500: #2E90FA;

  /* Component-level derived tokens */
  --abies-focus-ring: rgba(137,180,43,0.42);
  --abies-link: var(--abies-accent-azure-600);

  /* Typography (recommended) */
  --abies-font-ui: ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, Helvetica, Arial;
  --abies-font-mono: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
}

### 7.2 Light theme mapping
:root[data-theme="light"]{
  --abies-bg: var(--abies-neutral-0);
  --abies-bg-elevated: #F8FAFC;
  --abies-bg-soft: var(--abies-neutral-100);

  --abies-text: var(--abies-neutral-900);
  --abies-text-muted: var(--abies-neutral-600);
  --abies-text-subtle: var(--abies-neutral-500);

  --abies-border: var(--abies-neutral-200);
  --abies-border-strong: var(--abies-neutral-300);

  --abies-accent: var(--abies-brand-500);
  --abies-accent-hover: var(--abies-brand-600);
  --abies-accent-active: var(--abies-brand-700);
}

### 7.3 Dark theme mapping
:root[data-theme="dark"]{
  --abies-bg: #0B1220;
  --abies-bg-elevated: #101828;
  --abies-bg-soft: #1D2939;

  --abies-text: #F2F4F7;
  --abies-text-muted: rgba(242,244,247,0.86);
  --abies-text-subtle: rgba(242,244,247,0.64);

  --abies-border: rgba(242,244,247,0.10);
  --abies-border-strong: rgba(242,244,247,0.18);

  --abies-accent: var(--abies-brand-500);
  --abies-accent-hover: var(--abies-brand-300);
  --abies-accent-active: var(--abies-brand-600);

  --abies-focus-ring: rgba(183,213,106,0.42);
}

---

## 8. Usage rules (Conference discipline)

### Ratio guideline (visual balance)
- 70% neutrals
- 20% Abies greens
- 10% accents (azure/amber/magenta)

### Accessibility
- Don’t use green for body text.
- Use green for: CTA, active nav, key highlights, focus ring.
- Ensure text on --abies-accent uses very dark ink (#0B1220) or white depending on contrast test.

### Recommended pairing
- Background: Dark deck bg (#0B1220)
- Text: #F2F4F7
- Primary CTA: --abies-brand-500
- Secondary highlight: --abies-accent-azure-500

---

## 9. Quick UI examples (for WC components)

### Primary button
background: var(--abies-accent)
color: #0B1220
hover: background var(--abies-accent-hover)
active: background var(--abies-accent-active)

### Link
color: var(--abies-link)
hover: #2E90FA

### Focus ring
outline: 2px solid var(--abies-focus-ring)
outline-offset: 2px

---

## 10. Copy/paste hero CSS snippets

.abies-hero{
  background: linear-gradient(135deg,
    #2E3D10 0%,
    #6D9026 25%,
    #89B42B 55%,
    #B7D56A 100%);
}

.abies-hero--fluent{
  background: linear-gradient(135deg,
    #0B1220 0%,
    #1D2939 25%,
    #1570EF 55%,
    #89B42B 100%);
}

.abies-mica{
  background:
    radial-gradient(circle at 20% 20%,
      rgba(137,180,43,0.18) 0%,
      rgba(46,144,250,0.10) 35%,
      rgba(0,0,0,0.00) 70%);
}