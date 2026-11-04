import { buildPreOrderQueryParams, formatPreOrderLabel, PreOrderTarget } from './pre-order-navigation';

describe('pre-order navigation', () => {
  const target: PreOrderTarget = {
    id: 'res-1',
    locationAddress: '9 Abashidze Street',
    tableNumber: 3,
    startTime: new Date(2026, 10, 8, 16, 45, 30),
    endTime: new Date(2026, 10, 8, 18, 15, 0),
  };

  describe('formatPreOrderLabel', () => {
    it('shows the table number and the time without seconds', () => {
      expect(formatPreOrderLabel(target)).toBe(
        '9 Abashidze Street, Table 3, Sun Nov 08 2026, 16:45 - 18:15'
      );
    });

    it('accepts ISO strings for the times', () => {
      const label = formatPreOrderLabel({
        ...target,
        startTime: new Date(2026, 10, 8, 9, 5).toISOString(),
        endTime: new Date(2026, 10, 8, 10, 35).toISOString(),
      });

      expect(label).toBe('9 Abashidze Street, Table 3, Sun Nov 08 2026, 09:05 - 10:35');
    });

    it('uses the table name when the table number is missing', () => {
      expect(formatPreOrderLabel({ ...target, tableNumber: null, tableName: 'Table 12' })).toBe(
        '9 Abashidze Street, Table 12, Sun Nov 08 2026, 16:45 - 18:15'
      );
    });

    it('shows only the address and the table when a time is missing', () => {
      expect(formatPreOrderLabel({ ...target, endTime: undefined })).toBe('9 Abashidze Street, Table 3');
    });

    it('shows the start date only for a reservation that crosses midnight', () => {
      const label = formatPreOrderLabel({
        ...target,
        startTime: new Date(2026, 10, 8, 23, 30),
        endTime: new Date(2026, 10, 9, 1, 0),
      });

      expect(label).toBe('9 Abashidze Street, Table 3, Sun Nov 08 2026, 23:30 - 01:00');
    });

    it('omits the table and never shows the table ID when no table data is known', () => {
      const label = formatPreOrderLabel({ ...target, tableNumber: undefined });

      expect(label).toBe('9 Abashidze Street, Sun Nov 08 2026, 16:45 - 18:15');
    });
  });

  describe('buildPreOrderQueryParams', () => {
    it('enables the pre-order mode for the reservation', () => {
      expect(buildPreOrderQueryParams(target)).toEqual({
        preOrder: 'true',
        reservationId: 'res-1',
        reservationLabel: '9 Abashidze Street, Table 3, Sun Nov 08 2026, 16:45 - 18:15',
      });
    });
  });
});
