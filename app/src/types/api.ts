/**
 * Typed surface for API DTOs. Hand-written to keep parity with the C# DTOs in
 * CredoCms.Application — the project does NOT use NSwag (a deliberate Phase 1
 * decision to avoid generation/versioning friction for a small API).
 */

export type Role = "Administrator" | "Editor" | "Member";

// Phase 6 — analytics + cookie consent enums (top-level so they can be
// imported standalone by the consent banner / GA4 loader).
export type AnalyticsProvider = 0 | 1; // None | Ga4
export type ConsentBannerPosition = 0 | 1; // BottomRight | BottomFull

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  isActive: boolean;
  requirePasswordChangeOnFirstLogin: boolean;
  roles: Role[];
  expiresAtUtc: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResult {
  user: CurrentUser;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface AcceptInvitationRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface PublicSiteSettings {
  churchName: string;
  tagline: string | null;
  logoUrl: string | null;
  primaryColor: string;
  accentColor: string;
  contactEmail: string | null;
  contactPhone: string | null;
  contactAddress: string | null;
  facebookUrl: string | null;
  instagramUrl: string | null;
  youTubeUrl: string | null;
  xUrl: string | null;
  tikTokUrl: string | null;
  otherSocialLabel: string | null;
  otherSocialUrl: string | null;
  footerText: string | null;
  leadersPageLabel: string;
  homepageHeroCtaLabel: string;
  homepageHeroCtaLink: string;
  facebookLoginEnabled: boolean;
  // Phase 6 — analytics + cookie consent
  analyticsProvider: AnalyticsProvider;
  ga4MeasurementId: string | null;
  ga4ConsentBannerEnabled: boolean;
  ga4ConsentBannerPosition: ConsentBannerPosition;
  cookiePolicyPageSlug: string | null;
}

export interface SiteSettings extends PublicSiteSettings {
  defaultVersionRetentionCount: number;
  leaderCategoriesJson: string;
  documentCategoriesJson: string;
  maxDocumentSizeBytes: number;
  maxImageSizeBytes: number;
  imageMaxWidth: number;
  imageQuality: number;
  membersWelcomeText: string | null;
  defaultMetaDescription: string | null;
  // Phase 4 fields ---------------------------------------------------------
  getInvolvedPageLabel: string;
  classesPageLabel: string;
  classAudienceAgeGroupsJson: string;
  showRecentPastOnPublicClasses: boolean;
  recentPastClassesLookbackDays: number;
  blogCategoriesJson: string;
  blogPageLabel: string;
  profanityWordlist: string | null;
  profanityAllowlist: string | null;
  prayerRequestArchiveDays: number;
  prayerRequestRequireApproval: boolean;
  connectCardInterestsJson: string;
  connectCardAcknowledgmentMessageJson: string | null;
  connectCardPageLabel: string;
  cloudflareTurnstileSiteKey: string | null;
  cloudflareTurnstileSecretKey: string | null;
  facebookOAuthAppId: string | null;
  facebookOAuthAppSecret: string | null;
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
  rowVersion: string;
  // Phase 6 — admin-side carries the page-id; the public DTO resolves the
  // slug. Both coexist on this type; UpdateSiteSettingsRequest omits the
  // resolved slug so admin saves go through with the id.
  cookiePolicyPageId: string | null;
}

export type UpdateSiteSettingsRequest = Omit<
  SiteSettings,
  "createdAt" | "modifiedAt" | "modifiedByUserId" | "cookiePolicyPageSlug"
>;

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  isActive: boolean;
  emailConfirmed: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  roles: Role[];
}

export interface UserDetail extends Omit<UserListItem, "roles"> {
  lockoutEnabled: boolean;
  lockoutEndUtc: string | null;
  roles: Role[];
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  roles: Role[];
  sendInvitation: boolean;
}

export interface UpdateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  roles: Role[];
  isActive: boolean;
}

export interface HardDeleteUserRequest {
  confirmDisplayName: string;
}

export interface PageListItem {
  id: string;
  slug: string;
  title: string;
  excerpt: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  isSystemPage: boolean;
  modifiedAt: string;
  modifiedByUserId: string | null;
}

export interface PageDetail {
  id: string;
  slug: string;
  title: string;
  bodyJson: string;
  excerpt: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  metaDescription: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  isDeleted: boolean;
  isSystemPage: boolean;
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
  publishedAt: string | null;
  deletedAt: string | null;
}

export interface PublicPage {
  id: string;
  slug: string;
  title: string;
  bodyJson: string;
  excerpt: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  metaDescription: string | null;
  isMembersOnly: boolean;
  publishedAt: string;
}

export interface CreatePageRequest {
  slug: string;
  title: string;
  bodyJson: string;
  excerpt: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  metaDescription: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
}

