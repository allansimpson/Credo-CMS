// sermon-byseries-ideas.jsx — Five design directions for the "Sermons by
// Series" page. Same Editorial Warm language as By Book / the date-grouped
// listing — structure is what differs between directions.

// ─── Series data ─────────────────────────────────────────────────────────
// A series is the organizing unit: a run of messages through a book, a
// theme, or a season. Active series carry a progress notion; completed ones
// carry a fixed part count + date range.

const SERIES = [
  {
    title: 'Made to Belong', scope: 'Luke 14–15', context: 'AM Worship',
    status: 'active', parts: 4, planned: 6, start: 'Sep 2024', end: 'Present',
    startY: 2024, startM: 8, endY: 2024, endM: 9, speaker: 'Daniel Reyes',
    blurb: 'Jesus at the table — parables of welcome, the cost of following, and the Father who runs to meet the lost.',
    latest: { part: 4, title: 'A Table With Room', passage: 'Luke 14:15–24', date: 'Oct 06' },
  },
  {
    title: 'Through Hebrews', scope: 'Hebrews', context: 'AM Bible Class',
    status: 'active', parts: 12, planned: 13, start: 'Feb 2024', end: 'Present',
    startY: 2024, startM: 1, endY: 2024, endM: 9, speaker: 'Mark Daniels',
    blurb: 'A verse-by-verse walk through the letter to the Hebrews — a better covenant, a better priest, better promises.',
    latest: { part: 12, title: 'A Consuming Fire', passage: 'Hebrews 12', date: 'Oct 06' },
  },
  {
    title: 'Praying the Psalms', scope: 'Selected Psalms', context: 'Wednesday Night',
    status: 'active', parts: 13, planned: null, start: 'Jan 2024', end: 'Present',
    startY: 2024, startM: 0, endY: 2024, endM: 9, speaker: 'Various',
    blurb: 'Learning to pray with the songbook of the Bible — lament, trust, and praise, one psalm at a time.',
    latest: { part: 13, title: 'How Long, O Lord', passage: 'Psalm 13', date: 'Oct 02' },
  },
  {
    title: 'The Sermon on the Mount', scope: 'Matthew 5–7', context: 'AM Worship',
    status: 'complete', parts: 9, start: 'Apr 2023', end: 'Jun 2023',
    startY: 2023, startM: 3, endY: 2023, endM: 5, speaker: 'Daniel Reyes',
    blurb: 'The constitution of the kingdom — what life looks like under the reign of the King.',
  },
  {
    title: 'The Light Has Come', scope: 'Advent · Various', context: 'AM Worship',
    status: 'complete', parts: 4, start: 'Dec 2023', end: 'Dec 2023',
    startY: 2023, startM: 11, endY: 2023, endM: 11, speaker: 'Daniel Reyes',
    blurb: 'Four Sundays of Advent — hope, peace, joy, and the Word made flesh.',
  },
  {
    title: 'Letters to the Seven Churches', scope: 'Revelation 2–3', context: 'PM Worship',
    status: 'complete', parts: 7, start: 'Sep 2023', end: 'Nov 2023',
    startY: 2023, startM: 8, endY: 2023, endM: 10, speaker: 'Tom Hartline',
    blurb: 'Christ’s words to his churches — commendation, warning, and a promise to the one who overcomes.',
  },
  {
    title: 'Names of God', scope: 'Various', context: 'PM Worship',
    status: 'complete', parts: 7, start: 'Mar 2023', end: 'May 2023',
    startY: 2023, startM: 2, endY: 2023, endM: 4, speaker: 'Various',
    blurb: 'Knowing God by the names he reveals — Provider, Shepherd, Banner, Peace.',
  },
  {
    title: 'Living Faith', scope: 'James', context: 'AM Worship',
    status: 'complete', parts: 5, start: 'Jan 2023', end: 'Feb 2023',
    startY: 2023, startM: 0, endY: 2023, endM: 1, speaker: 'Daniel Reyes',
    blurb: 'Faith that works — wisdom from above, the taming of the tongue, and the prayer of the righteous.',
  },
  {
    title: 'First Things', scope: 'Genesis 1–11', context: 'AM Bible Class',
    status: 'complete', parts: 11, start: 'Sep 2022', end: 'Feb 2023',
    startY: 2022, startM: 8, endY: 2023, endM: 1, speaker: 'Mark Daniels',
    blurb: 'Creation, fall, flood, and the scattering — the foundations the whole Bible is built upon.',
  },
  {
    title: 'A Living Hope', scope: '1 Peter', context: 'AM Worship',
    status: 'complete', parts: 8, start: 'Sep 2022', end: 'Nov 2022',
    startY: 2022, startM: 8, endY: 2022, endM: 10, speaker: 'Daniel Reyes',
    blurb: 'Holiness and hope for exiles — standing firm in a world that is not our home.',
  },
  {
    title: 'The Shepherd Psalm', scope: 'Psalm 23', context: 'PM Worship',
    status: 'complete', parts: 4, start: 'Jul 2022', end: 'Aug 2022',
    startY: 2022, startM: 6, endY: 2022, endM: 7, speaker: 'Tom Hartline',
    blurb: 'A summer in the most beloved of psalms — the Lord who leads, restores, and keeps.',
  },
  {
    title: 'The Servant King', scope: 'Mark', context: 'AM Bible Class',
    status: 'complete', parts: 16, start: 'Jan 2021', end: 'Aug 2022',
    startY: 2021, startM: 0, endY: 2022, endM: 7, speaker: 'Mark Daniels',
    blurb: 'A steady walk through the action-driven gospel of Mark — on the road with the Servant King.',
  },
];

