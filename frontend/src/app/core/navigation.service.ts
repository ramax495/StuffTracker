import { Injectable, inject, DestroyRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs/operators';
import { TelegramService } from '../telegram/telegram.service';

@Injectable({
  providedIn: 'root'
})
export class NavigationService {
  private readonly router = inject(Router);
  private readonly telegramService = inject(TelegramService);
  private readonly destroyRef = inject(DestroyRef);

  private isInitialized = false;
  private readonly homeRoutes = ['/', ''];

  /**
   * Initialize navigation service.
   * Should be called once from AppComponent.
   */
  initialize(): void {
    if (this.isInitialized) {
      return;
    }

    this.isInitialized = true;
    this.setupBackButtonHandler();
    this.subscribeToRouterEvents();
    this.updateBackButtonVisibility(this.router.url);
  }

  private setupBackButtonHandler(): void {
    const backButtonHandler = () => {
      this.navigateBack();
    };

    this.telegramService.onBackButtonClick(backButtonHandler);

    this.destroyRef.onDestroy(() => {
      this.telegramService.offBackButtonClick(backButtonHandler);
    });
  }

  private subscribeToRouterEvents(): void {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((event) => {
        this.updateBackButtonVisibility(event.urlAfterRedirects);
      });
  }

  private updateBackButtonVisibility(url: string): void {
    const isHomeRoute = this.isHome(url);

    if (isHomeRoute) {
      this.telegramService.hideBackButton();
    } else {
      this.telegramService.showBackButton();
    }
  }

  private isHome(url: string): boolean {
    const cleanUrl = url.split('?')[0].split('#')[0];
    return this.homeRoutes.includes(cleanUrl);
  }

  /**
   * Navigate back in history or to home if no history
   */
  navigateBack(): void {
    const currentUrl = this.router.url;

    if (this.isHome(currentUrl)) {
      return;
    }

    // Check if we can go back in browser history
    if (window.history.length > 1) {
      window.history.back();
    } else {
      // Navigate to home if no history
      this.router.navigate(['/']);
    }
  }

  /**
   * Navigate to a specific route
   */
  navigateTo(path: string | string[]): void {
    const commands = Array.isArray(path) ? path : [path];
    this.router.navigate(commands);
  }
}