export type UpdatePageRequest = CreatePageRequest;

export interface NewsListItem {
  id: string;
  slug: string;
  title: string;
  excerpt: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  publishedAt: string | null;
  expiresAt: string | null;
  modifiedAt: string;
}

export interface NewsDetail {
  id: string;
  slug: string;
  title: string;
  bodyJson: string;
  excerpt: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  metaDescription: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  isDeleted: boolean;
  expiresAt: string | null;
  calendarDate: string | null;
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
  publishedAt: string | null;
  deletedAt: string | null;
}

export interface PublicNewsItem {
  id: string;
  slug: string;
  title: string;
  excerpt: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  isMembersOnly: boolean;
  publishedAt: string;
  calendarDate: string | null;
}

export interface PublicNewsDetail extends Omit<PublicNewsItem, "excerpt"> {
  bodyJson: string;
  excerpt: string | null;
  metaDescription: string | null;
}

export interface CreateNewsItemRequest {
  slug: string;
  title: string;
  bodyJson: string;
  excerpt: string | null;
  heroImageUrl: string | null;
  heroImageWebpUrl: string | null;
  heroImageAlt: string | null;
  metaDescription: string | null;
  isPublished: boolean;
  isMembersOnly: boolean;
  expiresAt: string | null;
  calendarDate: string | null;
}

export type UpdateNewsItemRequest = CreateNewsItemRequest;

export type DayOfWeek =
  | "Sunday"
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday"
  | "Friday"
  | "Saturday";

export interface ServiceTime {
  id: string;
  name: string;
  dayOfWeek: DayOfWeek;
  startTime: string; // "HH:mm:ss"
  endTime: string | null;
  location: string | null;
  notes: string | null;
  displayOrder: number;
  isActive: boolean;
  isDeleted: boolean;
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
}

export interface PublicServiceTime {
  name: string;
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string | null;
  location: string | null;
  notes: string | null;
  displayOrder: number;
}

export interface CreateServiceTimeRequest {
  name: string;
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string | null;
  location: string | null;
  notes: string | null;
  displayOrder: number;
  isActive: boolean;
}

export type UpdateServiceTimeRequest = CreateServiceTimeRequest;

export interface Leader {
  id: string;
  fullName: string;
  title: string | null;
  category: string;
  bioJson: string | null;
  email: string | null;
  photoUrl: string | null;
  photoWebpUrl: string | null;
  photoAlt: string | null;
  displayOrder: number;
  createdAt: string;
  modifiedAt: string;
}

export interface PublicLeader {
  id: string;
  fullName: string;
  title: string | null;
  category: string;
  bioJson: string | null;
  photoUrl: string | null;
  photoWebpUrl: string | null;
  photoAlt: string | null;
  displayOrder: number;
}

export interface CreateLeaderRequest {
  fullName: string;
  title: string | null;
  category: string;
  bioJson: string | null;
  email: string | null;
  photoUrl: string | null;
  photoWebpUrl: string | null;
  photoAlt: string | null;
  displayOrder: number;
}

export type UpdateLeaderRequest = CreateLeaderRequest;

export interface DocumentDto {
  id: string;
  title: string;
  description: string | null;
  category: string;
  blobUrl: string;
  originalFilename: string | null;
  sizeBytes: number;
  isPublished: boolean;
  isMembersOnly: boolean;
  isDeleted: boolean;
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
}

export interface PublicDocument {
  id: string;
  title: string;
  description: string | null;
  category: string;
  sizeBytes: number;
  isMembersOnly: boolean;
  modifiedAt: string;
}

export interface UpdateDocumentMetadataRequest {
  title: string;
  description: string | null;
  category: string;
  isPublished: boolean;
  isMembersOnly: boolean;
}

export type AnnouncementSeverity = 0 | 1 | 2; // Info | Warning | Critical

export interface AnnouncementBanner {
  isActive: boolean;
  severity: AnnouncementSeverity;
  message: string;
  linkUrl: string | null;
  linkLabel: string | null;
  startsAt: string | null;
  endsAt: string | null;
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
}

export interface PublicAnnouncementBanner {
  severity: AnnouncementSeverity;
  message: string;
  linkUrl: string | null;
  linkLabel: string | null;
}

export interface UpdateAnnouncementBannerRequest {
  isActive: boolean;
  severity: AnnouncementSeverity;
  message: string;
  linkUrl: string | null;
  linkLabel: string | null;
  startsAt: string | null;
  endsAt: string | null;
}

export interface AuditLogEntry {
  id: string;
  timestamp: string;
  userId: string | null;
  userDisplayNameSnapshot: string;
  action: string;
  entityType: string;
  entityId: string | null;
  detailsJson: string | null;
  ipAddress: string | null;
}

export interface ApiErrorResponse {
  errors: string[];
}
