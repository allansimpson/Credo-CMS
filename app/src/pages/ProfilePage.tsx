import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";
import { useAuth } from "@/hooks/useAuth";
import { authApi } from "@/lib/api/auth";
import { profileApi, type Profile } from "@/lib/api/profile";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapEditor } from "@/components/shared/TipTapEditor";

const TABS = [
  { id: "personal", label: "Personal Info" },
  { id: "directory", label: "Directory" },
  { id: "notifications", label: "Notifications" },
  { id: "account", label: "Account" },
] as const;

type TabId = typeof TABS[number]["id"];

export function ProfilePage() {
  const { user } = useAuth();
  const [active, setActive] = useState<TabId>("personal");
  const [profile, setProfile] = useState<Profile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    profileApi.get()
      .then((p) => { setProfile(p); setLoading(false); })
      .catch(() => setLoading(false));
  }, []);

  if (!user) return null;

  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <main className="mx-auto w-full max-w-4xl flex-1 px-4 py-10">
          {/* Header — name + email + photo preview */}
          <header className="flex flex-wrap items-center gap-4 border-b pb-6">
            {profile?.photoBlobUrl ? (
              <picture>
                {profile.photoWebpBlobUrl && <source srcSet={profile.photoWebpBlobUrl} type="image/webp" />}
                <img
                  src={profile.photoBlobUrl}
                  alt={profile.photoAltText ?? ""}
                  className="h-16 w-16 object-cover"
                />
              </picture>
            ) : (
              <span
                aria-hidden
                className="grid h-16 w-16 place-items-center bg-accent text-2xl font-bold text-accent-foreground"
              >
                {user.firstName.slice(0, 1).toUpperCase()}{user.lastName.slice(0, 1).toUpperCase()}
              </span>
            )}
            <div>
              <h1 className="text-2xl font-bold">{user.displayName}</h1>
              <p className="text-sm text-muted">{user.email}</p>
              {user.roles.length > 0 && (
                <p className="mt-1 text-xs text-muted">{user.roles.join(" · ")}</p>
              )}
            </div>
          </header>

          {/* Tab bar — horizontally scrollable on mobile */}
          <nav className="mt-6 overflow-x-auto border-b" aria-label="Profile sections">
            <div className="flex min-w-max gap-1">
              {TABS.map((t) => (
                <button
                  key={t.id}
                  type="button"
                  onClick={() => setActive(t.id)}
                  className={
                    "whitespace-nowrap px-4 py-3 text-sm transition-colors " +
                    (active === t.id
                      ? "border-b-2 border-accent text-foreground font-semibold"
                      : "text-muted hover:text-foreground")
                  }
                >
                  {t.label}
                </button>
              ))}
            </div>
          </nav>

          <div className="mt-6">
            {loading && <p className="text-muted">Loading profile…</p>}
            {!loading && !profile && <p className="text-danger">Could not load profile.</p>}
            {!loading && profile && (
              <>
                {active === "personal" && (
                  <PersonalTab profile={profile} onUpdated={setProfile} />
                )}
                {active === "directory" && (
                  <DirectoryTab profile={profile} onUpdated={setProfile} />
                )}
                {active === "notifications" && (
                  <NotificationsTab profile={profile} onUpdated={setProfile} />
                )}
                {active === "account" && <AccountTab />}
              </>
            )}
          </div>
        </main>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}

// ---- shared ----------------------------------------------------------------

function FormBanner({ errors, success }: { errors: string[]; success: boolean }) {
  return (
    <>
      {errors.length > 0 && (
        <div role="alert" className="rounded-md border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
          <ul className="list-disc pl-5">{errors.map((err) => <li key={err}>{err}</li>)}</ul>
        </div>
      )}
      {success && (
        <div role="status" className="rounded-md border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-800">
          Saved.
        </div>
      )}
    </>
  );
}

interface TabPropsBase {
  profile: Profile;
  onUpdated: (next: Profile) => void;
}

// ---- Personal tab ----------------------------------------------------------

