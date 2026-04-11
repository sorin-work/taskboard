import { linkify } from '../lib/linkify';

/** Plain text with line breaks kept and http(s) links made clickable. */
export function RichText({ text }: { text: string }) {
  return (
    <>
      {linkify(text).map((part, i) =>
        part.kind === 'link' ? (
          <a
            key={i}
            href={part.href}
            target="_blank"
            rel="noopener noreferrer"
            onClick={(e) => e.stopPropagation()}
          >
            {part.text}
          </a>
        ) : (
          <span key={i}>{part.text}</span>
        ),
      )}
    </>
  );
}
