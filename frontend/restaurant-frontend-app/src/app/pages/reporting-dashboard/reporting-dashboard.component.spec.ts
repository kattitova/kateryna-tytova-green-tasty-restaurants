import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ReportingDashboardComponent } from './reporting-dashboard.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

describe('ReportingDashboardComponent', () => {
  let component: ReportingDashboardComponent;
  let fixture: ComponentFixture<ReportingDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
      ],
      imports: [ReportingDashboardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReportingDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