function PersonalTab({ profile, onUpdated }: TabPropsBase) {
  const [form, setForm] = useState({
    phoneNumber: profile.phoneNumber ?? "",
    addressLine1: profile.addressLine1 ?? "",
    addressLine2: profile.addressLine2 ?? "",
    city: profile.city ?? "",
    stateOrRegion: profile.stateOrRegion ?? "",
    postalCode: profile.postalCode ?? "",
    country: profile.country ?? "",
    photoBlobUrl: profile.photoBlobUrl,
    photoWebpBlobUrl: profile.photoWebpBlobUrl,
    photoAltText: profile.photoAltText ?? "",
    publicAuthorBio: profile.publicAuthorBio,
  });
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);
    try {
      const updated = await profileApi.updatePersonal({
        phoneNumber: form.phoneNumber || null,
        addressLine1: form.addressLine1 || null,
        addressLine2: form.addressLine2 || null,
        city: form.city || null,
        stateOrRegion: form.stateOrRegion || null,
        postalCode: form.postalCode || null,
        country: form.country || null,
        photoBlobUrl: form.photoBlobUrl,
        photoWebpBlobUrl: form.photoWebpBlobUrl,
        photoAltText: form.photoAltText || null,
        publicAuthorBio: form.publicAuthorBio,
      });
      onUpdated(updated);
      setSuccess(true);
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Save failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} success={success} />

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Read-only</legend>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <span className="block text-xs uppercase tracking-wide text-muted">Name</span>
            <span className="mt-1 block text-sm">{profile.firstName} {profile.lastName}</span>
            <span className="mt-1 block text-xs text-muted">Contact an administrator to change your name.</span>
          </div>
          <div>
            <span className="block text-xs uppercase tracking-wide text-muted">Email</span>
            <span className="mt-1 block text-sm">{profile.email}</span>
            <span className="mt-1 block text-xs text-muted">Email changes go through admin.</span>
          </div>
        </div>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Contact</legend>
        <Field label="Phone">
          <input
            type="tel"
            value={form.phoneNumber}
            maxLength={50}
            onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
            className="input"
          />
        </Field>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Address</legend>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Field label="Address line 1">
            <input value={form.addressLine1} maxLength={200} onChange={(e) => setForm({ ...form, addressLine1: e.target.value })} className="input" />
          </Field>
          <Field label="Address line 2">
            <input value={form.addressLine2} maxLength={200} onChange={(e) => setForm({ ...form, addressLine2: e.target.value })} className="input" />
          </Field>
          <Field label="City">
            <input value={form.city} maxLength={100} onChange={(e) => setForm({ ...form, city: e.target.value })} className="input" />
          </Field>
          <Field label="State / region">
            <input value={form.stateOrRegion} maxLength={100} onChange={(e) => setForm({ ...form, stateOrRegion: e.target.value })} className="input" />
          </Field>
          <Field label="Postal code">
            <input value={form.postalCode} maxLength={20} onChange={(e) => setForm({ ...form, postalCode: e.target.value })} className="input" />
          </Field>
          <Field label="Country">
            <input value={form.country} maxLength={100} onChange={(e) => setForm({ ...form, country: e.target.value })} className="input" />
          </Field>
        </div>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Photo</legend>
        <ImageUpload
          ariaLabel="Profile photo"
          hint="Square images render best. Alt text is required when a photo is set."
          value={{
            url: form.photoBlobUrl,
            webpUrl: form.photoWebpBlobUrl,
            alt: form.photoAltText || null,
          }}
          onChange={(next) => setForm({
            ...form,
            photoBlobUrl: next.url,
            photoWebpBlobUrl: next.webpUrl,
            photoAltText: next.alt ?? "",
          })}
        />
        {form.photoBlobUrl && (
          <Field label="Photo alt text" hint="Required. Describe the photo briefly for screen readers.">
            <input
              required
              value={form.photoAltText}
              maxLength={500}
              onChange={(e) => setForm({ ...form, photoAltText: e.target.value })}
              className="input"
            />
          </Field>
        )}
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Public author bio</legend>
        <p className="text-xs text-muted">
          Optional. Surfaces on the blog author archive when you have at least one
          published post. Plain prose works best.
        </p>
        <TipTapEditor
          ariaLabel="Public author bio"
          valueJson={form.publicAuthorBio}
          onChangeJson={(json) => setForm({ ...form, publicAuthorBio: json })}
          placeholder="A sentence or two for readers…"
        />
      </fieldset>

      <Submit submitting={submitting} />
      <Styles />
    </form>
  );
}

