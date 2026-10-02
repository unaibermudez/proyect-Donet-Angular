import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Product } from './product';

// Acceso a la API de productos. Los componentes lo usan en vez de llamar a HTTP directamente.
@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.baseUrl);
  }
}
