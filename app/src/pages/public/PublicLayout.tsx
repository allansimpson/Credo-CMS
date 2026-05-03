import { Outlet } from "react-router-dom";
import { ChurchThemeLayout } from "@/themes/ChurchThemeLayout";
import { PublicNavBar } from "@/components/shared/PublicNavBar";
import { PublicFooter } from "@/components/shared/PublicFooter";

export function PublicLayout() {
  return (
    <ChurchThemeLayout>
      <div className="flex min-h-screen flex-col">
        <PublicNavBar />
        <div className="flex-1">
          <Outlet />
        </div>
        <PublicFooter />
      </div>
    </ChurchThemeLayout>
  );
}
