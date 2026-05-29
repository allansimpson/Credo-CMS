// public-tokens.jsx — Three direction themes + shared primitives for the
// public church site prototype.
//
// Each direction is a small object of CSS custom properties applied to a
// <PublicFrame theme="..."> wrapper. Components read from var(--bg) etc
// and adapt automatically. Keeps the screens themselves theme-agnostic.

const PUBLIC_THEMES = {
  // Direction 1 — Editorial Warm
  // Extends the admin language to the public site. Cream paper, ink, warm
  // accent. Single sans. Big numerals + rule dividers as signature.
  editorial: {
    bg: '#f6f4ef',
    panel: '#fbfaf6',
    panelAlt: '#f0ede5',
    inset: '#1a1815',          // dark insets (pull-quotes, footer)
    insetFg: '#f5efe2',
    fg: '#1a1815',
    fgSoft: '#3a352e',
    muted: '#7a7165',
    border: '#dcd5c5',
    borderSoft: '#ebe6d8',
    accent: '#b8531a',
    accentFg: '#fbfaf6',
    accentSoft: '#b8531a22',
    fontHeading: '"Inter Tight", system-ui, -apple-system, sans-serif',
    fontBody: '"Inter Tight", system-ui, -apple-system, sans-serif',
    fontMono: '"JetBrains Mono", ui-monospace, monospace',
    headingTracking: '-0.02em',
    label: 'Editorial Warm',
  },

  // Direction 2 — Cathedral
  // Traditional church gravitas. Serif display, sans body, gold accent,
  // bone + ink. Square corners read as intentional.
  cathedral: {
    bg: '#f4ede0',
    panel: '#fbf6e9',
    panelAlt: '#ede5d2',
    inset: '#1c1a14',
    insetFg: '#efe7d4',
    fg: '#1c1a14',
    fgSoft: '#3d362a',
    muted: '#6e6452',
    border: '#cabd9e',
    borderSoft: '#ddd2b7',
    accent: '#a8801f',
    accentFg: '#fbf6e9',
    accentSoft: '#a8801f1c',
    fontHeading: '"Source Serif 4", "Source Serif Pro", Georgia, serif',
    fontBody: '"Inter Tight", system-ui, -apple-system, sans-serif',
    fontMono: '"JetBrains Mono", ui-monospace, monospace',
    headingTracking: '-0.005em',
    label: 'Cathedral',
  },

  // Direction 3 — Quiet Sanctuary
  // Pared-back, contemporary, lots of whitespace, very large type. Bone +
  // warm gray + a soft sage. Almost monochrome.
  quiet: {
    bg: '#fbfaf7',
    panel: '#ffffff',
    panelAlt: '#f4f2ec',
    inset: '#26302a',
    insetFg: '#e9eae3',
    fg: '#1f231f',
    fgSoft: '#48504a',
    muted: '#8b8e84',
    border: '#e3e1d8',
    borderSoft: '#eeece4',
    accent: '#5b7a5a',
    accentFg: '#ffffff',
    accentSoft: '#5b7a5a18',
    fontHeading: '"Inter Tight", system-ui, -apple-system, sans-serif',
    fontBody: '"Inter Tight", system-ui, -apple-system, sans-serif',
    fontMono: '"JetBrains Mono", ui-monospace, monospace',
    headingTracking: '-0.03em',
    label: 'Quiet Sanctuary',
  },
};

function themeVars(t) {
  return {
    '--bg': t.bg,
    '--panel': t.panel,
    '--panel-alt': t.panelAlt,
    '--inset': t.inset,
    '--inset-fg': t.insetFg,
    '--fg': t.fg,
    '--fg-soft': t.fgSoft,
    '--muted': t.muted,
    '--border': t.border,
    '--border-soft': t.borderSoft,
    '--accent': t.accent,
    '--accent-fg': t.accentFg,
    '--accent-soft': t.accentSoft,
    '--font-heading': t.fontHeading,
    '--font-body': t.fontBody,
    '--font-mono': t.fontMono,
    '--heading-tracking': t.headingTracking,
  };
}

function PublicFrame({ theme = 'editorial', children, style }) {
  const t = PUBLIC_THEMES[theme];
  return (
    <div style={{
      ...themeVars(t),
      width: '100%', height: '100%',
      background: 'var(--bg)',
      color: 'var(--fg)',
      fontFamily: 'var(--font-body)',
      fontSize: 14, lineHeight: 1.55,
      display: 'flex', flexDirection: 'column',
      overflow: 'auto',
      ...style,
    }}>{children}</div>
  );
}

// ─── Type primitives ─────────────────────────────────────────────────────

