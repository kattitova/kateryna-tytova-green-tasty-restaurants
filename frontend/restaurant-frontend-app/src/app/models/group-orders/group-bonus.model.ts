export type GroupBonusState = 'InProgress' | 'Achieved' | 'Missed';
export type GroupBonusTier = 'TenPercentDiscount' | 'FreeDessert';

/** Group bonus that the backend computes. `deadline` is an ISO UTC string. */
export interface GroupBonus {
  state: GroupBonusState;
  tier: GroupBonusTier | null;
  discountPercent: number;
  deadline: string | null;
}