const SERIES_ACTIVE = SERIES.filter(s => s.status === 'active');
const SERIES_DONE = SERIES.filter(s => s.status === 'complete');
const TOTAL_SERIES = SERIES.length;
const TOTAL_MESSAGES = SERIES.reduce((n, s) => n + s.parts, 0);

// Context color — keep the four teaching tracks visually distinct, matching
// the date-grouped listing's vocabulary.
const SERIES_CTX_COLOR = {
  'AM Worship': 'var(--fg)',
  'AM Bible Class': 'var(--muted)',
  'PM Worship': 'var(--accent)',
  'Wednesday Night': 'var(--accent)',
};

// Group completed series by start-year (newest first).
function seriesByYear(list) {
  const map = {};
  for (const s of list) (map[s.startY] = map[s.startY] || []).push(s);
  return Object.keys(map).map(Number).sort((a, b) => b - a).map(y => ({ year: y, items: map[y] }));
}

// ─── Shared small parts ──────────────────────────────────────────────────

function SeriesPageHeader({ idea, oneLine }) {
  return (
    <>
      <section style={{ padding: '52px 56px 28px', borderBottom: '1px solid var(--border)' }}>
        <div style={{ maxWidth: 1180, margin: '0 auto', display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', flexWrap: 'wrap', gap: 16 }}>
          <div>
            <Eyebrow accent>Sermons · By Series</Eyebrow>
            <Headline size={64} style={{ marginTop: 14 }}>Follow the thread.</Headline>
          </div>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11.5, color: 'var(--muted)', letterSpacing: '0.08em' }}>
            {TOTAL_SERIES} SERIES · {TOTAL_MESSAGES} MESSAGES · {SERIES_ACTIVE.length} RUNNING NOW
          </span>
        </div>
      </section>
      <section style={{ padding: '14px 56px', background: 'var(--inset)', color: 'var(--inset-fg)' }}>
        <div style={{ maxWidth: 1180, margin: '0 auto', display: 'flex', alignItems: 'baseline', gap: 20, flexWrap: 'wrap' }}>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, letterSpacing: '0.18em', textTransform: 'uppercase', color: 'var(--accent)' }}>Idea</span>
          <span style={{ fontFamily: 'var(--font-heading)', fontSize: 18, fontWeight: 600, letterSpacing: '-0.01em' }}>{idea}</span>
          <span style={{ fontSize: 13, color: 'rgba(245,239,226,0.65)', flex: 1, minWidth: 0 }}>{oneLine}</span>
        </div>
      </section>
    </>
  );
}