function MetaLabel({ children, style }) {
  return (
    <span style={{
      fontSize: 10.5, fontWeight: 600,
      letterSpacing: '0.18em', textTransform: 'uppercase',
      color: 'var(--muted)',
      fontFamily: 'var(--font-body)',
      ...style,
    }}>{children}</span>
  );
}

function Eyebrow({ children, accent, style }) {
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 10,
      fontSize: 11, fontWeight: 600,
      letterSpacing: '0.2em', textTransform: 'uppercase',
      color: accent ? 'var(--accent)' : 'var(--muted)',
      ...style,
    }}>
      <span style={{ width: 24, height: 1, background: 'currentColor', opacity: 0.6 }} />
      {children}
    </span>
  );
}

function Headline({ size = 56, children, style }) {
  return (
    <h1 style={{
      margin: 0,
      fontFamily: 'var(--font-heading)',
      fontSize: size,
      fontWeight: 600,
      letterSpacing: 'var(--heading-tracking)',
      lineHeight: 1.05,
      color: 'var(--fg)',
      ...style,
    }}>{children}</h1>
  );
}

function BigNum({ children, size = 36, style }) {
  return (
    <span style={{
      fontFamily: 'var(--font-heading)',
      fontSize: size,
      fontWeight: 600,
      letterSpacing: '-0.025em',
      lineHeight: 1,
      fontVariantNumeric: 'tabular-nums',
      color: 'var(--fg)',
      ...style,
    }}>{children}</span>
  );
}

// ─── Buttons / chips ─────────────────────────────────────────────────────

function Btn({ variant = 'primary', size = 'md', children, icon, iconRight, style, ...rest }) {
  const sizes = {
    sm: { height: 30, padding: '0 12px', fontSize: 12.5 },
    md: { height: 38, padding: '0 18px', fontSize: 13.5 },
    lg: { height: 46, padding: '0 22px', fontSize: 14 },
  };
  const variants = {
    primary: {
      background: 'var(--accent)', color: 'var(--accent-fg)',
      border: '1px solid var(--accent)',
    },
    secondary: {
      background: 'var(--panel)', color: 'var(--fg)',
      border: '1px solid var(--border)',
    },
    ghost: {
      background: 'transparent', color: 'var(--fg)',
      border: '1px solid transparent',
    },
    inverse: {
      background: 'transparent', color: 'var(--inset-fg)',
      border: '1px solid rgba(255,255,255,0.18)',
    },
    inverseFilled: {
      background: 'var(--accent)', color: 'var(--accent-fg)',
      border: '1px solid var(--accent)',
    },
  };
  return (
    <button style={{
      display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 8,
      fontFamily: 'var(--font-body)',
      fontWeight: 500,
      cursor: 'pointer', whiteSpace: 'nowrap',
      ...sizes[size],
      ...variants[variant],
      ...style,
    }} {...rest}>
      {icon && <span style={{ display: 'inline-flex' }}>{icon}</span>}
      {children}
      {iconRight && <span style={{ display: 'inline-flex' }}>{iconRight}</span>}
    </button>
  );
}

function Chip({ children, tone = 'muted', style }) {
  const tones = {
    muted: { color: 'var(--muted)', bg: 'var(--border-soft)' },
    accent: { color: 'var(--accent)', bg: 'var(--accent-soft)' },
    inverse: { color: 'var(--inset-fg)', bg: 'rgba(255,255,255,0.08)' },
  };
  const c = tones[tone];
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 6,
      padding: '4px 10px',
      fontSize: 10.5, fontWeight: 600,
      letterSpacing: '0.14em', textTransform: 'uppercase',
      background: c.bg, color: c.color,
      ...style,
    }}>{children}</span>
  );
}

// ─── Image placeholders (typographic, no decoration) ──────────────────────

function ImageSlot({ ratio = '16/10', label = 'Photo', tone = 'normal', style }) {
  const fg = tone === 'inset' ? 'rgba(245,239,226,0.5)' : 'var(--muted)';
  const bg = tone === 'inset' ? 'rgba(255,255,255,0.05)' : 'var(--panel-alt)';
  const border = tone === 'inset' ? '1px solid rgba(255,255,255,0.08)' : '1px solid var(--border)';
  return (
    <div style={{
      aspectRatio: ratio,
      background: bg,
      border,
      backgroundImage: `repeating-linear-gradient(135deg, transparent 0 18px, ${tone === 'inset' ? 'rgba(255,255,255,0.025)' : 'rgba(0,0,0,0.025)'} 18px 36px)`,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      color: fg,
      fontFamily: 'var(--font-mono)', fontSize: 11, letterSpacing: '0.16em', textTransform: 'uppercase',
      ...style,
    }}>{label}</div>
  );
}