// ---- Directory tab ---------------------------------------------------------

function DirectoryTab({ profile, onUpdated }: TabPropsBase) {
  const [form, setForm] = useState({
    isListedInDirectory: profile.isListedInDirectory,
    showEmailInDirectory: profile.showEmailInDirectory,
    showPhoneInDirectory: profile.showPhoneInDirectory,
    showAddressInDirectory: profile.showAddressInDirectory,
    showPhotoInDirectory: profile.showPhotoInDirectory,
  });
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const setMaster = (next: boolean) => {
    if (!next) {
      // Visual mirror of the server-side rule: master off → all sub-toggles off.
      setForm({
        isListedInDirectory: false,
        showEmailInDirectory: false,
        showPhoneInDirectory: false,
        showAddressInDirectory: false,
        showPhotoInDirectory: false,
      });
    } else {
      setForm({ ...form, isListedInDirectory: true });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setErrors([]); setSuccess(false);
    try {
      const updated = await profileApi.updateDirectory(form);
      onUpdated(updated);
      setSuccess(true);
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Save failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  const subs: Array<{ key: keyof typeof form; label: string; visible: boolean }> = [
    { key: "showEmailInDirectory", label: "Show my email", visible: !!profile.email },
    { key: "showPhoneInDirectory", label: "Show my phone", visible: !!profile.phoneNumber },
    { key: "showAddressInDirectory", label: "Show my address", visible: !!profile.city || !!profile.addressLine1 },
    { key: "showPhotoInDirectory", label: "Show my photo", visible: !!profile.photoBlobUrl },
  ];

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} success={success} />

      <fieldset className="space-y-4 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Master toggle</legend>
        <label className="flex items-start gap-3 text-sm">
          <input
            type="checkbox"
            checked={form.isListedInDirectory}
            onChange={(e) => setMaster(e.target.checked)}
            className="mt-1"
          />
          <span>
            <span className="block font-medium">Include me in the members directory</span>
            <span className="mt-1 block text-xs text-muted">
              Members can browse the directory once signed in. Each field below has its own toggle.
            </span>
          </span>
        </label>
      </fieldset>

      {form.isListedInDirectory && (
        <fieldset className="space-y-3 rounded-lg border bg-card p-4">
          <legend className="px-2 text-sm font-semibold">What other members see</legend>
          {subs.map((s) => (
            <label key={s.key} className="flex items-start gap-3 text-sm">
              <input
                type="checkbox"
                checked={form[s.key] as boolean}
                onChange={(e) => setForm({ ...form, [s.key]: e.target.checked })}
                disabled={!s.visible}
                className="mt-1"
              />
              <span>
                <span className={s.visible ? "block" : "block text-muted"}>{s.label}</span>
                {!s.visible && (
                  <span className="mt-1 block text-xs text-muted">
                    No value on file — add it on the Personal Info tab to enable.
                  </span>
                )}
              </span>
            </label>
          ))}
        </fieldset>
      )}

      {form.isListedInDirectory && (
        <fieldset className="space-y-2 rounded-lg border bg-panel-alt p-4">
          <legend className="px-2 text-sm font-semibold">Preview</legend>
          <p className="text-xs text-muted">Other signed-in members will see:</p>
          <div className="rounded-md bg-card p-4 text-sm">
            <p className="font-semibold">{profile.firstName} {profile.lastName}</p>
            {form.showEmailInDirectory && profile.email && <p>{profile.email}</p>}
            {form.showPhoneInDirectory && profile.phoneNumber && <p>{profile.phoneNumber}</p>}
            {form.showAddressInDirectory && (profile.city || profile.addressLine1) && (
              <p>
                {[profile.addressLine1, profile.city, profile.stateOrRegion, profile.postalCode]
                  .filter(Boolean).join(", ")}
              </p>
            )}
          </div>
        </fieldset>
      )}

      <Submit submitting={submitting} />
      <Styles />
    </form>
  );
}

// ---- Notifications tab -----------------------------------------------------

function NotificationsTab({ profile, onUpdated }: TabPropsBase) {
  const [form, setForm] = useState({
    receiveNewsEmails: profile.receiveNewsEmails,
    receiveBlogEmails: profile.receiveBlogEmails,
    receiveBroadcastEmails: profile.receiveBroadcastEmails,
    receiveGroupEmailsGlobal: profile.receiveGroupEmailsGlobal,
  });
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true); setErrors([]); setSuccess(false);
    try {
      const updated = await profileApi.updateNotifications(form);
      onUpdated(updated);
      setSuccess(true);
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Save failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <FormBanner errors={errors} success={success} />

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Email notifications</legend>
        <Toggle
          checked={form.receiveNewsEmails}
          onChange={(v) => setForm({ ...form, receiveNewsEmails: v })}
          label="Church news"
          hint="New news posts from staff. Sent at most weekly."
        />
        <Toggle
          checked={form.receiveBlogEmails}
          onChange={(v) => setForm({ ...form, receiveBlogEmails: v })}
          label="Blog posts"
          hint="Devotionals, sermon notes, missions updates, pastor's reflections."
        />
        <Toggle
          checked={form.receiveBroadcastEmails}
          onChange={(v) => setForm({ ...form, receiveBroadcastEmails: v })}
          label="Broadcast messages"
          hint="Important announcements that go to the whole congregation."
        />
        <Toggle
          checked={form.receiveGroupEmailsGlobal}
          onChange={(v) => setForm({ ...form, receiveGroupEmailsGlobal: v })}
          label="Group emails"
          hint="Messages from groups you're a member of. Master toggle; per-group overrides land alongside Groups in Phase 4."
        />
      </fieldset>

      {form.receiveGroupEmailsGlobal && (
        <fieldset className="space-y-2 rounded-lg border border-dashed bg-panel-alt p-4">
          <legend className="px-2 text-sm font-semibold">Per-group overrides</legend>
          <p className="text-xs text-muted">
            Per-group email overrides will appear here once Groups land. For now, the
            global toggle above applies to every group you join.
          </p>
        </fieldset>
      )}

      <Submit submitting={submitting} />
      <Styles />
    </form>
  );
}

