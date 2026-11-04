import { Component, OnInit, ChangeDetectionStrategy, inject, HostListener } from '@angular/core';
import { finalize } from 'rxjs';
import { DishCardComponent } from '@app/shared/custom-components/dish-card/dish-card.component';
import { DishesService } from '@app/services/dishes/dishes.service';
import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { MainCoverImageComponent } from '@app/shared/custom-components/main-cover-image/main-cover-image.component';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { DishSort } from '@app/models/enums/dish-sort.model';
import { DishType } from '@app/models/enums/dish-type.model';
import { DishSortingLabels, DishTypeLabels } from '@app/shared/mappers/dish-enum.mapper';
import { DishModalComponent } from '@app/components/dish-modal/dish-modal.component';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { CartStateService } from '@app/services/cart/cart-state.service';
import { ToastService } from '@app/services/toast/toast.service';

@Component({
  selector: 'app-menu-page',
  standalone: true,
  imports: [RouterModule, DishCardComponent, MainCoverImageComponent, DishModalComponent],
  templateUrl: './menu-page.component.html',
  styleUrl: './menu-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
  providers: [DishesService],
})
export class MenuPageComponent implements OnInit {
  private toast = inject(ToastService);
  dishes: DishListResponse[] = [];
  filteredDishes: DishListResponse[] = [];
  isLoadingDishes = false;
  dishSkeletonCount = 4;

  get dishSkeletonItems(): number[] {
    return Array.from({ length: this.dishSkeletonCount }, (_, index) => index + 1);
  }

  isPreOrderEnabled = false;
  preOrderReservationId?: string;
  preOrderReservationLabel?: string;

  sortOptions = Object.values(DishSort);
  sortLabels = DishSortingLabels;

  menuCategories = Object.values(DishType);
  menuCategoryLabels = DishTypeLabels;

  selectedCategory: DishType | null = null; // initially show all
  selectedSort: DishSort = DishSort.PopularityDescending;

  private readonly dishesService = inject(DishesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly orderService = inject(PreOrdersService);
  private readonly cartState = inject(CartStateService);

  ngOnInit(): void {
    this.updateDishSkeletonCount();

    this.route.queryParams.subscribe((params) => {
      const preOrder = params['preOrder'];
      this.isPreOrderEnabled = !!preOrder;
      if (this.isPreOrderEnabled) {
        this.preOrderReservationId = params['reservationId'];
        this.preOrderReservationLabel = params['reservationLabel'];
        if (!this.preOrderReservationId) {
          console.error('Pre-order is enabled but no reservation ID provided.');
          this.toast.error('Pre-order is enabled but no reservation ID provided.', 'Error');
          this.isPreOrderEnabled = false;
        }
      }

      const category = params['category'] as DishType;
      const sort = params['sort'] as DishSort;

      this.selectedCategory =
        category && Object.values(DishType).includes(category) ? category : null;
      this.selectedSort =
        sort && Object.values(DishSort).includes(sort) ? sort : DishSort.PopularityDescending;

      this.loadDishes();
    });
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.updateDishSkeletonCount();
  }

  loadDishes(): void {
    const category = this.selectedCategory ?? undefined;
    const sort = this.selectedSort;
    this.isLoadingDishes = true;

    this.dishesService
      .getDishes(category, sort)
      .pipe(
        finalize(() => {
          this.isLoadingDishes = false;
        })
      )
      .subscribe({
        next: (data) => {
          this.dishes = data.map((dish) => ({
            ...dish,
            imageUrl: dish.imageUrl || 'assets/img/dish.png',
            name: dish.name || 'Unknown Dish',
            price: dish.price || 0,
            weight: dish.weight || 'N/A',
            description: dish.description || 'N/A',
            state: dish.state || 'N/A',
            dishType: dish.dishType || 'N/A',
            calories: dish.calories || '0',
            proteins: dish.proteins || '0',
            fats: dish.fats || '0',
            carbohydrates: dish.carbohydrates || '0',
            vitamins: dish.vitamins || '0',
          }));

          this.filteredDishes = this.dishes;
        },
        error: () => {
          this.dishes = [];
          this.filteredDishes = [];
        },
      });
  }

  onCategorySelected(category: DishType): void {
    this.selectedCategory = this.selectedCategory === category ? null : category;

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        category: this.selectedCategory || null,
        sort: this.selectedSort,
      },
      queryParamsHandling: 'merge',
    });
  }

  onSortChanged(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    const sort = selectElement.value as DishSort;
    this.selectedSort = sort;

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        category: this.selectedCategory || null,
        sort: this.selectedSort,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  selectedDish: DishListResponse | null = null;

  openModal(dish: DishListResponse) {
    this.selectedDish = dish;
  }

  closeModal() {
    this.selectedDish = null;
  }

  addDishToCart(dish: DishListResponse): void {
    this.orderService
      .addDishToPreOrderByReservationId(this.preOrderReservationId!, dish, 1)
      .subscribe({
        next: () => {
          // Same wording as the guest page, so the owner also sees which dish was added.
          this.toast.success(`${dish.name} added to your pre-order.`, 'Success');
          this.cartState.refresh();
        },
        error: (err) => {
          this.toast.error((err as Error).message, 'Error');
        },
      });
  }

  private updateDishSkeletonCount(): void {
    const viewportWidth = window.innerWidth;

    if (viewportWidth <= 768) {
      this.dishSkeletonCount = 1;
      return;
    }

    if (viewportWidth <= 1200) {
      this.dishSkeletonCount = 2;
      return;
    }

    this.dishSkeletonCount = 4;
  }
}