// ─── Icons (inline SVG, single stroke style) ──────────────────────────────

function PIcon({ name, size = 16, color = 'currentColor' }) {
  const stroke = { stroke: color, strokeWidth: 1.6, strokeLinecap: 'round', strokeLinejoin: 'round', fill: 'none' };
  const s = { width: size, height: size, display: 'inline-block', flexShrink: 0 };
  switch (name) {
    case 'arrow-right': return <svg viewBox="0 0 24 24" style={s}><path d="M4 12h16M14 6l6 6-6 6" {...stroke} /></svg>;
    case 'arrow-down': return <svg viewBox="0 0 24 24" style={s}><path d="M12 4v16M6 14l6 6 6-6" {...stroke} /></svg>;
    case 'play': return <svg viewBox="0 0 24 24" style={s}><path d="M8 5l11 7-11 7V5z" {...stroke} /></svg>;
    case 'mail': return <svg viewBox="0 0 24 24" style={s}><rect x="3" y="5" width="18" height="14" {...stroke} /><path d="M3 7l9 7 9-7" {...stroke} /></svg>;
    case 'phone': return <svg viewBox="0 0 24 24" style={s}><path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2z" {...stroke} /></svg>;
    case 'pin': return <svg viewBox="0 0 24 24" style={s}><path d="M12 22s7-7 7-12a7 7 0 0 0-14 0c0 5 7 12 7 12z" {...stroke} /><circle cx="12" cy="10" r="2.5" {...stroke} /></svg>;
    case 'clock': return <svg viewBox="0 0 24 24" style={s}><circle cx="12" cy="12" r="9" {...stroke} /><path d="M12 7v5l3 2" {...stroke} /></svg>;
    case 'calendar': return <svg viewBox="0 0 24 24" style={s}><rect x="3" y="5" width="18" height="16" {...stroke} /><path d="M3 10h18M8 3v4M16 3v4" {...stroke} /></svg>;
    case 'menu': return <svg viewBox="0 0 24 24" style={s}><path d="M4 7h16M4 12h16M4 17h16" {...stroke} /></svg>;
    case 'search': return <svg viewBox="0 0 24 24" style={s}><circle cx="11" cy="11" r="7" {...stroke} /><path d="M21 21l-4.5-4.5" {...stroke} /></svg>;
    case 'church': return <svg viewBox="0 0 24 24" style={s}><path d="M12 2v4M10 4h4M5 22V11l7-3 7 3v11M5 22h14M9 22v-5h6v5M11 13h2" {...stroke} /></svg>;
    case 'cross': return <svg viewBox="0 0 24 24" style={s}><path d="M12 3v18M6 9h12" {...stroke} /></svg>;
    case 'facebook': return <svg viewBox="0 0 24 24" style={s}><path d="M14 8h2V5h-2a3 3 0 0 0-3 3v2H9v3h2v8h3v-8h2.5l.5-3H14V8z" {...stroke} /></svg>;
    case 'instagram': return <svg viewBox="0 0 24 24" style={s}><rect x="3" y="3" width="18" height="18" rx="0" {...stroke} /><circle cx="12" cy="12" r="4" {...stroke} /><circle cx="17" cy="7" r="0.8" fill={color} /></svg>;
    case 'youtube': return <svg viewBox="0 0 24 24" style={s}><rect x="2" y="6" width="20" height="12" {...stroke} /><path d="M10 9l5 3-5 3V9z" {...stroke} /></svg>;
    case 'check': return <svg viewBox="0 0 24 24" style={s}><path d="M5 13l4 4 10-12" {...stroke} /></svg>;
    case 'plus': return <svg viewBox="0 0 24 24" style={s}><path d="M12 5v14M5 12h14" {...stroke} /></svg>;
    case 'lock': return <svg viewBox="0 0 24 24" style={s}><rect x="5" y="11" width="14" height="10" {...stroke} /><path d="M8 11V8a4 4 0 0 1 8 0v3" {...stroke} /></svg>;
    case 'quote': return <svg viewBox="0 0 24 24" style={s}><path d="M7 17V11a4 4 0 0 1 4-4M14 17V11a4 4 0 0 1 4-4" {...stroke} /></svg>;
    default: return <svg viewBox="0 0 24 24" style={s}><circle cx="12" cy="12" r="9" {...stroke} /></svg>;
  }
}

Object.assign(window, {
  PUBLIC_THEMES, themeVars, PublicFrame,
  MetaLabel, Eyebrow, Headline, BigNum,
  Btn, Chip, ImageSlot, PIcon,
});
