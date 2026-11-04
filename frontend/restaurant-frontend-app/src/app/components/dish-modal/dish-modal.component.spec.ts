import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DishListResponse } from '@app/models/dishes/dish-list-response.model';

import { DishModalComponent } from './dish-modal.component';

describe('DishModalComponent', () => {
  let fixture: ComponentFixture<DishModalComponent>;

  const dish = {
    id: 'dish-1',
    name: 'Green Salad',
    imageUrl: 'https://test.api/salad.jpg',
    description: 'Fresh greens with olive oil.',
    calories: '210',
    proteins: '5 g',
    fats: '12',
    carbohydrates: '18 g',
    vitamins: 'Vitamin C',
    price: 12,
    weight: '250 g',
    state: 'Available',
    dishType: 'Appetizer',
  } as DishListResponse;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DishModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DishModalComponent);
    fixture.componentInstance.dish = dish;
    fixture.detectChanges();
  });

  it('shows the dish name, description and nutrition values', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('Green Salad');
    expect(text).toContain('Fresh greens with olive oil.');
    expect(text).toContain('210 kcal');
  });
});