// Filter / view-switch bar shared with By Book + the listing. "By Series" is
// the active tab here.
function SeriesFilterBar({ children }) {
  return (
    <section style={{ padding: '24px 56px', borderBottom: '1px solid var(--border)', background: 'var(--panel-alt)' }}>
      <div style={{ maxWidth: 1180, margin: '0 auto', display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
        <PIcon name="search" size={16} color="var(--muted)" />
        <input
          placeholder="Search series — 'Hebrews', 'Advent', 'Psalms'"
          style={{ flex: 1, minWidth: 200, padding: '10px 14px', fontSize: 14, background: 'var(--panel)', border: '1px solid var(--border)', fontFamily: 'var(--font-body)', color: 'var(--fg)' }}
        />
        <Btn variant="secondary" size="sm">Latest</Btn>
        <Btn variant="primary" size="sm" style={{ background: 'var(--inset)' }}>By Series</Btn>
        <Btn variant="secondary" size="sm">By Book</Btn>
        {children}
      </div>
    </section>
  );
}

function ContextLabel({ context, size = 11 }) {
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 8,
      fontFamily: 'var(--font-mono)', fontSize: size, fontWeight: 600,
      letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--fg-soft)',
    }}>
      <span style={{ width: 8, height: 8, background: SERIES_CTX_COLOR[context], borderRadius: '50%', display: 'inline-block' }} />
      {context}
    </span>
  );
}

function StatusChip({ status }) {
  if (status === 'active') return <Chip tone="accent">Now preaching</Chip>;
  return <Chip tone="muted">Complete</Chip>;
}

// Local coverage bar (the token file doesn't export one).
function SeriesCoverageBar({ covered, total, height = 6 }) {
  return (
    <div style={{ position: 'relative', height, background: 'var(--border-soft)', overflow: 'hidden' }}>
      <div style={{ position: 'absolute', top: 0, left: 0, bottom: 0, width: `${(covered / total) * 100}%`, background: 'var(--accent)' }} />
    </div>
  );
}

// "Part 4 of 6 · 67%" style progress line for active series.
function ActiveProgress({ s, height = 8 }) {
  const known = s.planned != null;
  return (
    <div>
      {known ? (
        <SeriesCoverageBar covered={s.parts} total={s.planned} height={height} />
      ) : (
        <div style={{ height, background: 'var(--accent)', opacity: 0.85 }} />
      )}
      <div style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--muted)', marginTop: 8, letterSpacing: '0.04em' }}>
        {known
          ? `PART ${s.parts} OF ~${s.planned} · ${Math.round((s.parts / s.planned) * 100)}%`
          : `${s.parts} PARTS · ONGOING`}
      </div>
    </div>
  );
}

function PlayBadge({ size = 44 }) {
  return (
    <button style={{
      width: size, height: size, background: 'var(--accent-soft)', color: 'var(--accent)',
      border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer', flexShrink: 0,
    }}>
      <PIcon name="play" size={Math.round(size * 0.36)} />
    </button>
  );
}


// ═════════════════════════════════════════════════════════════════════════
// 1 — Series Shelf
// A magazine-style card grid. Each series is a tile: series art with a
// status chip, the title in the editorial heading face, scope + context,
// a one-line blurb, and a footer that either shows live progress (active)
// or the part count + date span (complete). Most visual; best for a church
// that invests in series artwork.
// ═════════════════════════════════════════════════════════════════════════
function SeriesCard({ s }) {
  return (
    <a href="#" style={{
      display: 'flex', flexDirection: 'column',
      background: 'var(--panel)', border: '1px solid var(--border)',
      color: 'var(--fg)', textDecoration: 'none',
    }}>
      <div style={{ position: 'relative', background: 'var(--inset)' }}>
        <ImageSlot ratio="16/9" label={`${s.title} · art`} tone="inset" />
        <div style={{ position: 'absolute', left: 14, top: 14 }}>
          <StatusChip status={s.status} />
        </div>
      </div>
      <div style={{ padding: '22px 24px 24px', display: 'flex', flexDirection: 'column', flex: 1 }}>
        <ContextLabel context={s.context} />
        <Headline size={27} style={{ fontWeight: 600, marginTop: 12, lineHeight: 1.1 }}>{s.title}</Headline>
        <div style={{ fontFamily: 'var(--font-mono)', fontSize: 12, color: 'var(--accent)', marginTop: 8, letterSpacing: '0.04em' }}>{s.scope}</div>
        <p style={{ fontSize: 13.5, color: 'var(--fg-soft)', marginTop: 12, lineHeight: 1.55, flex: 1, textWrap: 'pretty' }}>{s.blurb}</p>
        <div style={{ marginTop: 18, paddingTop: 16, borderTop: '1px solid var(--border-soft)' }}>
          {s.status === 'active' ? (
            <ActiveProgress s={s} height={6} />
          ) : (
            <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between' }}>
              <span style={{ fontFamily: 'var(--font-heading)', fontSize: 30, fontWeight: 600, letterSpacing: '-0.03em', color: 'var(--fg)' }}>
                {s.parts}<span style={{ fontSize: 14, color: 'var(--muted)', fontWeight: 500, marginLeft: 6 }}>parts</span>
              </span>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--muted)', letterSpacing: '0.06em' }}>{s.start} – {s.end}</span>
            </div>
          )}
        </div>
      </div>
    </a>
  );
}

