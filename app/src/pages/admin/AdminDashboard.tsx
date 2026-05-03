import { Link } from "react-router-dom";
import { Users, ScrollText, Settings } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";

export function AdminDashboard() {
  const { user } = useAuth();

  return (
    <div>
      <h1 className="text-2xl font-bold">Welcome back, {user?.firstName}</h1>
      <p className="mt-2 text-muted-foreground">
        This is your admin dashboard. Phase 1 ships the foundational shell. Content
        types arrive in subsequent phases.
      </p>

      <div className="mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Card to="/admin/users" icon={Users} title="Users" description="Invite, edit, and manage staff and members." />
        <Card to="/admin/audit-log" icon={ScrollText} title="Audit Log" description="See who did what across the system." />
        <Card to="/admin/settings" icon={Settings} title="Site Settings" description="Branding, contact details, social links." />
      </div>
    </div>
  );
}

interface CardProps {
  to: string;
  icon: React.ComponentType<{ className?: string }>;
  title: string;
  description: string;
}

function Card({ to, icon: Icon, title, description }: CardProps) {
  return (
    <Link
      to={to}
      className="block rounded-lg border bg-card p-5 shadow-sm transition-colors hover:bg-muted"
    >
      <Icon className="h-6 w-6 text-accent" />
      <div className="mt-3 font-semibold">{title}</div>
      <p className="mt-1 text-sm text-muted-foreground">{description}</p>
    </Link>
  );
}
