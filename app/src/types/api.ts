/**
 * Typed surface for API DTOs. Hand-written to keep parity with the C# DTOs in
 * CredoCms.Application — the project does NOT use NSwag (a deliberate Phase 1
 * decision to avoid generation/versioning friction for a small API).
 */

export type Role = "Administrator" | "Editor" | "Member";

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
  createdAt: string;
  modifiedAt: string;
  modifiedByUserId: string | null;
  rowVersion: string;
}

export type UpdateSiteSettingsRequest = Omit<
  SiteSettings,
  "createdAt" | "modifiedAt" | "modifiedByUserId"
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