// ---- Account tab -----------------------------------------------------------

function AccountTab() {
  const { user, refresh } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [errors, setErrors] = useState<string[]>([]);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  async function handlePasswordChange(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    setSuccess(false);
    try {
      await authApi.changePassword({ currentPassword, newPassword });
      setCurrentPassword("");
      setNewPassword("");
      setSuccess(true);
      await refresh();
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Password change failed."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  }

  if (!user) return null;

  return (
    <div className="space-y-6">
      <FacebookConnectedAccount />


      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Calendar feed</legend>
        <p className="text-xs text-muted">
          Subscribe to your upcoming events from any calendar app — including
          members-only events.
        </p>
        <Link
          to="/profile/calendar-feed"
          className="inline-flex h-9 items-center justify-center border bg-card px-3 text-sm hover:bg-panel-alt"
        >
          Manage feed URL →
        </Link>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">My event registrations</legend>
        <Link
          to="/profile/registrations"
          className="inline-flex h-9 items-center justify-center border bg-card px-3 text-sm hover:bg-panel-alt"
        >
          View registrations →
        </Link>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">My groups</legend>
        <p className="text-xs text-muted">
          Group memberships will appear here once Groups land in Phase 4.
        </p>
        <Link
          to="/profile/groups"
          className="inline-flex h-9 items-center justify-center border bg-card px-3 text-sm hover:bg-panel-alt"
        >
          View groups →
        </Link>
      </fieldset>

      <fieldset className="space-y-3 rounded-lg border bg-card p-4">
        <legend className="px-2 text-sm font-semibold">Change password</legend>
        <form onSubmit={handlePasswordChange} className="space-y-4">
          <FormBanner errors={errors} success={success} />
          <Field label="Current password">
            <input
              type="password"
              required
              autoComplete="current-password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              className="input"
            />
          </Field>
          <Field label="New password" hint="At least 12 characters.">
            <input
              type="password"
              required
              minLength={12}
              autoComplete="new-password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className="input"
            />
          </Field>
          <button
            type="submit"
            disabled={submitting}
            className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {submitting ? "Saving…" : "Update password"}
          </button>
        </form>
      </fieldset>
      <Styles />
    </div>
  );
}

// ---- bits ------------------------------------------------------------------

function Field({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted">{hint}</span>}
    </label>
  );
}

// ---- Facebook account linking (Q15) -----------------------------------

function FacebookConnectedAccount() {
  const [isLinked, setIsLinked] = useState<boolean | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    fetch("/api/profile/facebook-status", { credentials: "include" })
      .then((r) => (r.ok ? r.json() : null))
      .then((d) => setIsLinked(d?.isLinked ?? false))
      .catch(() => setIsLinked(false));
  }, []);

  const unlink = async () => {
    if (!window.confirm("Unlink Facebook from your account?")) return;
    setSubmitting(true);
    try {
      await fetch("/api/auth/facebook/unlink", {
        method: "POST",
        credentials: "include",
      });
      setIsLinked(false);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <fieldset className="space-y-3 rounded-lg border bg-card p-4">
      <legend className="px-2 text-sm font-semibold">Connected accounts</legend>
      <p className="text-xs text-muted">
        Linking Facebook lets you sign in with the "Continue with Facebook" button
        on the login page. We never create new accounts from Facebook — only the
        link is stored.
      </p>
      {isLinked === null ? (
        <p className="text-xs text-muted">Checking…</p>
      ) : isLinked ? (
        <div className="flex items-center gap-3">
          <span className="rounded bg-success/15 px-2 py-0.5 text-[11px] font-medium uppercase tracking-wider text-success">
            Linked
          </span>
          <button
            type="button"
            onClick={unlink}
            disabled={submitting}
            className="inline-flex h-9 items-center justify-center rounded-md border border-danger/30 bg-card px-3 text-sm text-danger hover:bg-danger/10 disabled:opacity-50"
          >
            Unlink Facebook
          </button>
        </div>
      ) : (
        <a
          href="/api/auth/facebook/link-challenge?returnUrl=/profile"
          className="inline-flex h-9 items-center justify-center rounded-md border bg-card px-3 text-sm hover:bg-panel-alt"
        >
          Link Facebook account
        </a>
      )}
    </fieldset>
  );
}

function Toggle({
  checked, onChange, label, hint,
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  label: string;
  hint?: string;
}) {
  return (
    <label className="flex items-start gap-3 text-sm">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="mt-1"
      />
      <span>
        <span className="block font-medium">{label}</span>
        {hint && <span className="mt-1 block text-xs text-muted">{hint}</span>}
      </span>
    </label>
  );
}

function Submit({ submitting }: { submitting: boolean }) {
  return (
    <button
      type="submit"
      disabled={submitting}
      className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
    >
      {submitting ? "Saving…" : "Save changes"}
    </button>
  );
}

function Styles() {
  return (
    <style>{`
      .input {
        height: 2.5rem;
        width: 100%;
        border-radius: 0.375rem;
        border: 1px solid hsl(var(--input));
        background: hsl(var(--background));
        padding: 0 0.75rem;
        font-size: 0.875rem;
      }
    `}</style>
  );
}
