import { Injectable, signal } from '@angular/core';
import WebApp from '@twa-dev/sdk';

export interface TelegramUser {
  id: number;
  first_name: string;
  last_name?: string;
  username?: string;
  language_code?: string;
  is_premium?: boolean;
  photo_url?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TelegramService {
  private readonly isTelegramEnvironment: boolean;

  readonly viewportHeight = signal<number>(window.innerHeight);
  readonly viewportStableHeight = signal<number>(window.innerHeight);
  readonly isExpanded = signal<boolean>(false);

  constructor() {
    this.isTelegramEnvironment = this.checkTelegramEnvironment();

    if (this.isTelegramEnvironment) {
      this.setupViewportListener();
    }
  }

  private checkTelegramEnvironment(): boolean {
    try {
      return typeof WebApp !== 'undefined' && !!WebApp.initData;
    } catch {
      return false;
    }
  }

  private setupViewportListener(): void {
    WebApp.onEvent('viewportChanged', (event: { isStateStable: boolean }) => {
      this.viewportHeight.set(WebApp.viewportHeight);
      if (event.isStateStable) {
        this.viewportStableHeight.set(WebApp.viewportStableHeight);
      }
    });
  }

  /**
   * Get the initialization data string for authentication
   */
  getInitData(): string | null {
    if (!this.isTelegramEnvironment) {
      return null;
    }
    return WebApp.initData || null;
  }

  /**
   * Get the parsed user information from Telegram
   */
  getUserInfo(): TelegramUser | null {
    if (!this.isTelegramEnvironment) {
      return null;
    }
    return WebApp.initDataUnsafe?.user || null;
  }

  /**
   * Show the main action button at the bottom
   */
  showMainButton(): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.MainButton.show();
  }

  /**
   * Hide the main action button
   */
  hideMainButton(): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.MainButton.hide();
  }

  /**
   * Set the text displayed on the main button
   */
  setMainButtonText(text: string): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.MainButton.setText(text);
  }

  /**
   * Register a callback for main button clicks
   */
  onMainButtonClick(callback: () => void): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.MainButton.onClick(callback);
  }

  /**
   * Remove a previously registered main button click callback
   */
  offMainButtonClick(callback: () => void): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.MainButton.offClick(callback);
  }

  /**
   * Show the back button in the header
   */
  showBackButton(): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.BackButton.show();
  }

  /**
   * Hide the back button in the header
   */
  hideBackButton(): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.BackButton.hide();
  }

  /**
   * Register a callback for back button clicks
   */
  onBackButtonClick(callback: () => void): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.BackButton.onClick(callback);
  }

  /**
   * Remove a previously registered back button click callback
   */
  offBackButtonClick(callback: () => void): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.BackButton.offClick(callback);
  }

  /**
   * Signal to Telegram that the Mini App is ready to be displayed
   */
  ready(): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.ready();
  }

  /**
   * Expand the Mini App to full height
   */
  expand(): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.expand();
    this.isExpanded.set(true);
    this.viewportHeight.set(WebApp.viewportHeight);
    this.viewportStableHeight.set(WebApp.viewportStableHeight);
  }

  /**
   * Close the Mini App
   */
  close(): void {
    if (!this.isTelegramEnvironment) {
      return;
    }
    WebApp.close();
  }

  /**
   * Check if running inside Telegram environment
   */
  isInTelegram(): boolean {
    return this.isTelegramEnvironment;
  }

  /**
   * Get the current color scheme (dark or light)
   */
  getColorScheme(): 'dark' | 'light' {
    if (!this.isTelegramEnvironment) {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return WebApp.colorScheme;
  }

  /**
   * Get theme parameters from Telegram
   */
  getThemeParams(): Record<string, string | undefined> {
    if (!this.isTelegramEnvironment) {
      return {};
    }
    // ThemeParams type from SDK has specific keys, cast via unknown for flexibility
    return (WebApp.themeParams as unknown as Record<string, string | undefined>) || {};
  }
}
