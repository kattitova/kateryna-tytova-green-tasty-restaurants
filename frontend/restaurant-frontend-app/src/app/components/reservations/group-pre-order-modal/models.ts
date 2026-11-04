export interface BreakdownRow {
  label: string;
  /** Formatted amount, for example "$9.50", "−$2.85" or "—". Null: the row has no amount. */
  amount: string | null;
  tone: 'muted' | 'success';
}
