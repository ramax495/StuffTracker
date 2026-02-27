import { ChangeDetectionStrategy, Component, OnInit, inject, effect, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { TelegramService } from './telegram/telegram.service';
import { NavigationService } from './core/navigation.service';
import { ErrorToastComponent } from './shared/components/error-toast';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, ErrorToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly router = inject(Router);
  private readonly telegramService = inject(TelegramService);
  private readonly navigationService = inject(NavigationService);
  private readonly platformId = inject(PLATFORM_ID);

  constructor() {
    // Update CSS custom properties when viewport height changes
    effect(() => {
      if (isPlatformBrowser(this.platformId)) {
        const height = this.telegramService.viewportHeight();
        const stableHeight = this.telegramService.viewportStableHeight();

        document.documentElement.style.setProperty('--tg-viewport-height', `${height}px`);
        document.documentElement.style.setProperty('--tg-viewport-stable-height', `${stableHeight}px`);
      }
    });
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      // Signal to Telegram that the Mini App is ready
      this.telegramService.ready();

      // Expand to full height
      this.telegramService.expand();

      // Initialize navigation service for BackButton management
      this.navigationService.initialize();
    }
  }

  /**
   * Navigate to the search page
   */
  navigateToSearch(): void {
    this.triggerHapticFeedback();
    this.router.navigate(['/search']);
  }

  private triggerHapticFeedback(): void {
    if (this.telegramService.isInTelegram()) {
      try {
        // @ts-expect-error - HapticFeedback may not be typed in SDK
        window.Telegram?.WebApp?.HapticFeedback?.impactOccurred('light');
      } catch {
        // Silently ignore if haptic feedback is not available
      }
    }
  }
}