function SeriesShelfDirection() {
  return (
    <div data-direction="editorial" style={{ width: '100%', minHeight: '100%' }}>
      <PublicFrame theme="editorial">
        <SeriesPageHeader
          idea="1 · Series Shelf"
          oneLine="Magazine-style card grid. Series art, status chip, blurb, and either live progress (active) or part count + span (complete). Most visual — rewards series artwork."
        />
        <SeriesFilterBar />
        <section style={{ padding: '40px 56px 72px' }}>
          <div style={{ maxWidth: 1180, margin: '0 auto' }}>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 16, marginBottom: 22 }}>
              <Eyebrow accent>Now preaching</Eyebrow>
              <span style={{ flex: 1, height: 1, background: 'var(--border)' }} />
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24, marginBottom: 48 }}>
              {SERIES_ACTIVE.map((s, i) => <SeriesCard key={i} s={s} />)}
            </div>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 16, marginBottom: 22 }}>
              <Eyebrow>The archive</Eyebrow>
              <span style={{ flex: 1, height: 1, background: 'var(--border)' }} />
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--muted)', letterSpacing: '0.1em' }}>{SERIES_DONE.length} COMPLETED SERIES</span>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24 }}>
              {SERIES_DONE.map((s, i) => <SeriesCard key={i} s={s} />)}
            </div>
          </div>
        </section>
      </PublicFrame>
    </div>
  );
}


// ═════════════════════════════════════════════════════════════════════════
// 2 — The Index
// Editorial full-width list. Active series sit up top under "Now preaching"
// with a live progress bar; completed series are grouped by year. Each row
// leads with the title in the heading face, carries scope + context + speaker
// as mono meta, and ends with a big part-count numeral. Dense, archival,
// fastest to scan a long history.
// ═════════════════════════════════════════════════════════════════════════
function IndexRow({ s }) {
  return (
    <a href="#" style={{
      display: 'grid', gridTemplateColumns: '1fr auto', alignItems: 'center', gap: 32,
      padding: '22px 0', borderBottom: '1px solid var(--border-soft)',
      color: 'var(--fg)', textDecoration: 'none',
    }}>
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
          <span style={{ fontFamily: 'var(--font-heading)', fontSize: 25, fontWeight: 600, letterSpacing: '-0.018em' }}>{s.title}</span>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 12, color: 'var(--accent)', letterSpacing: '0.04em' }}>{s.scope}</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 18, marginTop: 9, flexWrap: 'wrap' }}>
          <ContextLabel context={s.context} size={10.5} />
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11.5, color: 'var(--muted)' }}>{s.speaker}</span>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11.5, color: 'var(--muted)' }}>{s.start} – {s.end}</span>
        </div>
        {s.status === 'active' && (
          <div style={{ marginTop: 14, maxWidth: 360 }}><ActiveProgress s={s} height={6} /></div>
        )}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontFamily: 'var(--font-heading)', fontSize: 40, fontWeight: 600, letterSpacing: '-0.03em', color: s.status === 'active' ? 'var(--accent)' : 'var(--fg)', lineHeight: 1 }}>{s.parts}</div>
          <div style={{ fontFamily: 'var(--font-mono)', fontSize: 10, color: 'var(--muted)', letterSpacing: '0.14em', textTransform: 'uppercase', marginTop: 4 }}>parts</div>
        </div>
        <PIcon name="arrow-right" size={16} color="var(--muted)" />
      </div>
    </a>
  );
}

