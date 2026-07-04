---
name: Subiekt Mobile System
colors:
  surface: '#f8f9fa'
  surface-dim: '#d9dadb'
  surface-bright: '#f8f9fa'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f4f5'
  surface-container: '#edeeef'
  surface-container-high: '#e7e8e9'
  surface-container-highest: '#e1e3e4'
  on-surface: '#191c1d'
  on-surface-variant: '#444651'
  inverse-surface: '#2e3132'
  inverse-on-surface: '#f0f1f2'
  outline: '#757682'
  outline-variant: '#c5c5d3'
  surface-tint: '#4059aa'
  primary: '#00236f'
  on-primary: '#ffffff'
  primary-container: '#1e3a8a'
  on-primary-container: '#90a8ff'
  inverse-primary: '#b6c4ff'
  secondary: '#006c4a'
  on-secondary: '#ffffff'
  secondary-container: '#82f5c1'
  on-secondary-container: '#00714e'
  tertiary: '#442100'
  on-tertiary: '#ffffff'
  tertiary-container: '#653400'
  on-tertiary-container: '#fc922b'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dce1ff'
  primary-fixed-dim: '#b6c4ff'
  on-primary-fixed: '#00164e'
  on-primary-fixed-variant: '#264191'
  secondary-fixed: '#85f8c4'
  secondary-fixed-dim: '#68dba9'
  on-secondary-fixed: '#002114'
  on-secondary-fixed-variant: '#005137'
  tertiary-fixed: '#ffdcc3'
  tertiary-fixed-dim: '#ffb77d'
  on-tertiary-fixed: '#2f1500'
  on-tertiary-fixed-variant: '#6e3900'
  background: '#f8f9fa'
  on-background: '#191c1d'
  surface-variant: '#e1e3e4'
typography:
  headline-lg:
    fontFamily: Inter
    fontSize: 30px
    fontWeight: '700'
    lineHeight: 38px
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
  headline-lg-mobile:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  base: 4px
  xs: 8px
  sm: 12px
  md: 16px
  lg: 24px
  xl: 32px
  touch-target: 48px
---

## Brand & Style

This design system is engineered for the high-utility environment of Polish warehouse management and ERP operations. The brand personality is **authoritative, efficient, and resilient**. It prioritizes data integrity and speed of task completion over decorative aesthetics.

The design style is **Corporate Modern with a Utility focus**. It utilizes high-contrast interfaces, a structured grid, and clear visual hierarchies to ensure readability in varying light conditions (from bright offices to dimly lit warehouses). The aesthetic is grounded in professionalism, evoking a sense of "software as a tool." Every element is optimized for touch interaction on mobile devices while maintaining the dense information architecture required for desktop ERP workflows.

## Colors

The palette is strictly functional, utilizing high-chroma signals for status-driven workflows.

- **Primary (Granatowy):** Used for primary actions, navigation headers, and branding elements. It establishes a sense of institutional trust.
- **Secondary (Zielony):** Reserved for "Success" states, such as "Skompletowano" (Packed) or "Wysłano" (Sent).
- **Warning (Bursztynowy):** Indicates urgency or incomplete data, such as "Brakujące dane" or "Zbliżający się termin."
- **Danger (Czerwony):** Critical errors, overdue payments, or "Błąd synchronizacji."
- **Neutral (Tło):** A clean `#F9FAFB` base to maximize contrast with black text (`#111827`) and reduce eye strain during prolonged use.

## Typography

**Inter** is selected for its exceptional legibility and neutral character, crucial for reading SKU numbers and quantities. 

- **Polish Characters:** Ensure full support for *ą, ć, ę, ł, ń, ó, ś, ź, ż*.
- **Numeric Clarity:** Use tabular figures (monospaced numbers) for tables and inventory lists to ensure decimal points align vertically.
- **Hierarchy:** Use `label-md` for table headers and section titles to differentiate between static labels and dynamic data.

## Layout & Spacing

The design system follows a mobile-first, fluid grid approach that transitions to a fixed sidebar layout for desktop displays.

- **Mobile (PWA):** Uses a 4-column grid with 16px margins. Bottom navigation is persistent for thumb-reach accessibility. All interactive elements (buttons, inputs) must meet a minimum `48px` touch target height.
- **Desktop:** Transitions to a 12-column grid with a fixed 280px left-hand sidebar for navigation. 
- **Inventory Tables:** On mobile, tables reflow into "Data Cards." On desktop, they utilize a dense row height (40px) to maximize information density.
- **Safe Areas:** Adhere to "Safe Area Insets" for modern mobile browsers to prevent overlap with notch or home indicators.

## Elevation & Depth

This system avoids soft shadows and skeuomorphism in favor of **Low-Contrast Outlines** and **Tonal Layers**.

- **Cards:** Use a 1px solid border (`#E5E7EB`) instead of shadows to define container boundaries.
- **Active States:** Subtle background shifts (e.g., `#F3F4F6`) indicate hover or press.
- **Modals/Drawers:** Use a high-contrast backdrop overlay (60% opacity black) to focus the user's attention during critical inventory adjustments.
- **Z-Index:**
  1. Base Surface
  2. Floating Action Buttons (FAB) for scanning
  3. Persistent Navigation
  4. Overlays/Modals

## Shapes

The "Soft" setting is applied to maintain a professional, slightly technical appearance. 

- **Standard Elements:** Buttons and Input fields use a 4px (`0.25rem`) radius.
- **Cards & Modals:** Use an 8px (`0.5rem`) radius to provide a clear but subtle visual distinction from the background.
- **Badges:** Use a 100px "pill" radius to differentiate status indicators from functional buttons.

## Components

- **Buttons:** Primary buttons use solid `#1E3A8A` with white text. Ghost buttons use `#1E3A8A` borders for secondary actions like "Anuluj" (Cancel).
- **Status Badges:** Must contain an icon (e.g., Checkmark for "OK", Clock for "Pending") alongside text. Backgrounds are low-saturation tints of the status color with high-saturation text for contrast.
- **Input Fields:** Large text inputs with clear floating labels. Include a "Scan" icon suffix for fields where SKU/EAN input is expected.
- **Data Tables:** Zebra-striping (`#F9FAFB`) for readability. Headers are sticky on both mobile and desktop.
- **Mobile Navigation:** 4-5 icon-based destinations: "Pulpit" (Dashboard), "Magazyn" (Warehouse), "Skanuj" (Scan - Central Action), "Zamówienia" (Orders), "Więcej" (More).
- **Action Drawers:** Use bottom sheets on mobile for quick data entry (e.g., changing quantity) to keep the interaction within the thumb zone.
- **List Items:** Feature chevron indicators (`>`) for drill-down actions and bolded primary data (e.g., Product Name).