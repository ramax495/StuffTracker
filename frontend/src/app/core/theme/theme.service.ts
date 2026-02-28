import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { TelegramService } from '../../telegram/telegram.service';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'theme';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly telegramService = inject(TelegramService);

  readonly theme = signal<Theme>('light');
  readonly isDark = computed(() => this.theme() === 'dark');

  /**
   * Initialize theme on app startup.
   * Priority: localStorage → Telegram colorScheme → system prefers-color-scheme
   */
  initialize(): void {
    if (!isPlatformBrowser(this.platformId)) {
      console.debug('[ThemeService.initialize] skipping — not in browser');
      return;
    }

    const stored = localStorage.getItem(STORAGE_KEY) as Theme | null;

    if (stored === 'dark' || stored === 'light') {
      console.debug(`[ThemeService.initialize] resolved theme: ${stored}, source: localStorage`);
      this.applyTheme(stored);
      return;
    }

    const telegramScheme = this.telegramService.getColorScheme();
    console.debug(`[ThemeService.initialize] resolved theme: ${telegramScheme}, source: telegram`);
    this.applyTheme(telegramScheme);
  }

  /**
   * Toggle between light and dark, persist to localStorage.
   */
  toggleTheme(): void {
    const current = this.theme();
    const next: Theme = current === 'dark' ? 'light' : 'dark';
    console.debug(`[ThemeService.toggleTheme] toggling from ${current} to ${next}`);
    localStorage.setItem(STORAGE_KEY, next);
    this.applyTheme(next);
  }

  /**
   * Apply theme by setting data-theme attribute on documentElement.
   */
  private applyTheme(theme: Theme): void {
    this.theme.set(theme);
    if (isPlatformBrowser(this.platformId)) {
      document.documentElement.setAttribute('data-theme', theme);
    }
  }
}
