interface Props {
  error: unknown;
}

export default function ErrorBanner({ error }: Props) {
  if (!error) return null;
  const message = error instanceof Error ? error.message : String(error);
  return (
    <div className="error-banner" role="alert">
      {message}
    </div>
  );
}