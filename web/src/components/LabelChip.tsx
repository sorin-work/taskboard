import type { Label } from '../api/types';

export function LabelChip({ label, compact }: { label: Label; compact?: boolean }) {
  return (
    <span
      className={`label-chip label-${label.color}${compact ? ' compact' : ''}`}
      title={label.name}
    >
      {compact ? null : label.name}
    </span>
  );
}
