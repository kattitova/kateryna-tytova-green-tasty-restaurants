/** Data for the reservation details row: address, date, time range and guests. */
export interface ReservationDetails {
  address: string;
  startTime?: Date | string;
  endTime?: Date | string;
  guestCount?: number;
}
