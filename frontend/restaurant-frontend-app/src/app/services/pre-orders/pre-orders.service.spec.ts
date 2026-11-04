import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

import { PreOrdersService } from './pre-orders.service';

describe('PreOrdersService', () => {
  let service: PreOrdersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
      ],
    });

    service = TestBed.inject(PreOrdersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets the orders and shows the error toast by default', () => {
    service.getPreOrders().subscribe();

    const req = httpMock.expectOne('https://test.api/orders');
    expect(req.request.method).toBe('GET');
    expect(req.request.context.get(SKIP_ERROR_TOAST)).toBeFalse();
    req.flush([]);
  });

  it('skips the error toast when asked', () => {
    service.getPreOrders(true).subscribe();

    const req = httpMock.expectOne('https://test.api/orders');
    expect(req.request.context.get(SKIP_ERROR_TOAST)).toBeTrue();
    req.flush([]);
  });
});