function TheIndexDirection() {
  const years = seriesByYear(SERIES_DONE);
  return (
    <div data-direction="editorial" style={{ width: '100%', minHeight: '100%' }}>
      <PublicFrame theme="editorial">
        <SeriesPageHeader
          idea="2 · The Index"
          oneLine="Editorial full-width list. Active series on top with live progress, completed series grouped by year. Leads with the title, ends with a big part-count. Densest, fastest to scan."
        />
        <SeriesFilterBar />
        <section style={{ padding: '44px 56px 72px' }}>
          <div style={{ maxWidth: 1100, margin: '0 auto' }}>
            <div style={{
              fontFamily: 'var(--font-mono)', fontSize: 11, fontWeight: 600, letterSpacing: '0.18em',
              textTransform: 'uppercase', color: 'var(--accent)', paddingBottom: 14,
              borderBottom: '2px solid var(--inset)', marginBottom: 4,
            }}>
              Now preaching <span style={{ marginLeft: 8, opacity: 0.6, fontWeight: 400 }}>· {SERIES_ACTIVE.length}</span>
            </div>
            {SERIES_ACTIVE.map((s, i) => <IndexRow key={i} s={s} />)}

            <div style={{ marginTop: 48 }}>
              {years.map(({ year, items }) => (
                <div key={year} style={{ marginBottom: 40 }}>
                  <div style={{
                    display: 'flex', alignItems: 'baseline', gap: 16,
                    paddingBottom: 14, borderBottom: '2px solid var(--inset)', marginBottom: 4,
                  }}>
                    <span style={{ fontFamily: 'var(--font-heading)', fontSize: 30, fontWeight: 600, letterSpacing: '-0.02em' }}>{year}</span>
                    <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--muted)', letterSpacing: '0.12em' }}>{items.length} SERIES</span>
                  </div>
                  {items.map((s, i) => <IndexRow key={i} s={s} />)}
                </div>
              ))}
            </div>
          </div>
        </section>
      </PublicFrame>
    </div>
  );
}


// ═════════════════════════════════════════════════════════════════════════
// 3 — Featured + Archive
// A large hero for the flagship running series (Made to Belong), with art,
// blurb, progress, and a jump to the latest message. A compact strip of the
// other active series sits beside/below it, then the completed archive as a
// tight two-column index. Editorial entry point without losing the archive.
// ═════════════════════════════════════════════════════════════════════════
function ArchiveIndexRow({ s }) {
  return (
    <a href="#" style={{
      display: 'grid', gridTemplateColumns: '1fr auto', alignItems: 'baseline', gap: 16,
      padding: '13px 0', borderBottom: '1px solid var(--border-soft)',
      color: 'var(--fg)', textDecoration: 'none',
    }}>
      <div>
        <div style={{ fontFamily: 'var(--font-heading)', fontSize: 18, fontWeight: 500, letterSpacing: '-0.01em' }}>{s.title}</div>
        <div style={{ fontFamily: 'var(--font-mono)', fontSize: 10.5, color: 'var(--muted)', marginTop: 3, letterSpacing: '0.04em' }}>{s.scope} · {s.start}</div>
      </div>
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 13, color: 'var(--accent)', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>{s.parts}</span>
    </a>
  );
}

