import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GroupOrdersService } from './group-orders.service';

const BASE = 'https://test.api';

describe('GroupOrdersService', () => {
  let service: GroupOrdersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: BASE },
      ],
    });
    service = TestBed.inject(GroupOrdersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getOwnerGroupId$ strips the "owner-" prefix from the owner session id', () => {
    let result: string | undefined;
    service.getOwnerGroupId$('r1').subscribe((id) => (result = id));

    const req = httpMock.expectOne(`${BASE}/reservations/r1/guest-session-id`);
    expect(req.request.method).toBe('GET');
    req.flush({ guestSessionId: 'owner-g1' });

    expect(result).toBe('g1');
  });

  it('getOwnerGroupId$ errors when the session id has no "owner-" prefix', () => {
    let error: Error | undefined;
    service.getOwnerGroupId$('r1').subscribe({ error: (e) => (error = e) });

    httpMock.expectOne(`${BASE}/reservations/r1/guest-session-id`).flush({ guestSessionId: 'g1' });

    expect(error?.message).toBe('Unexpected owner session id');
  });

  it('removeGuest$ sends DELETE to the guest session of the group', () => {
    service.removeGuest$('g1', 'guest-anna').subscribe();

    const req = httpMock.expectOne(`${BASE}/guest/groups/g1/sessions/guest-anna`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ participants: [], total: 0 });
  });

  it('submit$ sends POST with the reservation id in the body', () => {
    service.submit$('g1', 'r1').subscribe();

    const req = httpMock.expectOne(`${BASE}/guest/groups/g1/submit`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reservationId: 'r1' });
    req.flush([]);
  });
});
