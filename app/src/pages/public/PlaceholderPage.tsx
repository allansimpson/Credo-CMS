interface PlaceholderPageProps {
  title: string;
  body?: string;
}

export function PlaceholderPage({ title, body }: PlaceholderPageProps) {
  return (
    <main className="mx-auto max-w-3xl px-4 py-16 text-center">
      <h1 className="text-3xl font-bold tracking-tight">{title}</h1>
      <p className="mt-4 text-muted-foreground">
        {body ?? "Content for this page will be added in a future phase."}
      </p>
    </main>
  );
}
