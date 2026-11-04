import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DishListResponse } from '@app/models/dishes/dish-list-response.model';

import { DishSelectCardComponent } from './dish-select-card.component';

describe('DishSelectCardComponent', () => {
  let fixture: ComponentFixture<DishSelectCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DishSelectCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DishSelectCardComponent);
    fixture.componentInstance.dish = {
      id: 'dish-1',
      name: 'Truffle Mushroom Risotto',
      imageUrl: 'https://test.api/risotto.jpg',
      description: 'Creamy risotto with truffle oil.',
      price: 18,
    } as DishListResponse;
    fixture.componentInstance.quantity = 2;
    fixture.detectChanges();
  });

  it('shows the dish name, description and quantity', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.dish-name')?.textContent).toContain('Truffle Mushroom Risotto');
    expect(element.textContent).toContain('Creamy risotto with truffle oil.');
    expect(element.querySelector('.value')?.textContent?.trim()).toBe('2');
  });
});
