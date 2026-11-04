import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';

import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { DishesService } from '@app/services/dishes/dishes.service';
import { FeedbacksService } from '@app/services/feedbacks/feedbacks.service';
import { LocationsService } from '@app/services/locations/locations.service';
import { ReservationService } from '@app/services/reservation/reservation.service';
import { UsersService } from '@app/services/users/users.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

import { MainPageComponent } from './main-page.component';

describe('MainPageComponent popular dishes', () => {
  let fixture: ComponentFixture<MainPageComponent>;
  let router: Router;

  const makeDish = (index: number): DishListResponse => ({
    id: `dish-${index}`,
    name: `Dish name ${index}`,
    imageUrl: `https://test.api/dish-${index}.jpg`,
    price: 10 + index,
    state: 'Available',
    weight: '250 g',
    dishType: 'Appetizer',
    description: `Description of dish ${index}`,
    calories: '100',
    proteins: '5',
    carbohydrates: '10',
    fats: '3',
    vitamins: 'C',
  });

  const render = async (popularDishes$: Observable<DishListResponse[]>): Promise<HTMLElement> => {
    const dishesService = jasmine.createSpyObj<DishesService>('DishesService', [
      'getPopularDishes',
    ]);
    dishesService.getPopularDishes.and.returnValue(popularDishes$);
    const locationsService = jasmine.createSpyObj<LocationsService>('LocationsService', [
      'getLocations',
    ]);
    locationsService.getLocations.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [MainPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: ActivatedRoute, useValue: { queryParams: of({}) } },
      ],
    })
      .overrideComponent(MainPageComponent, {
        set: {
          providers: [
            { provide: DishesService, useValue: dishesService },
            { provide: LocationsService, useValue: locationsService },
            {
              provide: ReservationService,
              useValue: jasmine.createSpyObj<ReservationService>('ReservationService', ['getReservationById']),
            },
            {
              provide: UsersService,
              useValue: jasmine.createSpyObj<UsersService>('UsersService', ['getUserName']),
            },
            {
              provide: FeedbacksService,
              useValue: jasmine.createSpyObj<FeedbacksService>('FeedbacksService', [
                'getFeedbackByReservation',
              ]),
            },
          ],
        },
      })
      .compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(MainPageComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };

  const cards = (host: HTMLElement): HTMLElement[] =>
    Array.from(
      host.querySelectorAll<HTMLElement>('.popular-dishes-section .dish-cards .dish-card')
    );

  const modal = (host: HTMLElement): HTMLElement | null => host.querySelector('app-dish-modal');

  const press = (card: HTMLElement, key: string): KeyboardEvent => {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    card.dispatchEvent(event);
    fixture.detectChanges();
    return event;
  };

  describe('with loaded dishes', () => {
    let host: HTMLElement;

    beforeEach(async () => {
      host = await render(of([1, 2, 3, 4, 5].map(makeDish)));
    });

    // red before
    it('renders each popular dish (max 4) as a focusable role=button without href', () => {
      expect(cards(host).length).toBe(4);
      for (const card of cards(host)) {
        expect(card.getAttribute('role')).toBe('button');
        expect(card.getAttribute('tabindex')).toBe('0');
        expect(card.hasAttribute('href')).toBeFalse();
        expect(card.tagName).not.toBe('A');
      }
    });

    // pin
    it('shows no dish modal before an interaction', () => {
      expect(modal(host)).toBeNull();
    });

    // red before
    it('opens the modal with the clicked dish on click', () => {
      cards(host)[1].click();
      fixture.detectChanges();

      const text = modal(host)?.textContent ?? '';
      expect(modal(host)).not.toBeNull();
      expect(text).toContain('Dish name 2');
      expect(text).toContain('Description of dish 2');
      expect(text).not.toContain('Dish name 1');
    });

    // red before
    it('opens the modal on Enter', () => {
      press(cards(host)[2], 'Enter');

      expect(modal(host)?.textContent).toContain('Description of dish 3');
    });

    // red before
    it('opens the modal on Space', () => {
      press(cards(host)[0], ' ');

      expect(modal(host)?.textContent).toContain('Description of dish 1');
    });

    // red before
    it('prevents the page scroll on Space', () => {
      const event = press(cards(host)[0], ' ');

      expect(event.defaultPrevented).toBeTrue();
    });

    // pin: catches a handler that opens on any key or prevents default for every key
    it('ignores keys other than Enter and Space and does not prevent their default', () => {
      for (const key of ['a', 'Tab', 'ArrowDown']) {
        const event = press(cards(host)[0], key);

        expect(event.defaultPrevented).withContext(key).toBeFalse();
        expect(modal(host)).withContext(key).toBeNull();
      }
    });

    // red before
    it('closes the modal with the close button', () => {
      cards(host)[0].click();
      fixture.detectChanges();
      expect(modal(host)).not.toBeNull();

      modal(host)!.querySelector<HTMLElement>('.close-button')!.click();
      fixture.detectChanges();

      expect(modal(host)).toBeNull();
    });

    // red before
    it('does not navigate on a card click', () => {
      const navigateByUrl = spyOn(router, 'navigateByUrl').and.resolveTo(true);
      const navigate = spyOn(router, 'navigate').and.resolveTo(true);

      cards(host)[0].click();
      fixture.detectChanges();

      expect(navigateByUrl).not.toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
    });
  });

  describe('while loading', () => {
    // pin (guards decision 4: the new pointer/focus styles must not hit skeletons)
    it('renders skeleton cards that are not interactive', async () => {
      const host = await render(new Subject<DishListResponse[]>());
      const skeletons = Array.from(
        host.querySelectorAll<HTMLElement>('.popular-dishes-section .skeleton-card')
      );

      expect(skeletons.length).toBe(4);
      for (const skeleton of skeletons) {
        expect(skeleton.hasAttribute('role')).toBeFalse();
        expect(skeleton.hasAttribute('tabindex')).toBeFalse();
        expect(getComputedStyle(skeleton).cursor).not.toBe('pointer');
      }
    });
  });

  describe('when loading fails', () => {
    // pin
    it('shows no cards and no modal', async () => {
      const host = await render(throwError(() => new Error('boom')));

      expect(cards(host).length).toBe(0);
      expect(modal(host)).toBeNull();
    });
  });
});
