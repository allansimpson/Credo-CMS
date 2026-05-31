import { useEffect, useState } from "react";
import { Info, Mail, Phone, MapPin, Image as ImageIcon, User } from "lucide-react";
import {
  profileApi,
  type Profile,
  type UpdateDirectoryRequest,
  type UpdateNotificationsRequest,
  type UpdatePersonalInfoRequest,
} from "@/lib/api/profile";
import { ImageUpload } from "@/components/shared/ImageUpload";
import { TipTapEditor } from "@/components/shared/TipTapEditor";
import {
  Banner,
  Content,
  InlineError,
  PageHead,
  Panel,
  Skeleton,
} from "@/components/members/portal-primitives";

/**
 * Member self-edit profile. Name is read-only (admin-managed).
 * Directory: master toggle gates 4 per-field share toggles. Notifications:
 * 4 prefs (News, Blog, Broadcast, Group).
 */
export function ProfilePage() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(false);
    profileApi.get()
      .then((p) => { if (!cancelled) setProfile(p); })
      .catch(() => { if (!cancelled) setError(true); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 2200);
    return () => clearTimeout(t);
  }, [toast]);

  const flash = (msg: string) => setToast(msg);

  if (loading) {
    return (
      <Content>
        <PageHead title="My Profile" />
        <Skeleton className="mb-3 h-20 w-full" />
        <Skeleton className="h-32 w-full" />
      </Content>
    );
  }

  if (error || !profile) {
    return (
      <Content>
        <PageHead title="My Profile" />
        <InlineError onRetry={() => location.reload()} />
      </Content>
    );
  }

  return (
    <Content>
      <PageHead title="My Profile" />

      {toast && (
        <div className="mb-4">
          <Banner tone="success" icon={<Info strokeWidth={1.75} className="h-4 w-4" />}>
            {toast}
          </Banner>
        </div>
      )}

      <IdentitySection profile={profile} />
      <PersonalSection profile={profile} onSaved={(p) => { setProfile(p); flash("Saved."); }} />
      <DirectorySection profile={profile} onSaved={(p) => { setProfile(p); flash("Directory preferences updated."); }} />
      <NotificationsSection profile={profile} onSaved={(p) => { setProfile(p); flash("Notification preferences updated."); }} />
    </Content>
  );
}

// ────── Identity (read-only) ──────

function IdentitySection({ profile }: { profile: Profile }) {
  return (
    <section className="mb-6">
      <h2 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
        Identity
      </h2>
      <Panel>
        <div className="flex flex-col gap-1">
          <p className="font-heading text-lg font-semibold">{profile.displayName}</p>
          <p className="text-sm text-muted">{profile.email}</p>
          <p className="mt-2 inline-flex items-center gap-2 font-mono text-[10.5px] uppercase tracking-[0.12em] text-muted">
            <Info strokeWidth={1.5} className="h-3.5 w-3.5" />
            Name and email are managed by an administrator.
          </p>
        </div>
      </Panel>
    </section>
  );
}

// ────── Personal info (editable) ──────

