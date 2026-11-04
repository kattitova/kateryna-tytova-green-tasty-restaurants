import { OrderItem } from '@app/models/dishes/order-item.model';
import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { buildOrderBill, groupBonusLabel } from './order-bill.helpers';

function item(price: number, quantity: number, state: number | undefined = 0): OrderItem {
  return { dish: { id: `d-${price}`, price } as unknown as DishListResponse, quantity, state: state as number };
}

describe('buildOrderBill', () => {
  const fourDishes = [item(34, 4)];

  it('applies a 10 percent discount to the subtotal', () => {
    expect(buildOrderBill(fourDishes, 10)).toEqual({
      subtotal: 136,
      discountPercent: 10,
      discountAmount: 13.6,
      finalTotal: 122.4,
    });
  });

  [0, null, undefined, -5].forEach((percent) => {
    it(`gives no discount for percent ${percent}`, () => {
      expect(buildOrderBill(fourDishes, percent)).toEqual({
        subtotal: 136,
        discountPercent: 0,
        discountAmount: 0,
        finalTotal: 136,
      });
    });
  });

  it('returns zeros for an empty list', () => {
    expect(buildOrderBill([], 10)).toEqual({ subtotal: 0, discountPercent: 10, discountAmount: 0, finalTotal: 0 });
  });

  it('excludes canceled items and counts items without a state', () => {
    const bill = buildOrderBill([item(10, 1, 0), item(20, 1, 1), item(100, 1, 2), item(5, 2, undefined)], 0);

    expect(bill.subtotal).toBe(40);
    expect(bill.finalTotal).toBe(40);
  });

  it('rounds money values to 2 decimals', () => {
    expect(buildOrderBill([item(19.99, 3)], 10)).toEqual({
      subtotal: 59.97,
      discountPercent: 10,
      discountAmount: 6,
      finalTotal: 53.97,
    });
  });
});

describe('groupBonusLabel', () => {
  it('shows the percent for a discount tier', () => {
    expect(groupBonusLabel('TenPercentDiscount', 10)).toBe('Group bonus: −10%');
    expect(groupBonusLabel('TenPercentDiscount', 15)).toBe('Group bonus: −15%');
  });

  it('gives no label for a discount tier without a positive percent', () => {
    expect(groupBonusLabel('TenPercentDiscount', 0)).toBeNull();
    expect(groupBonusLabel('TenPercentDiscount', undefined)).toBeNull();
    expect(groupBonusLabel('TenPercentDiscount', null)).toBeNull();
  });

  it('shows "Free dessert" for the dessert tier, whatever the percent', () => {
    expect(groupBonusLabel('FreeDessert', 0)).toBe('Free dessert');
    expect(groupBonusLabel('FreeDessert', 10)).toBe('Free dessert');
  });

  it('gives no label without a tier', () => {
    expect(groupBonusLabel(null, 10)).toBeNull();
    expect(groupBonusLabel(undefined, undefined)).toBeNull();
  });
});