function FeaturedArchiveDirection() {
  const hero = SERIES.find(s => s.title === 'Made to Belong');
  const otherActive = SERIES_ACTIVE.filter(s => s !== hero);
  const years = seriesByYear(SERIES_DONE);
  const half = Math.ceil(years.length / 2);
  const colA = years.slice(0, half), colB = years.slice(half);
  return (
    <div data-direction="editorial" style={{ width: '100%', minHeight: '100%' }}>
      <PublicFrame theme="editorial">
        <SeriesPageHeader
          idea="3 · Featured + Archive"
          oneLine="A hero for the flagship running series, the other active series as compact siblings, then the completed archive as a tight two-column index. Editorial entry point."
        />
        <SeriesFilterBar />
        {/* Hero */}
        <section style={{ padding: '48px 56px', borderBottom: '1px solid var(--border)' }}>
          <div style={{ maxWidth: 1180, margin: '0 auto' }}>
            <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginBottom: 24 }}>
              <Eyebrow accent>Now preaching</Eyebrow>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11.5, color: 'var(--muted)', letterSpacing: '0.12em' }}>LATEST · {hero.latest.date}</span>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1.3fr 1fr', gap: 40 }}>
              <a href="#" style={{ position: 'relative', background: 'var(--inset)', textDecoration: 'none' }}>
                <ImageSlot ratio="16/9" label="Made to Belong · series art" tone="inset" />
                <div style={{ position: 'absolute', left: 18, top: 18 }}><Chip tone="accent">{hero.context}</Chip></div>
                <button style={{
                  position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)',
                  width: 60, height: 60, background: 'var(--accent)', color: 'var(--accent-fg)',
                  border: 'none', display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer',
                }}><PIcon name="play" size={22} /></button>
              </a>
              <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
                <ContextLabel context={hero.context} />
                <Headline size={48} style={{ fontWeight: 600, marginTop: 12 }}>{hero.title}</Headline>
                <div style={{ fontFamily: 'var(--font-mono)', fontSize: 12.5, color: 'var(--accent)', marginTop: 10, letterSpacing: '0.04em' }}>{hero.scope} · {hero.speaker}</div>
                <p style={{ fontSize: 16, color: 'var(--fg-soft)', marginTop: 16, lineHeight: 1.6, textWrap: 'pretty' }}>{hero.blurb}</p>
                <div style={{ marginTop: 20, maxWidth: 380 }}><ActiveProgress s={hero} height={8} /></div>
                <div style={{ marginTop: 22, display: 'flex', gap: 12 }}>
                  <Btn variant="primary" icon={<PIcon name="play" size={14} />}>Latest message</Btn>
                  <Btn variant="secondary" iconRight={<PIcon name="arrow-right" size={14} />}>All {hero.parts} so far</Btn>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* Other active */}
        <section style={{ padding: '36px 56px', borderBottom: '1px solid var(--border)', background: 'var(--panel-alt)' }}>
          <div style={{ maxWidth: 1180, margin: '0 auto' }}>
            <Eyebrow style={{ marginBottom: 18 }}>Also running</Eyebrow>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 24 }}>
              {otherActive.map((s, i) => (
                <a key={i} href="#" style={{
                  display: 'grid', gridTemplateColumns: 'auto 1fr auto', gap: 18, alignItems: 'center',
                  background: 'var(--panel)', border: '1px solid var(--border)', padding: '18px 20px',
                  color: 'var(--fg)', textDecoration: 'none',
                }}>
                  <PlayBadge size={42} />
                  <div>
                    <ContextLabel context={s.context} size={10} />
                    <div style={{ fontFamily: 'var(--font-heading)', fontSize: 20, fontWeight: 600, letterSpacing: '-0.012em', marginTop: 5 }}>{s.title}</div>
                    <div style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--muted)', marginTop: 4 }}>{s.scope} · {s.planned ? `part ${s.parts} of ~${s.planned}` : `${s.parts} parts`}</div>
                  </div>
                  <PIcon name="arrow-right" size={15} color="var(--muted)" />
                </a>
              ))}
            </div>
          </div>
        </section>

        {/* Archive index */}
        <section style={{ padding: '44px 56px 72px' }}>
          <div style={{ maxWidth: 1180, margin: '0 auto' }}>
            <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginBottom: 28 }}>
              <Headline size={30} style={{ fontWeight: 600 }}>The archive</Headline>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11.5, color: 'var(--muted)', letterSpacing: '0.12em' }}>{SERIES_DONE.length} COMPLETED SERIES</span>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 64 }}>
              {[colA, colB].map((col, ci) => (
                <div key={ci}>
                  {col.map(({ year, items }) => (
                    <div key={year} style={{ marginBottom: 30 }}>
                      <div style={{ fontFamily: 'var(--font-mono)', fontSize: 11, fontWeight: 600, letterSpacing: '0.18em', color: 'var(--muted)', paddingBottom: 10, borderBottom: '1px solid var(--border)', marginBottom: 2 }}>
                        {year} <span style={{ marginLeft: 8, opacity: 0.55, fontWeight: 400 }}>· {items.length}</span>
                      </div>
                      {items.map((s, i) => <ArchiveIndexRow key={i} s={s} />)}
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </section>
      </PublicFrame>
    </div>
  );
}


// ═════════════════════════════════════════════════════════════════════════
// 4 — The Timeline
// The preaching history as a vertical spine. Year markers anchor the rail;
// each series hangs off it as a block with a 12-month duration bar showing
// when it ran and for how long. Active series extend to "now." Reveals the
// rhythm of the church's teaching at a glance.
// ═════════════════════════════════════════════════════════════════════════
const TL_MONTHS = ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D'];

function DurationBar({ s }) {
  // Position the bar on a 12-month axis using the series' months within its
  // start year; series that span into later years run to the right edge.
  const left = (s.startM / 12) * 100;
  const endMonth = s.endY > s.startY ? 12 : s.endM + 1;
  const width = ((endMonth - s.startM) / 12) * 100;
  const active = s.status === 'active';
  return (
    <div style={{ position: 'relative', height: 34 }}>
      {/* Month gridlines */}
      <div style={{ position: 'absolute', inset: 0, display: 'grid', gridTemplateColumns: 'repeat(12, 1fr)' }}>
        {TL_MONTHS.map((m, i) => (
          <div key={i} style={{ borderLeft: '1px solid var(--border-soft)', display: 'flex', alignItems: 'flex-end', justifyContent: 'center', paddingBottom: 2 }}>
            <span style={{ fontFamily: 'var(--font-mono)', fontSize: 8.5, color: 'var(--muted)', opacity: 0.5 }}>{m}</span>
          </div>
        ))}
      </div>
      {/* Run bar */}
      <div style={{
        position: 'absolute', top: 6, height: 14, left: `${left}%`, width: `${width}%`,
        background: active ? 'var(--accent)' : 'color-mix(in oklab, var(--accent) 40%, var(--bg))',
        border: '1px solid ' + (active ? 'var(--accent)' : 'color-mix(in oklab, var(--accent) 55%, var(--border))'),
        display: 'flex', alignItems: 'center', paddingLeft: 7,
      }}>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 9.5, fontWeight: 600, color: active ? 'var(--accent-fg)' : 'var(--fg-soft)', whiteSpace: 'nowrap', letterSpacing: '0.04em' }}>
          {s.parts} pts
        </span>
      </div>
      {s.endY > s.startY && (
        <span style={{ position: 'absolute', right: 4, top: 9, fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--muted)' }}>→ {s.endY}</span>
      )}
    </div>
  );
}

function TimelineRow({ s }) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '40px 1fr 280px', gap: 20, alignItems: 'center', padding: '14px 0', borderBottom: '1px solid var(--border-soft)' }}>
      {/* Dot on the rail */}
      <div style={{ position: 'relative', height: '100%', display: 'flex', justifyContent: 'center' }}>
        <div style={{ position: 'absolute', top: 0, bottom: 0, width: 1, background: 'var(--border)' }} />
        <div style={{
          width: 13, height: 13, borderRadius: '50%', marginTop: 6,
          background: s.status === 'active' ? 'var(--accent)' : 'var(--bg)',
          border: '2px solid ' + (s.status === 'active' ? 'var(--accent)' : 'var(--muted)'),
          zIndex: 1,
        }} />
      </div>
      <a href="#" style={{ color: 'var(--fg)', textDecoration: 'none' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14, flexWrap: 'wrap' }}>
          <span style={{ fontFamily: 'var(--font-heading)', fontSize: 21, fontWeight: 600, letterSpacing: '-0.015em' }}>{s.title}</span>
          {s.status === 'active' && <Chip tone="accent">Live</Chip>}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginTop: 6 }}>
          <ContextLabel context={s.context} size={10} />
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--accent)' }}>{s.scope}</span>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--muted)' }}>{s.speaker}</span>
        </div>
      </a>
      <DurationBar s={s} />
    </div>
  );
}