function PersonalSection({ profile, onSaved }: { profile: Profile; onSaved: (p: Profile) => void }) {
  const [phone, setPhone] = useState(profile.phoneNumber ?? "");
  const [addr1, setAddr1] = useState(profile.addressLine1 ?? "");
  const [addr2, setAddr2] = useState(profile.addressLine2 ?? "");
  const [city, setCity] = useState(profile.city ?? "");
  const [state, setState] = useState(profile.stateOrRegion ?? "");
  const [postal, setPostal] = useState(profile.postalCode ?? "");
  const [country, setCountry] = useState(profile.country ?? "");
  const [photo, setPhoto] = useState({
    url: profile.photoBlobUrl,
    webpUrl: profile.photoWebpBlobUrl,
    alt: profile.photoAltText,
  });
  const [bio, setBio] = useState<string | null>(profile.publicAuthorBio);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    try {
      const req: UpdatePersonalInfoRequest = {
        phoneNumber: phone.trim() || null,
        addressLine1: addr1.trim() || null,
        addressLine2: addr2.trim() || null,
        city: city.trim() || null,
        stateOrRegion: state.trim() || null,
        postalCode: postal.trim() || null,
        country: country.trim() || null,
        photoBlobUrl: photo.url,
        photoWebpBlobUrl: photo.webpUrl,
        photoAltText: photo.alt,
        publicAuthorBio: bio,
      };
      const updated = await profileApi.updatePersonal(req);
      onSaved(updated);
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Couldn't save. Try again."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="mb-6">
      <h2 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
        Personal info
      </h2>
      <form onSubmit={handleSubmit}>
        <Panel className="space-y-4">
          {errors.length > 0 && <InlineError message={errors[0]} />}

          {/* Photo */}
          <div>
            <label className="mb-1.5 block text-sm font-medium">Profile photo</label>
            <ImageUpload
              ariaLabel="Profile photo"
              value={photo}
              onChange={(next) => setPhoto({ url: next.url, webpUrl: next.webpUrl, alt: next.alt })}
            />
          </div>

          {/* Bio */}
          <div>
            <label className="mb-1.5 block text-sm font-medium">Short bio</label>
            <TipTapEditor
              ariaLabel="Public author bio"
              valueJson={bio}
              onChangeJson={setBio}
              placeholder="Tell other members a little about yourself…"
            />
            <p className="mt-1 text-xs text-muted">
              Visible on your directory profile when you're listed.
            </p>
          </div>

          {/* Phone */}
          <Field label="Phone">
            <input
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              maxLength={50}
              className="profile-input"
            />
          </Field>

          {/* Address */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Address line 1">
              <input value={addr1} onChange={(e) => setAddr1(e.target.value)} maxLength={200} className="profile-input" />
            </Field>
            <Field label="Address line 2">
              <input value={addr2} onChange={(e) => setAddr2(e.target.value)} maxLength={200} className="profile-input" />
            </Field>
            <Field label="City">
              <input value={city} onChange={(e) => setCity(e.target.value)} maxLength={100} className="profile-input" />
            </Field>
            <Field label="State / Region">
              <input value={state} onChange={(e) => setState(e.target.value)} maxLength={100} className="profile-input" />
            </Field>
            <Field label="Postal code">
              <input value={postal} onChange={(e) => setPostal(e.target.value)} maxLength={20} className="profile-input" />
            </Field>
            <Field label="Country">
              <input value={country} onChange={(e) => setCountry(e.target.value)} maxLength={100} className="profile-input" />
            </Field>
          </div>

          <div>
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex items-center bg-accent px-4 py-2 text-sm font-semibold text-accent-foreground hover:bg-accent/90 disabled:opacity-50"
            >
              {submitting ? "Saving…" : "Save changes"}
            </button>
          </div>
        </Panel>
      </form>
      <style>{`
        .profile-input {
          height: 2.5rem;
          width: 100%;
          border: 1px solid hsl(var(--border));
          background: hsl(var(--panel));
          padding: 0 0.75rem;
          font-size: 0.875rem;
        }
        .profile-input:focus-visible {
          border-color: hsl(var(--accent));
          outline: none;
        }
      `}</style>
    </section>
  );
}

// ────── Directory (master toggle gating per-field) ──────

function DirectorySection({ profile, onSaved }: { profile: Profile; onSaved: (p: Profile) => void }) {
  const [listed, setListed] = useState(profile.isListedInDirectory);
  const [showEmail, setShowEmail] = useState(profile.showEmailInDirectory);
  const [showPhone, setShowPhone] = useState(profile.showPhoneInDirectory);
  const [showAddress, setShowAddress] = useState(profile.showAddressInDirectory);
  const [showPhoto, setShowPhoto] = useState(profile.showPhotoInDirectory);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);
    try {
      const req: UpdateDirectoryRequest = {
        isListedInDirectory: listed,
        showEmailInDirectory: showEmail,
        showPhoneInDirectory: showPhone,
        showAddressInDirectory: showAddress,
        showPhotoInDirectory: showPhoto,
      };
      const updated = await profileApi.updateDirectory(req);
      onSaved(updated);
      // Server logical-ANDs per-field with master — re-sync from the response.
      setShowEmail(updated.showEmailInDirectory);
      setShowPhone(updated.showPhoneInDirectory);
      setShowAddress(updated.showAddressInDirectory);
      setShowPhoto(updated.showPhotoInDirectory);
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Couldn't save. Try again."];
      setErrors(messages);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="mb-6">
      <h2 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
        Directory
      </h2>
      <form onSubmit={handleSubmit}>
        <Panel className="space-y-4">
          {errors.length > 0 && <InlineError message={errors[0]} />}

          {/* Master toggle */}
          <label className="flex items-start gap-3 border border-accent/40 bg-accent/[0.07] p-3">
            <input
              type="checkbox"
              checked={listed}
              onChange={(e) => setListed(e.target.checked)}
              className="mt-1"
            />
            <span>
              <span className="block font-semibold">List me in the member directory</span>
              <span className="block text-xs text-muted">
                Other signed-in members will see your name and any contact details you choose to share below.
              </span>
            </span>
          </label>

          {/* Name row (locked) */}
          <div className="flex items-center justify-between border-b border-border-soft py-2.5">
            <span className="inline-flex items-center gap-2 text-sm">
              <User strokeWidth={1.5} className="h-4 w-4 text-muted" /> Name
            </span>
            <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-muted">
              Always shared
            </span>
          </div>

          {/* Per-field toggles, gated by master */}
          <FieldToggle
            icon={<Mail strokeWidth={1.5} className="h-4 w-4" />}
            label="Email"
            disabled={!listed}
            checked={showEmail}
            onChange={setShowEmail}
          />
          <FieldToggle
            icon={<Phone strokeWidth={1.5} className="h-4 w-4" />}
            label="Phone"
            disabled={!listed}
            checked={showPhone}
            onChange={setShowPhone}
          />
          <FieldToggle
            icon={<MapPin strokeWidth={1.5} className="h-4 w-4" />}
            label="Address"
            disabled={!listed}
            checked={showAddress}
            onChange={setShowAddress}
          />
          <FieldToggle
            icon={<ImageIcon strokeWidth={1.5} className="h-4 w-4" />}
            label="Profile photo"
            disabled={!listed}
            checked={showPhoto}
            onChange={setShowPhoto}
          />

          <div>
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex items-center bg-accent px-4 py-2 text-sm font-semibold text-accent-foreground hover:bg-accent/90 disabled:opacity-50"
            >
              {submitting ? "Saving…" : "Save directory preferences"}
            </button>
          </div>
        </Panel>
      </form>
    </section>
  );
}

