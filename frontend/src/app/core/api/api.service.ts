import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface ApiError {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  /**
   * Perform a GET request
   */
  get<T>(endpoint: string, params?: Record<string, string | number | boolean>): Observable<T> {
    const httpParams = this.buildParams(params);
    return this.http.get<T>(`${this.baseUrl}${endpoint}`, { params: httpParams })
      .pipe(catchError(this.handleError));
  }

  /**
   * Perform a POST request
   */
  post<T>(endpoint: string, body?: unknown): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${endpoint}`, body)
      .pipe(catchError(this.handleError));
  }

  /**
   * Perform a PUT request
   */
  put<T>(endpoint: string, body?: unknown): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${endpoint}`, body)
      .pipe(catchError(this.handleError));
  }

  /**
   * Perform a PATCH request
   */
  patch<T>(endpoint: string, body?: unknown): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}${endpoint}`, body)
      .pipe(catchError(this.handleError));
  }

  /**
   * Perform a DELETE request
   */
  delete<T = void>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${endpoint}`)
      .pipe(catchError(this.handleError));
  }

  private buildParams(params?: Record<string, string | number | boolean>): HttpParams {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== null && value !== undefined) {
          httpParams = httpParams.set(key, String(value));
        }
      });
    }
    return httpParams;
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let apiError: ApiError;

    if (error.error instanceof ErrorEvent) {
      // Client-side or network error
      apiError = {
        message: error.error.message || 'A network error occurred',
        statusCode: 0
      };
    } else {
      // Server-side error
      apiError = {
        message: error.error?.message || error.message || 'An unexpected error occurred',
        statusCode: error.status,
        errors: error.error?.errors
      };
    }

    return throwError(() => apiError);
  }
}
