---
name: Professional ERP Logic
colors:
  surface: '#faf8ff'
  surface-dim: '#d9d9e5'
  surface-bright: '#faf8ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f3fe'
  surface-container: '#ededf9'
  surface-container-high: '#e7e7f3'
  surface-container-highest: '#e1e2ed'
  on-surface: '#191b23'
  on-surface-variant: '#434655'
  inverse-surface: '#2e3039'
  inverse-on-surface: '#f0f0fb'
  outline: '#737686'
  outline-variant: '#c3c6d7'
  surface-tint: '#0053db'
  primary: '#004ac6'
  on-primary: '#ffffff'
  primary-container: '#2563eb'
  on-primary-container: '#eeefff'
  inverse-primary: '#b4c5ff'
  secondary: '#505f76'
  on-secondary: '#ffffff'
  secondary-container: '#d0e1fb'
  on-secondary-container: '#54647a'
  tertiary: '#943700'
  on-tertiary: '#ffffff'
  tertiary-container: '#bc4800'
  on-tertiary-container: '#ffede6'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dbe1ff'
  primary-fixed-dim: '#b4c5ff'
  on-primary-fixed: '#00174b'
  on-primary-fixed-variant: '#003ea8'
  secondary-fixed: '#d3e4fe'
  secondary-fixed-dim: '#b7c8e1'
  on-secondary-fixed: '#0b1c30'
  on-secondary-fixed-variant: '#38485d'
  tertiary-fixed: '#ffdbcd'
  tertiary-fixed-dim: '#ffb596'
  on-tertiary-fixed: '#360f00'
  on-tertiary-fixed-variant: '#7d2d00'
  background: '#faf8ff'
  on-background: '#191b23'
  surface-variant: '#e1e2ed'
typography:
  headline-lg:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.05em
  data-mono:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
    letterSpacing: -0.01em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  container-padding: 16px
  gutter: 12px
  stack-sm: 4px
  stack-md: 12px
  stack-lg: 24px
  tap-target: 48px
---

## Brand & Style

The design system is engineered for high-utility business environments, prioritizing speed of recognition and operational efficiency. It bridges the gap between the robust legacy of enterprise resource planning and the fluid expectations of modern mobile applications. 

The aesthetic is **Corporate / Modern**—a disciplined approach that utilizes a utilitarian layout to manage complex data density. It avoids decorative elements in favor of functional clarity. The goal is to evoke a sense of reliability and precision, ensuring that warehouse managers and sales representatives can navigate inventory, orders, and status updates with zero cognitive friction. It is a tool, not a toy; every shadow and border exists to define hierarchy and interaction zones.

## Colors

The palette is anchored in a high-contrast foundation to ensure legibility under various lighting conditions, common in mobile work environments. 

- **Primary Blue:** Used exclusively for primary actions and active states, providing a clear "path of intent."
- **Surfaces:** A neutral gray (#F3F4F6) differentiates background containers from the white base, creating a clear "card-on-canvas" architecture.
- **Semantic Logic:** Status colors are high-chroma to ensure immediate recognition of inventory levels:
    - **Success (Green):** Indicates 'Na stanie' (In stock).
    - **Warning (Orange):** Indicates 'Niski stan' (Low stock).
    - **Error (Red):** Indicates 'Brak' (Out of stock).
- **Neutrals:** A scale of Slates is used for secondary text and borders to maintain a professional, de-saturated environment that doesn't distract from critical data.

## Typography

The typography system utilizes **Inter** for its exceptional legibility and neutral, systematic character. It is designed to handle dense alphanumeric strings (SKUs, quantities, prices) without losing clarity.

- **Scale:** Headlines are kept modest in size to maximize vertical screen real estate on mobile devices.
- **Hierarchy:** We use font weight (Semibold 600) rather than massive size increases to denote importance. 
- **Data Presentation:** For inventory counts and numerical values, the `data-mono` style (using Inter's tabular features) ensures that numbers align vertically in lists, aiding quick scanning.
- **Labels:** Micro-copy and secondary metadata use an uppercase label style to distinguish "Title" from "Value" in key-value pairs.

## Layout & Spacing

This design system follows a **strict 8px grid** to ensure consistency across all mobile viewports.

- **Mobile-First Grid:** A 4-column layout is the standard, with 16px outer margins and 12px gutters.
- **Touch Targets:** Any interactive element (buttons, list items, checkboxes) must maintain a minimum height of 48px to accommodate industrial-use cases where precision might be hampered by movement or environment.
- **Vertical Rhythm:** Content is stacked using the `stack` units. `stack-sm` (4px) is for grouping labels with inputs; `stack-md` (12px) is for separating elements within a card; `stack-lg` (24px) separates distinct sections of the UI.
- **Compactness:** While padding is generous for touch, the internal padding of cards is kept to 12px to allow more data columns to be visible simultaneously.

## Elevation & Depth

To maintain a professional and "flat" business aesthetic, depth is used sparingly and only to denote interaction layers.

- **Base Layer:** The application background uses the primary white.
- **Surface Layer:** Secondary containers, search bars, and navigation backgrounds use the soft gray (#F3F4F6) with no shadow.
- **Card Elevation:** Interactive cards use a white background with a 1px border (#E5E7EB) and a very subtle ambient shadow: `0 1px 3px 0 rgba(0, 0, 0, 0.1)`. This provides a slight "lift" without appearing heavy or skeuomorphic.
- **Active State:** When a card or list item is pressed, it should transition to a primary-tinted background (5% opacity of Blue) rather than increasing shadow depth.
- **Modals/Drawers:** Use a backdrop blur (8px) and a higher elevation shadow to pull focus to critical decision points.

## Shapes

The shape language balances modern software aesthetics with professional restraint.

- **Standard Radius:** Components such as cards, input fields, and buttons use a **0.5rem (8px)** radius. This provides a "softened professional" look that feels modernized compared to legacy ERP systems.
- **Large Radius:** Larger containers or bottom sheets use **1rem (16px)** on top corners to signify they are temporary overlays.
- **Logic:** Sharp corners are avoided to reduce visual "noise" in dense data grids, but "Pill" shapes are also avoided to maintain a serious, business-functional tone.

## Components

### Buttons
- **Primary:** Solid Blue (#2563EB) with white text. 48px height.
- **Secondary:** Surface Gray (#F3F4F6) with Slate text. Used for "Cancel" or "Edit" actions.
- **Icon Buttons:** Used for "Add to Cart" or "Filter" in headers, maintaining a 48x48px tap area.

### Status Chips
- Small, 24px height badges with high-contrast text. 
- Use a light-tint background of the status color (10% opacity) with a solid text color for high legibility without being overwhelming.

### Data Cards
- White background, 8px radius, 1px border.
- Layout: Title and SKU on the left, Quantity and Status Chip on the right.
- Visual feedback on tap: #EFF6FF (Light blue tint).

### Inputs
- Outlined style with a 1px gray border.
- On focus: Border changes to Primary Blue with a 2px thickness.
- Labels are always visible above the input, never floating, to ensure clarity during fast data entry.

### Lists
- Standardized 64px-72px height per row.
- Separated by a 1px hairline divider (#F3F4F6).
- Include trailing chevron icons for items that lead to a detail view.