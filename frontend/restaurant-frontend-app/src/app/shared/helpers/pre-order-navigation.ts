import { formatDate } from '@angular/common';
import { Params } from '@angular/router';

/** The reservation data that the menu needs to open in pre-order mode. */
export interface PreOrderTarget {
  id: string;
  locationAddress: string;
  startTime?: Date | string;
  endTime?: Date | string;
  tableNumber?: number | null;
  tableName?: string;
}

const TIME_FORMAT = 'HH:mm';
const LOCALE = 'en-US';

/** Label for the menu banner, e.g. "9 Abashidze Street, Table 3, Sun Nov 08 2026, 16:45 - 18:15". */
export function formatPreOrderLabel(target: PreOrderTarget): string {
  const parts = [target.locationAddress, tableLabel(target)];

  if (target.startTime && target.endTime) {
    parts.push(
      new Date(target.startTime).toDateString(),
      `${formatDate(target.startTime, TIME_FORMAT, LOCALE)} - ${formatDate(target.endTime, TIME_FORMAT, LOCALE)}`
    );
  }

  return parts.filter((part): part is string => !!part).join(', ');
}

/** Query params that open `/menu` in pre-order mode for one reservation. */
export function buildPreOrderQueryParams(target: PreOrderTarget): Params {
  return {
    preOrder: 'true',
    reservationId: target.id,
    reservationLabel: formatPreOrderLabel(target),
  };
}

// The table ID is an internal UUID. Never show it to the user.
function tableLabel(target: PreOrderTarget): string | undefined {
  if (target.tableNumber != null) {
    return `Table ${target.tableNumber}`;
  }

  return target.tableName || undefined;
}