function TimelineDirection() {
  // newest start first, grouped by year
  const years = seriesByYear(SERIES).sort((a, b) => b.year - a.year);
  return (
    <div data-direction="editorial" style={{ width: '100%', minHeight: '100%' }}>
      <PublicFrame theme="editorial">
        <SeriesPageHeader
          idea="4 · The Timeline"
          oneLine="The preaching history as a vertical spine. Year markers anchor the rail; each series carries a 12-month duration bar. Reveals the rhythm of the church's teaching at a glance."
        />
        <SeriesFilterBar />
        <section style={{ padding: '40px 56px 72px' }}>
          <div style={{ maxWidth: 1100, margin: '0 auto' }}>
            {years.map(({ year, items }) => (
              <div key={year} style={{ marginBottom: 12 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '40px 1fr', gap: 20, alignItems: 'center', padding: '20px 0 10px' }}>
                  <div style={{ position: 'relative', height: 48, display: 'flex', justifyContent: 'center' }}>
                    <div style={{ position: 'absolute', top: 0, bottom: 0, width: 1, background: 'var(--border)' }} />
                    <div style={{ width: 17, height: 17, borderRadius: '50%', background: 'var(--inset)', marginTop: 16, zIndex: 1 }} />
                  </div>
                  <div style={{ display: 'flex', alignItems: 'baseline', gap: 16 }}>
                    <span style={{ fontFamily: 'var(--font-heading)', fontSize: 56, fontWeight: 600, letterSpacing: '-0.03em', color: 'var(--fg)', lineHeight: 1 }}>{year}</span>
                    <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--muted)', letterSpacing: '0.14em', textTransform: 'uppercase' }}>{items.length} series begun</span>
                  </div>
                </div>
                {items.map((s, i) => <TimelineRow key={i} s={s} />)}
              </div>
            ))}
          </div>
        </section>
      </PublicFrame>
    </div>
  );
}


