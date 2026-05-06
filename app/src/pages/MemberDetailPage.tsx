import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Mail, MapPin, Phone, Users } from "lucide-react";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { TipTapReadOnly } from "@/components/shared/TipTapReadOnly";
import { membersApi, type MemberDetail } from "@/lib/api/members";

export function MemberDetailPage() {
  const { userId } = useParams<{ userId: string }>();
  const [member, setMember] = useState<MemberDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!userId) return;
    let cancelled = false;
    setLoading(true);
    membersApi.get(userId)
      .then((m) => { if (!cancelled) setMember(m); })
      .catch((err) => {
        if (cancelled) return;
        // Server returns 404 both for "no such user" and "not opted into
        // directory". Treat both as the same "no member here" message.
        const status = (err as { status?: number }).status;
        if (status === 404) setNotFound(true);
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [userId]);

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-10">
          <Link
            to="/members"
            className="mb-6 inline-flex items-center gap-1 text-sm text-muted hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" /> Back to directory
          </Link>

          {loading && <p className="text-muted">Loading…</p>}
          {!loading && notFound && (
            <div className="rounded-lg border bg-card p-6">
              <h1 className="text-xl font-bold">Member not found</h1>
              <p className="mt-2 text-sm text-muted">
                This member isn't currently listed in the directory.
              </p>
            </div>
          )}
          {!loading && member && (
            <article className="space-y-8">
              <header className="flex flex-wrap items-start gap-6 border-b pb-6">
                {member.photoBlobUrl ? (
                  <picture>
                    {member.photoWebpBlobUrl && (
                      <source srcSet={member.photoWebpBlobUrl} type="image/webp" />
                    )}
                    <img
                      src={member.photoBlobUrl}
                      alt={member.photoAltText ?? ""}
                      className="h-28 w-28 object-cover"
                    />
                  </picture>
                ) : (
                  <span
                    aria-hidden
                    className="grid h-28 w-28 place-items-center bg-panel-alt text-3xl font-bold text-fg-soft"
                  >
                    {member.firstName.slice(0, 1)}{member.lastName.slice(0, 1)}
                  </span>
                )}
                <div className="min-w-0 flex-1">
                  <h1 className="text-3xl font-bold leading-tight">{member.displayName}</h1>
                  {member.email && (
                    <a
                      href={`mailto:${member.email}`}
                      className="mt-3 inline-flex h-10 items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
                    >
                      <Mail className="h-4 w-4" /> Send email
                    </a>
                  )}
                </div>
              </header>

              {(member.email || member.phoneNumber || hasAddress(member)) && (
                <section className="rounded-lg border bg-card p-6">
                  <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
                    Contact
                  </h2>
                  <dl className="mt-3 space-y-3 text-sm">
                    {member.email && (
                      <ContactRow icon={<Mail className="h-4 w-4" />} label="Email">
                        <a href={`mailto:${member.email}`} className="text-primary hover:underline">
                          {member.email}
                        </a>
                      </ContactRow>
                    )}
                    {member.phoneNumber && (
                      <ContactRow icon={<Phone className="h-4 w-4" />} label="Phone">
                        <a href={`tel:${member.phoneNumber}`} className="text-primary hover:underline">
                          {member.phoneNumber}
                        </a>
                      </ContactRow>
                    )}
                    {hasAddress(member) && (
                      <ContactRow icon={<MapPin className="h-4 w-4" />} label="Address">
                        <span>{formatAddress(member)}</span>
                      </ContactRow>
                    )}
                  </dl>
                </section>
              )}

              {member.publicAuthorBio && (
                <section className="rounded-lg border bg-card p-6">
                  <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
                    About
                  </h2>
                  <div className="prose prose-sm mt-3 max-w-none">
                    <TipTapReadOnly json={member.publicAuthorBio} />
                  </div>
                </section>
              )}

              {member.groupMemberships.length > 0 && (
                <section className="rounded-lg border bg-card p-6">
                  <h2 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-muted">
                    <Users className="h-4 w-4" /> Groups
                  </h2>
                  <ul className="mt-3 space-y-1.5 text-sm">
                    {member.groupMemberships.map((g) => (
                      <li key={g.groupId} className="flex items-center gap-2">
                        <Link to={`/groups/${g.groupSlug}`} className="text-primary hover:underline">
                          {g.groupName}
                        </Link>
                        {g.isLeader && (
                          <span className="rounded bg-accent/15 px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider text-accent">
                            Leader
                          </span>
                        )}
                      </li>
                    ))}
                  </ul>
                </section>
              )}
            </article>
          )}
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

function ContactRow({
  icon, label, children,
}: { icon: React.ReactNode; label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start gap-3">
      <span aria-hidden className="mt-0.5 text-muted">{icon}</span>
      <div>
        <dt className="text-xs uppercase tracking-wide text-muted">{label}</dt>
        <dd className="mt-0.5">{children}</dd>
      </div>
    </div>
  );
}

function hasAddress(m: MemberDetail): boolean {
  return Boolean(m.addressLine1 || m.city || m.stateOrRegion || m.postalCode || m.country);
}

function formatAddress(m: MemberDetail): string {
  return [m.addressLine1, m.addressLine2, m.city, m.stateOrRegion, m.postalCode, m.country]
    .filter(Boolean)
    .join(", ");
}
