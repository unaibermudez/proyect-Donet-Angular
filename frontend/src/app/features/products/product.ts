// Categorías tal como llegan en el JSON (el backend serializa el enum como texto).
export type ProductCategory = 'Phone' | 'Computer' | 'Console';

// Lo que devuelve GET /api/products. Refleja ProductResponse del backend.
export interface Product {
  id: number;
  name: string;
  brand: string;
  model: string;
  category: ProductCategory;
  price: number;
  stock: number;
  releaseDate: string;
  ramGb: number | null;
  storageGb: number | null;
  screenInches: number | null;
}