// ═════════════════════════════════════════════════════════════════════════
// 5 — By Track
// Three lanes for the parallel teaching tracks: Sunday Worning Worship,
// Bible Class, and Evening & Midweek. Each lane lists its series newest
// first, active series highlighted. Mirrors how the church actually
// teaches — three threads running side by side.
// ═════════════════════════════════════════════════════════════════════════
const TRACKS = [
  { name: 'AM Worship', sub: 'Sunday morning', match: ['AM Worship'] },
  { name: 'Bible Class', sub: 'Verse-by-verse study', match: ['AM Bible Class'] },
  { name: 'Evening & Midweek', sub: 'PM Worship · Wednesday', match: ['PM Worship', 'Wednesday Night'] },
];

function TrackSeriesRow({ s }) {
  const active = s.status === 'active';
  return (
    <a href="#" style={{
      display: 'block', padding: '18px 20px',
      borderBottom: '1px solid var(--border-soft)',
      background: active ? 'var(--accent-soft)' : 'transparent',
      color: 'var(--fg)', textDecoration: 'none',
    }}>
      <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 12 }}>
        <span style={{ fontFamily: 'var(--font-heading)', fontSize: 19, fontWeight: 600, letterSpacing: '-0.012em', textWrap: 'pretty' }}>{s.title}</span>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 13, fontWeight: 600, color: active ? 'var(--accent)' : 'var(--muted)', flexShrink: 0 }}>{s.parts}</span>
      </div>
      <div style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--accent)', marginTop: 6, letterSpacing: '0.04em' }}>{s.scope}</div>
      <div style={{ fontFamily: 'var(--font-mono)', fontSize: 10.5, color: 'var(--muted)', marginTop: 5, letterSpacing: '0.04em' }}>
        {active ? 'NOW PREACHING' : `${s.start} – ${s.end}`} · {s.speaker}
      </div>
      {active && <div style={{ marginTop: 12 }}><ActiveProgress s={s} height={5} /></div>}
    </a>
  );
}

function ByTrackDirection() {
  return (
    <div data-direction="editorial" style={{ width: '100%', minHeight: '100%' }}>
      <PublicFrame theme="editorial">
        <SeriesPageHeader
          idea="5 · By Track"
          oneLine="Three lanes for the parallel teaching tracks — Sunday morning, Bible Class, Evening & Midweek. Each lists its series newest first. Mirrors how the church actually teaches."
        />
        <SeriesFilterBar />
        <section style={{ padding: '40px 56px 72px' }}>
          <div style={{ maxWidth: 1180, margin: '0 auto', display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 28, alignItems: 'start' }}>
            {TRACKS.map((track, ti) => {
              const items = SERIES.filter(s => track.match.includes(s.context));
              const activeCount = items.filter(s => s.status === 'active').length;
              return (
                <div key={ti} style={{ border: '1px solid var(--border)', background: 'var(--panel)' }}>
                  <div style={{ padding: '22px 20px 18px', borderBottom: '2px solid var(--inset)', background: 'var(--panel-alt)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      <span style={{ width: 9, height: 9, borderRadius: '50%', background: SERIES_CTX_COLOR[track.match[0]], display: 'inline-block' }} />
                      <span style={{ fontFamily: 'var(--font-heading)', fontSize: 22, fontWeight: 600, letterSpacing: '-0.015em' }}>{track.name}</span>
                    </div>
                    <div style={{ fontFamily: 'var(--font-mono)', fontSize: 10.5, color: 'var(--muted)', marginTop: 8, letterSpacing: '0.12em', textTransform: 'uppercase' }}>
                      {track.sub} · {items.length} series{activeCount ? ` · ${activeCount} live` : ''}
                    </div>
                  </div>
                  {items.map((s, i) => <TrackSeriesRow key={i} s={s} />)}
                </div>
              );
            })}
          </div>
        </section>
      </PublicFrame>
    </div>
  );
}


Object.assign(window, {
  SeriesShelfDirection, TheIndexDirection, FeaturedArchiveDirection,
  TimelineDirection, ByTrackDirection,
});
