export type TextPart =
  | { kind: 'text'; text: string }
  | { kind: 'link'; text: string; href: string };

const URL_PATTERN = /\bhttps?:\/\/[^\s<>"]+/gi;
// Punctuation that usually ends a sentence rather than belonging to the url.
const TRAILING = /[).,;:!?'"\]]+$/;

/** Splits text into plain parts and http(s) links. Nothing else is interpreted. */
export function linkify(text: string): TextPart[] {
  const parts: TextPart[] = [];
  let last = 0;
  for (const match of text.matchAll(URL_PATTERN)) {
    const trailing = match[0].match(TRAILING)?.[0] ?? '';
    let url = match[0].slice(0, match[0].length - trailing.length);
    // Give back closing parens that balance an opening one, like Wikipedia links.
    const opens = (url.match(/\(/g) ?? []).length;
    let closes = (url.match(/\)/g) ?? []).length;
    for (const char of trailing) {
      if (char !== ')' || closes >= opens) {
        break;
      }
      url += ')';
      closes++;
    }
    const start = match.index;
    if (start > last) {
      parts.push({ kind: 'text', text: text.slice(last, start) });
    }
    parts.push({ kind: 'link', text: url, href: url });
    last = start + url.length;
  }
  if (last < text.length) {
    parts.push({ kind: 'text', text: text.slice(last) });
  }
  return parts;
}
