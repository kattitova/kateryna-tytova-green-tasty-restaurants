import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '../services/toast/toast.service';
import { AuthenticationService } from './services/authentication.service';
import { SKIP_ERROR_TOAST } from './skip-error-toast.context';
import { tokenInterceptor } from './token.interceptor';

const URL = 'https://test.api/reservations';

describe('tokenInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toastService: jasmine.SpyObj<ToastService>;
  let authService: jasmine.SpyObj<AuthenticationService>;

  beforeEach(() => {
    toastService = jasmine.createSpyObj<ToastService>('ToastService', ['error']);
    authService = jasmine.createSpyObj<AuthenticationService>('AuthenticationService', [
      'getTokens',
    ]);
    authService.getTokens.and.returnValue(null);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([tokenInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toastService },
        { provide: AuthenticationService, useValue: authService },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows the server message in a toast for a 400 response', () => {
    http.post(URL, {}).subscribe({ error: () => undefined });

    httpMock
      .expectOne(URL)
      .flush({ message: 'Overlaps' }, { status: 400, statusText: 'Bad Request' });

    expect(toastService.error).toHaveBeenCalledWith('Overlaps', 'Error');
  });

  it('does not show a toast but still rethrows when SKIP_ERROR_TOAST is set', () => {
    let receivedStatus: number | undefined;
    http
      .post(URL, {}, { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
      .subscribe({ error: (error) => (receivedStatus = error.status) });

    httpMock
      .expectOne(URL)
      .flush({ message: 'Overlaps' }, { status: 400, statusText: 'Bad Request' });

    expect(toastService.error).not.toHaveBeenCalled();
    expect(receivedStatus).toBe(400);
  });

  describe('owner and guest routes', () => {
    beforeEach(() => {
      authService.getTokens.and.returnValue({ idToken: 'id-1' } as ReturnType<AuthenticationService['getTokens']>);
    });

    it('sends the Cognito token to the owner group routes under /guest/groups/', () => {
      http.get('https://test.api/guest/groups/g1/sessions').subscribe();

      const req = httpMock.expectOne('https://test.api/guest/groups/g1/sessions');
      expect(req.request.headers.get('Authorization')).toBe('Bearer id-1');
      req.flush({});
    });

    it('keeps the guest token on guest session routes', () => {
      http
        .post(
          'https://test.api/guest/sessions/s1/dishes',
          {},
          { headers: { Authorization: 'Bearer guest-token' } }
        )
        .subscribe();

      const req = httpMock.expectOne('https://test.api/guest/sessions/s1/dishes');
      expect(req.request.headers.get('Authorization')).toBe('Bearer guest-token');
      req.flush({});
    });

    it('adds no Authorization header to guest invite routes', () => {
      http.get('https://test.api/guest/invites/t1').subscribe();

      const req = httpMock.expectOne('https://test.api/guest/invites/t1');
      expect(req.request.headers.has('Authorization')).toBeFalse();
      req.flush({});
    });
  });
});