function FieldToggle({
  icon,
  label,
  checked,
  disabled,
  onChange,
}: {
  icon: React.ReactNode;
  label: string;
  checked: boolean;
  disabled: boolean;
  onChange: (next: boolean) => void;
}) {
  return (
    <label
      className={`flex items-center justify-between border-b border-border-soft py-2.5 text-sm ${
        disabled ? "opacity-50" : ""
      }`}
    >
      <span className="inline-flex items-center gap-2">
        <span className="text-muted">{icon}</span>
        {label}
      </span>
      <input
        type="checkbox"
        checked={checked && !disabled}
        disabled={disabled}
        onChange={(e) => onChange(e.target.checked)}
      />
    </label>
  );
}

// ────── Notifications (4 prefs) ──────

function NotificationsSection({ profile, onSaved }: { profile: Profile; onSaved: (p: Profile) => void }) {
  const [news, setNews] = useState(profile.receiveNewsEmails);
  const [blog, setBlog] = useState(profile.receiveBlogEmails);
  const [broadcast, setBroadcast] = useState(profile.receiveBroadcastEmails);
  const [group, setGroup] = useState(profile.receiveGroupEmailsGlobal);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const req: UpdateNotificationsRequest = {
        receiveNewsEmails: news,
        receiveBlogEmails: blog,
        receiveBroadcastEmails: broadcast,
        receiveGroupEmailsGlobal: group,
      };
      const updated = await profileApi.updateNotifications(req);
      onSaved(updated);
    } catch {
      /* swallowed — user can retry */
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section>
      <h2 className="mb-3 font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-muted">
        Email notifications
      </h2>
      <form onSubmit={handleSubmit}>
        <Panel className="space-y-3">
          <PrefToggle label="News from the church" body="Pastoral letters, stories, announcements." checked={news} onChange={setNews} />
          <PrefToggle label="Blog posts" body="Devotionals and longer-form writing." checked={blog} onChange={setBlog} />
          <PrefToggle label="Broadcast emails" body="Important all-member updates." checked={broadcast} onChange={setBroadcast} />
          <PrefToggle label="Group emails" body="Messages from groups you're in." checked={group} onChange={setGroup} />
          <div>
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex items-center bg-accent px-4 py-2 text-sm font-semibold text-accent-foreground hover:bg-accent/90 disabled:opacity-50"
            >
              {submitting ? "Saving…" : "Save notification preferences"}
            </button>
          </div>
        </Panel>
      </form>
    </section>
  );
}

function PrefToggle({
  label,
  body,
  checked,
  onChange,
}: {
  label: string;
  body: string;
  checked: boolean;
  onChange: (next: boolean) => void;
}) {
  return (
    <label className="flex items-start gap-3 border-b border-border-soft py-2.5 last:border-b-0">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="mt-1"
      />
      <span className="flex-1">
        <span className="block text-sm font-medium">{label}</span>
        <span className="block text-xs text-muted">{body}</span>
      </span>
    </label>
  );
}

// Local Field wrapper for the personal form.
function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-sm font-medium">{label}</span>
      {children}
    </label>
  );
}
