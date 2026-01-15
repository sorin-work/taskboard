import { initials } from '../lib/initials';

const palette = ['#2f5bd3', '#0e9384', '#c4320a', '#7a5af8', '#dd2590', '#4e5ba6', '#b54708'];

function colorFor(id: string): string {
  let hash = 0;
  for (const char of id) {
    hash = (hash * 31 + char.charCodeAt(0)) | 0;
  }
  return palette[Math.abs(hash) % palette.length]!;
}

export function Avatar({ id, name, size = 26 }: { id: string; name: string; size?: number }) {
  return (
    <span
      className="avatar"
      title={name}
      aria-label={name}
      style={{ background: colorFor(id), width: size, height: size, fontSize: size * 0.42 }}
    >
      {initials(name)}
    </span>
  );
}
