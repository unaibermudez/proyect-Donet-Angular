import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Product, ProductCategory } from '../product';
import { ProductService } from '../product-service';

// Textos en español para cada categoría que llega de la API.
const categoryLabels: Record<ProductCategory, string> = {
  Phone: 'Móvil',
  Computer: 'Ordenador',
  Console: 'Consola',
};

@Component({
  imports: [CurrencyPipe],
  selector: 'app-product-list',
  styleUrl: './product-list.css',
  templateUrl: './product-list.html',
})
export class ProductList {
  private readonly productService = inject(ProductService);

  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly productCount = computed(() => this.products().length);
  protected readonly categoryLabels = categoryLabels;

  constructor() {
    this.productService
      .getAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (products) => {
          this.products.set(products);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('No se pudieron cargar los productos. ¿Está arrancada la API?');
          this.loading.set(false);
        },
      });
  }
}
